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

public sealed class ContractService
{
    public const string NumberRangeKey = "CON-CONTRACT";

    private const string AuditTargetType = "Contract";
    private const string AuditSource = "Modules.Construction";

    private readonly IContractRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IProjectLookup _projectLookup;
    private readonly ILookupCatalog _lookupCatalog;

    public ContractService(
        IContractRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IProjectLookup projectLookup,
        ILookupCatalog lookupCatalog)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _projectLookup = projectLookup;
        _lookupCatalog = lookupCatalog;
    }

    public async Task<ContractDto> CreateAsync(
        CreateContractRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ContractSecurity.MaintainPrivilegeKey);

        if (request.BoqLines.Count == 0)
            throw new ArgumentException("A contract needs at least one BOQ line.");

        var project = await _projectLookup.GetAsync(request.ProjectId, cancellationToken)
            ?? throw new ArgumentException($"Project {request.ProjectId} was not found.");
        if (project.Status != "Approved")
            throw new ArgumentException($"Project '{project.ProjectName}' is not Approved (status: {project.Status}) and cannot be contracted against.");

        var contractType = await _lookupCatalog.GetValueAsync("ContractType", request.ContractType, cancellationToken);
        if (contractType is null || !contractType.IsActive)
            throw new ArgumentException($"'{request.ContractType}' is not a known, active Contract Type.");

        var wbsElementsById = project.WbsElements.ToDictionary(w => w.Id);

        var contract = new Contract(
            actor, request.ProjectId, request.ContractType, request.PaymentTerms,
            request.AdvancePaymentPercentage, request.DefectsLiabilityPeriodMonths);

        foreach (var lineRequest in request.BoqLines)
        {
            if (!wbsElementsById.ContainsKey(lineRequest.WbsElementId))
                throw new ArgumentException(
                    $"WBS element {lineRequest.WbsElementId} does not belong to project '{project.ProjectName}'.");

            var unitOfMeasure = await _lookupCatalog.GetValueAsync("UnitOfMeasure", lineRequest.UnitOfMeasure, cancellationToken);
            if (unitOfMeasure is null || !unitOfMeasure.IsActive)
                throw new ArgumentException($"'{lineRequest.UnitOfMeasure}' is not a known, active Unit of Measure.");

            contract.AddBoqLine(
                lineRequest.Code, lineRequest.Description, lineRequest.DescriptionArabic, lineRequest.UnitOfMeasure,
                lineRequest.Quantity, lineRequest.Rate, lineRequest.WbsElementId);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        contract.AssignNumber(documentNumber);

        _repository.Add(contract);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(contract.Id), actor,
            $"Contract '{contract.DocumentNumber}' created ({contract.BoqLines.Count} BOQ lines, value {contract.ContractValue}).", AuditSource);

        return ToDto(contract);
    }

    public async Task<ContractDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await _repository.GetAsync(id, cancellationToken);
        return contract is null ? null : ToDto(contract);
    }

    public async Task<(IReadOnlyList<ContractDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<ContractDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ContractSecurity.MaintainPrivilegeKey);
        var contract = await RequireContractAsync(id, cancellationToken);

        var fromStatus = contract.Status;
        contract.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(contract.Id), actor,
            $"Contract '{contract.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(contract.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(ContractWorkflow.BusinessObjectType, ContractWorkflow.SubmitTransition, contract.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(contract, actor, cancellationToken);
        }

        return ToDto(contract);
    }

    public Task<ContractDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<ContractDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<ContractDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, ContractSecurity.ApprovePrivilegeKey);
        var contract = await RequireContractAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(ContractWorkflow.BusinessObjectType, contract.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Contract {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(contract, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(contract, actor, cancellationToken);

        return ToDto(contract);
    }

    private async Task ApproveInternalAsync(Contract contract, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = contract.Status;
        contract.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(contract.Id), actor,
            $"Contract '{contract.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(contract.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Contract contract, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = contract.Status;
        contract.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(contract.Id), actor,
            $"Contract '{contract.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(contract.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<Contract> RequireContractAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Contract {id} was not found.");

    private static ContractDto ToDto(Contract c) => new(
        c.Id, c.DocumentNumber, c.Status.ToString(), c.ProjectId, c.ContractType, c.PaymentTerms,
        c.AdvancePaymentPercentage, c.DefectsLiabilityPeriodMonths, c.ContractValue,
        c.BoqLines.Select(l => new BoqLineDto(
            l.Id, l.Code, l.Description, l.DescriptionArabic, l.UnitOfMeasure, l.Quantity, l.Rate, l.Amount, l.WbsElementId)).ToList(),
        c.CreatedAt, c.CreatedBy);
}
