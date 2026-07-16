using System.Text.Json;
using Modules.Construction.Domain;
using Modules.MasterData.Contracts;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Construction.Application;

public sealed class SubcontractService
{
    public const string NumberRangeKey = "CON-SUBCONTRACT";

    /// <summary>Narrower than <c>Modules.Procurement.Application.PurchaseOrderService.VendorEligibleRoles</c>
    /// — a Subcontract is semantically for an actual subcontractor, not any vendor-family role.</summary>
    private static readonly HashSet<string> SubcontractorEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Subcontractor",
    };

    private const string AuditTargetType = "Subcontract";
    private const string AuditSource = "Modules.Construction";

    private readonly ISubcontractRepository _repository;
    private readonly IContractRepository _contractRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IProjectLookup _projectLookup;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly ILookupCatalog _lookupCatalog;

    public SubcontractService(
        ISubcontractRepository repository,
        IContractRepository contractRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup,
        IBusinessPartnerLookup businessPartnerLookup,
        ILookupCatalog lookupCatalog)
    {
        _repository = repository;
        _contractRepository = contractRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _projectLookup = projectLookup;
        _businessPartnerLookup = businessPartnerLookup;
        _lookupCatalog = lookupCatalog;
    }

    public async Task<SubcontractDto> CreateAsync(
        CreateSubcontractRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, SubcontractSecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A subcontract needs at least one line.");

        var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
            ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
        if (project.Status != "Approved")
            throw new ArgumentException($"Project '{project.ProjectName}' is not Approved (status: {project.Status}) and cannot be subcontracted against.");

        await ValidateSubcontractorAsync(request.SubcontractorId, cancellationToken);

        if (request.ContractId is { } contractId)
        {
            var contract = await _contractRepository.GetAsync(contractId, cancellationToken)
                ?? throw new ArgumentException($"Contract {contractId} was not found.");
            if (contract.ProjectId != request.ProjectId)
                throw new ArgumentException($"Contract '{contract.DocumentNumber}' does not belong to project '{project.ProjectName}'.");
            if (contract.Status != BusinessObjectStatus.Approved)
                throw new ArgumentException($"Contract '{contract.DocumentNumber}' is not Approved and cannot be referenced by a subcontract.");
        }

        var wbsElementsById = project.WbsElements.ToDictionary(w => w.Id);

        var subcontract = new Subcontract(
            actor, request.ProjectId, request.ContractId, request.SubcontractorId,
            request.RetentionPercentage, request.MobilizationAdvancePercentage, request.DefectsLiabilityPeriodMonths);

        foreach (var lineRequest in request.Lines)
        {
            if (!wbsElementsById.ContainsKey(lineRequest.WbsElementId))
                throw new ArgumentException(
                    $"WBS element {lineRequest.WbsElementId} does not belong to project '{project.ProjectName}'.");

            var unitOfMeasure = await _lookupCatalog.GetValueAsync("UnitOfMeasure", lineRequest.UnitOfMeasure, cancellationToken);
            if (unitOfMeasure is null || !unitOfMeasure.IsActive)
                throw new ArgumentException($"'{lineRequest.UnitOfMeasure}' is not a known, active Unit of Measure.");

            subcontract.AddLine(
                lineRequest.Code, lineRequest.Description, lineRequest.DescriptionArabic, lineRequest.UnitOfMeasure,
                lineRequest.Quantity, lineRequest.Rate, lineRequest.WbsElementId);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        subcontract.AssignNumber(documentNumber);

        _repository.Add(subcontract);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(subcontract.Id), actor,
            $"Subcontract '{subcontract.DocumentNumber}' created ({subcontract.Lines.Count} lines, value {subcontract.SubcontractValue}).", AuditSource);

        return ToDto(subcontract);
    }

    public async Task<SubcontractDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subcontract = await _repository.GetAsync(id, cancellationToken);
        return subcontract is null ? null : ToDto(subcontract);
    }

    public async Task<(IReadOnlyList<SubcontractDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<SubcontractDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, SubcontractSecurity.MaintainPrivilegeKey);
        var subcontract = await RequireSubcontractAsync(id, cancellationToken);

        var fromStatus = subcontract.Status;
        subcontract.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(subcontract.Id), actor,
            $"Subcontract '{subcontract.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(subcontract.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(SubcontractWorkflow.BusinessObjectType, SubcontractWorkflow.SubmitTransition, subcontract.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(subcontract, actor, cancellationToken);
        }

        return ToDto(subcontract);
    }

    public Task<SubcontractDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<SubcontractDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<SubcontractDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, SubcontractSecurity.ApprovePrivilegeKey);
        var subcontract = await RequireSubcontractAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(SubcontractWorkflow.BusinessObjectType, subcontract.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Subcontract {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(subcontract, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(subcontract, actor, cancellationToken);

        return ToDto(subcontract);
    }

    private async Task ApproveInternalAsync(Subcontract subcontract, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = subcontract.Status;
        subcontract.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(subcontract.Id), actor,
            $"Subcontract '{subcontract.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(subcontract.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Subcontract subcontract, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = subcontract.Status;
        subcontract.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(subcontract.Id), actor,
            $"Subcontract '{subcontract.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(subcontract.Status.ToString()), AuditSource);
    }

    /// <summary>Records a back-charge against an Approved subcontract — see <c>Subcontract.AddBackCharge</c>
    /// for why this is gated on Approved rather than allowed at any status.</summary>
    public async Task<SubcontractDto> AddBackChargeAsync(
        Guid id, AddBackChargeRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, SubcontractSecurity.MaintainPrivilegeKey);
        var subcontract = await RequireSubcontractAsync(id, cancellationToken);

        var backCharge = subcontract.AddBackCharge(request.Description, request.Amount, request.DateIncurred);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(subcontract.Id), actor,
            $"Back charge of {backCharge.Amount} recorded against subcontract '{subcontract.DocumentNumber}'.",
            new[] { new FieldValueChange("BackCharges", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(backCharge.Amount)) },
            AuditSource);

        return ToDto(subcontract);
    }

    private async Task ValidateSubcontractorAsync(Guid subcontractorId, CancellationToken cancellationToken)
    {
        var subcontractor = await _businessPartnerLookup.GetAsync(subcontractorId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {subcontractorId} was not found.");
        if (subcontractor.Status != "Approved")
            throw new ArgumentException($"Business partner '{subcontractor.Name}' is not Approved and cannot receive a subcontract.");
        if (!subcontractor.BusinessRoles.Any(SubcontractorEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{subcontractor.Name}' does not hold the Subcontractor role and cannot receive a subcontract.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<Subcontract> RequireSubcontractAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Subcontract {id} was not found.");

    private static SubcontractDto ToDto(Subcontract s) => new(
        s.Id, s.DocumentNumber, s.Status.ToString(), s.ProjectId, s.ContractId, s.SubcontractorId,
        s.RetentionPercentage, s.MobilizationAdvancePercentage, s.DefectsLiabilityPeriodMonths,
        s.SubcontractValue, s.TotalBackCharges, s.NetPayableValue,
        s.Lines.Select(l => new SubcontractLineDto(
            l.Id, l.Code, l.Description, l.DescriptionArabic, l.UnitOfMeasure, l.Quantity, l.Rate, l.Amount, l.WbsElementId)).ToList(),
        s.BackCharges.Select(b => new BackChargeDto(b.Id, b.Description, b.Amount, b.DateIncurred)).ToList(),
        s.CreatedAt, s.CreatedBy);
}
