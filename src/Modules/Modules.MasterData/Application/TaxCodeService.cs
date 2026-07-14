using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.MasterData.Application;

public sealed class TaxCodeService
{
    public const string NumberRangeKey = "MD-TAX";

    private const string AuditTargetType = "TaxCode";
    private const string AuditSource = "Modules.MasterData";

    private readonly ITaxCodeRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public TaxCodeService(
        ITaxCodeRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    public async Task<TaxCodeDto> CreateAsync(
        CreateTaxCodeRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, TaxCodeSecurity.MaintainPrivilegeKey);

        if (!Enum.TryParse<TaxType>(request.TaxType, ignoreCase: true, out var taxType))
            throw new ArgumentException($"Invalid tax type '{request.TaxType}'. Expected Standard, ZeroRated, or Exempt.");

        var existing = await _repository.GetByCodeAsync(request.TaxCodeCode, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Tax code '{request.TaxCodeCode}' is already in use.");

        var taxCode = new TaxCode(actor, request.TaxCodeCode, request.TaxCodeName, request.Rate, taxType);
        taxCode.UpdateTaxCodeNameArabic(request.TaxCodeNameArabic);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        taxCode.AssignNumber(documentNumber);

        _repository.Add(taxCode);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(taxCode.Id), actor,
            $"Tax code '{taxCode.TaxCodeCode}' ({taxCode.TaxCodeName}) created.", AuditSource);

        return ToDto(taxCode);
    }

    public async Task<TaxCodeDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var taxCode = await _repository.GetAsync(id, cancellationToken);
        return taxCode is null ? null : ToDto(taxCode);
    }

    public async Task<(IReadOnlyList<TaxCodeDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<TaxCodeDto> UpdateAsync(
        Guid id, UpdateTaxCodeRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, TaxCodeSecurity.MaintainPrivilegeKey);
        var taxCode = await RequireTaxCodeAsync(id, cancellationToken);

        taxCode.UpdateTaxCodeName(request.TaxCodeName);
        taxCode.UpdateTaxCodeNameArabic(request.TaxCodeNameArabic);
        taxCode.UpdateRate(request.Rate);
        if (request.IsActive) taxCode.Activate(); else taxCode.Deactivate();

        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(taxCode.Id), actor,
            $"Tax code '{taxCode.TaxCodeCode}' updated.",
            new[]
            {
                new FieldValueChange("TaxCodeName", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.TaxCodeName)),
                new FieldValueChange("Rate", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.Rate)),
                new FieldValueChange("IsActive", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsActive)),
            },
            AuditSource);

        return ToDto(taxCode);
    }

    public async Task<TaxCodeDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, TaxCodeSecurity.MaintainPrivilegeKey);
        var taxCode = await RequireTaxCodeAsync(id, cancellationToken);
        var fromStatus = taxCode.Status;
        taxCode.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(taxCode.Id), actor,
            $"Tax code '{taxCode.TaxCodeCode}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(taxCode.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(TaxCodeWorkflow.BusinessObjectType, TaxCodeWorkflow.SubmitTransition, taxCode.Id);
        if (instance is null) { await ApproveInternalAsync(taxCode, actor, cancellationToken); return ToDto(taxCode); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(taxCode, actor, cancellationToken);

        return ToDto(taxCode);
    }

    public Task<TaxCodeDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<TaxCodeDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<TaxCodeDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, TaxCodeSecurity.ApprovePrivilegeKey);
        var taxCode = await RequireTaxCodeAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(TaxCodeWorkflow.BusinessObjectType, taxCode.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Tax code {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(taxCode, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(taxCode, actor, cancellationToken);

        return ToDto(taxCode);
    }

    private async Task ApproveInternalAsync(TaxCode taxCode, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = taxCode.Status;
        taxCode.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(taxCode.Id), actor,
            $"Tax code '{taxCode.TaxCodeCode}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(taxCode.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(TaxCode taxCode, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = taxCode.Status;
        taxCode.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(taxCode.Id), actor,
            $"Tax code '{taxCode.TaxCodeCode}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(taxCode.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid taxCodeId) => new(taxCodeId, AuditTargetType, "Self");

    private async Task<TaxCode> RequireTaxCodeAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tax code {id} was not found.");

    private static TaxCodeDto ToDto(TaxCode t) => new(
        t.Id, t.DocumentNumber, t.Status.ToString(), t.TaxCodeCode, t.TaxCodeName, t.TaxCodeNameArabic,
        t.Rate, t.TaxType.ToString(), t.IsActive, t.CreatedAt, t.CreatedBy);
}
