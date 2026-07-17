using System.Text.Json;
using Modules.Construction.Domain;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Construction.Application;

/// <summary>
/// Orchestrates <see cref="MeasurementSheet"/> — the piece that resolves polymorphism over "commercial
/// document" (construction-commercial-processes-spec.md §6c) at the Application layer, since the Domain type
/// deliberately has no dependency on <see cref="IContractRepository"/>/<see cref="ISubcontractRepository"/>
/// (both live in this same module, so no cross-module lookup is needed, unlike <see cref="IProjectLookup"/>).
/// </summary>
public sealed class MeasurementSheetService
{
    public const string NumberRangeKey = "CON-MEASUREMENT";

    private const string AuditTargetType = "MeasurementSheet";
    private const string AuditSource = "Modules.Construction";

    private readonly IMeasurementSheetRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly ISubcontractRepository _subcontractRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IProjectLookup _projectLookup;

    public MeasurementSheetService(
        IMeasurementSheetRepository repository,
        IContractRepository contractRepository,
        ISubcontractRepository subcontractRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _subcontractRepository = subcontractRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _projectLookup = projectLookup;
    }

    public async Task<MeasurementSheetDto> CreateAsync(
        CreateMeasurementSheetRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, MeasurementSheetSecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A measurement sheet needs at least one line.");
        if (!Enum.TryParse<CommercialDocumentType>(request.CommercialDocumentType, ignoreCase: true, out var documentType))
            throw new ArgumentException(
                $"'{request.CommercialDocumentType}' is not a known commercial document type (Contract or Subcontract).");

        var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
            ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
        if (project.Status != "Approved")
            throw new ArgumentException(
                $"Project '{project.ProjectName}' is not Approved (status: {project.Status}) and cannot be measured against.");

        var document = await LoadCommercialDocumentAsync(documentType, request.CommercialDocumentId, cancellationToken);
        if (document.ProjectId != request.ProjectId)
            throw new ArgumentException($"{documentType} '{document.Label}' does not belong to project '{project.ProjectName}'.");
        if (document.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"{documentType} '{document.Label}' is not Approved and cannot be measured against.");

        var wbsElementsById = project.WbsElements.ToDictionary(w => w.Id);

        var sheet = new MeasurementSheet(
            actor, request.ProjectId, documentType, request.CommercialDocumentId, request.PeriodStart, request.PeriodEnd, request.Notes);

        foreach (var lineRequest in request.Lines)
        {
            if (!document.Lines.TryGetValue(lineRequest.CommercialDocumentLineId, out var documentLine))
                throw new ArgumentException(
                    $"Line {lineRequest.CommercialDocumentLineId} does not belong to this {documentType}.");
            if (!wbsElementsById.TryGetValue(documentLine.WbsElementId, out var wbsElement) || !wbsElement.IsBillingElement)
                throw new ArgumentException(
                    $"The WBS element for line {lineRequest.CommercialDocumentLineId} is not flagged as a billing " +
                    "element and cannot be measured.");

            sheet.AddLine(lineRequest.CommercialDocumentLineId, lineRequest.QuantitySubmitted, lineRequest.Remarks);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        sheet.AssignNumber(documentNumber);

        _repository.Add(sheet);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(sheet.Id), actor,
            $"Measurement sheet '{sheet.DocumentNumber}' created against {documentType} '{document.Label}' ({sheet.Lines.Count} lines).",
            AuditSource);

        return ToDto(sheet);
    }

    public async Task<MeasurementSheetDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sheet = await _repository.GetAsync(id, cancellationToken);
        return sheet is null ? null : ToDto(sheet);
    }

    public async Task<(IReadOnlyList<MeasurementSheetDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<MeasurementSheetDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, MeasurementSheetSecurity.MaintainPrivilegeKey);
        var sheet = await RequireSheetAsync(id, cancellationToken);

        var fromStatus = sheet.Status;
        sheet.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(sheet.Id), actor,
            $"Measurement sheet '{sheet.DocumentNumber}' submitted for certification.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(sheet.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(MeasurementSheetWorkflow.BusinessObjectType, MeasurementSheetWorkflow.SubmitTransition, sheet.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        }

        return ToDto(sheet);
    }

    /// <summary>The Engineer's certification decision — unlike every other module's parameterless Approve,
    /// this one carries the per-line certified quantities (spec §2: a lower certified quantity than
    /// submitted is routine, not an edge case) and enforces the over-measurement guard against sibling-sheet
    /// history before ever calling the domain's Approve transition.</summary>
    public async Task<MeasurementSheetDto> CertifyAsync(
        Guid id, CertifyMeasurementSheetRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, MeasurementSheetSecurity.ApprovePrivilegeKey);
        var sheet = await RequireSheetAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(MeasurementSheetWorkflow.BusinessObjectType, sheet.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Measurement sheet {id} has no pending certification to decide.");

        await ValidateNoOverMeasurementAsync(sheet, request, cancellationToken);

        var certifiedByLineId = request.Lines.ToDictionary(l => l.LineId, l => l.QuantityCertified);
        sheet.RecordCertifiedQuantities(certifiedByLineId);

        _workflowEngine.Decide(instance, BuildPrincipal(actor), WorkflowDecision.Approve);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
        {
            var fromStatus = sheet.Status;
            sheet.Approve(actor);
            await _repository.SaveChangesAsync(cancellationToken);
            _auditRecorder.RecordStatusTransition(AuditReference(sheet.Id), actor,
                $"Measurement sheet '{sheet.DocumentNumber}' certified.",
                JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(sheet.Status.ToString()), AuditSource);
        }

        return ToDto(sheet);
    }

    public async Task<MeasurementSheetDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, MeasurementSheetSecurity.ApprovePrivilegeKey);
        var sheet = await RequireSheetAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(MeasurementSheetWorkflow.BusinessObjectType, sheet.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Measurement sheet {id} has no pending certification to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), WorkflowDecision.Reject);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Rejected)
        {
            var fromStatus = sheet.Status;
            sheet.Reject(actor);
            await _repository.SaveChangesAsync(cancellationToken);
            _auditRecorder.RecordStatusTransition(AuditReference(sheet.Id), actor,
                $"Measurement sheet '{sheet.DocumentNumber}' rejected.",
                JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(sheet.Status.ToString()), AuditSource);
        }

        return ToDto(sheet);
    }

    /// <summary>Cumulative certified quantity across every OTHER Approved sheet against the same commercial
    /// document line, plus what's being certified now, must not exceed that line's own Quantity — the spec's
    /// §2 guard against over-measurement without an approved Variation Order first increasing it.</summary>
    private async Task ValidateNoOverMeasurementAsync(
        MeasurementSheet sheet, CertifyMeasurementSheetRequest request, CancellationToken cancellationToken)
    {
        var document = await LoadCommercialDocumentAsync(sheet.CommercialDocumentType, sheet.CommercialDocumentId, cancellationToken);
        var siblingSheets = await _repository.ListByCommercialDocumentAsync(
            sheet.CommercialDocumentType, sheet.CommercialDocumentId, cancellationToken);

        var lineById = sheet.Lines.ToDictionary(l => l.Id);
        foreach (var certification in request.Lines)
        {
            if (!lineById.TryGetValue(certification.LineId, out var line))
                throw new ArgumentException($"Line {certification.LineId} does not belong to measurement sheet {sheet.Id}.");
            if (certification.QuantityCertified < 0)
                throw new ArgumentException("Certified quantity cannot be negative.");
            if (!document.Lines.TryGetValue(line.CommercialDocumentLineId, out var documentLine))
                throw new ArgumentException($"Line {line.CommercialDocumentLineId} no longer exists on this {sheet.CommercialDocumentType}.");

            var priorCertified = siblingSheets
                .Where(s => s.Id != sheet.Id && s.Status == BusinessObjectStatus.Approved)
                .SelectMany(s => s.Lines)
                .Where(l => l.CommercialDocumentLineId == line.CommercialDocumentLineId)
                .Sum(l => l.QuantityCertified ?? 0m);

            var cumulativeToDate = priorCertified + certification.QuantityCertified;
            if (cumulativeToDate > documentLine.Quantity)
                throw new ArgumentException(
                    $"Certifying {certification.QuantityCertified} for line {line.CommercialDocumentLineId} would bring the " +
                    $"cumulative certified quantity to {cumulativeToDate}, exceeding the line's own quantity of " +
                    $"{documentLine.Quantity}. An approved Variation Order must increase the line's quantity first.");
        }
    }

    private async Task<CommercialDocumentSnapshot> LoadCommercialDocumentAsync(
        CommercialDocumentType type, Guid id, CancellationToken cancellationToken)
    {
        if (type == CommercialDocumentType.Contract)
        {
            var contract = await _contractRepository.GetAsync(id, cancellationToken)
                ?? throw new ArgumentException($"Contract {id} was not found.");
            return new CommercialDocumentSnapshot(
                contract.ProjectId, contract.Status, contract.DocumentNumber,
                contract.BoqLines.ToDictionary(l => l.Id, l => (l.WbsElementId, l.Quantity)));
        }

        var subcontract = await _subcontractRepository.GetAsync(id, cancellationToken)
            ?? throw new ArgumentException($"Subcontract {id} was not found.");
        return new CommercialDocumentSnapshot(
            subcontract.ProjectId, subcontract.Status, subcontract.DocumentNumber,
            subcontract.Lines.ToDictionary(l => l.Id, l => (l.WbsElementId, l.Quantity)));
    }

    private sealed record CommercialDocumentSnapshot(
        Guid ProjectId, BusinessObjectStatus Status, string? Label,
        IReadOnlyDictionary<Guid, (Guid WbsElementId, decimal Quantity)> Lines);

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<MeasurementSheet> RequireSheetAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Measurement sheet {id} was not found.");

    private static MeasurementSheetDto ToDto(MeasurementSheet s) => new(
        s.Id, s.DocumentNumber, s.Status.ToString(), s.ProjectId, s.CommercialDocumentType.ToString(), s.CommercialDocumentId,
        s.PeriodStart, s.PeriodEnd, s.Notes,
        s.Lines.Select(l => new MeasurementLineDto(l.Id, l.CommercialDocumentLineId, l.QuantitySubmitted, l.QuantityCertified, l.Remarks)).ToList(),
        s.CreatedAt, s.CreatedBy);
}
