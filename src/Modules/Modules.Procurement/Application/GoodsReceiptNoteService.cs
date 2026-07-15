using System.Text.Json;
using Modules.Procurement.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Procurement.Application;

public sealed class GoodsReceiptNoteService
{
    public const string NumberRangeKey = "PROC-GRN";

    /// <summary>GRN statuses that still count toward "already received/committed" when checking a new line
    /// against its PO line's ordered quantity — everything except Rejected, which never happened.</summary>
    private static readonly HashSet<BusinessObjectStatus> CommittedStatuses = new()
    {
        BusinessObjectStatus.Draft, BusinessObjectStatus.Submitted, BusinessObjectStatus.Approved,
    };

    private const string AuditTargetType = "GoodsReceiptNote";
    private const string AuditSource = "Modules.Procurement";

    private readonly IGoodsReceiptNoteRepository _repository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public GoodsReceiptNoteService(
        IGoodsReceiptNoteRepository repository,
        IPurchaseOrderRepository purchaseOrderRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    public async Task<GoodsReceiptNoteDto> CreateAsync(
        CreateGoodsReceiptNoteRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GoodsReceiptNoteSecurity.MaintainPrivilegeKey);

        if (request.Lines.Count == 0)
            throw new ArgumentException("A goods receipt note needs at least one line.");

        var purchaseOrder = await _purchaseOrderRepository.GetAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new ArgumentException($"Purchase order {request.PurchaseOrderId} was not found.");
        if (purchaseOrder.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"Purchase order '{purchaseOrder.DocumentNumber}' must be Approved before a goods receipt note can be raised against it.");

        var existingGrns = await _repository.ListByPurchaseOrderAsync(request.PurchaseOrderId, cancellationToken);
        var alreadyReceivedByLine = existingGrns
            .Where(g => CommittedStatuses.Contains(g.Status))
            .SelectMany(g => g.Lines)
            .GroupBy(l => l.PurchaseOrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantityReceived));

        var grn = new GoodsReceiptNote(actor, request.PurchaseOrderId, request.ReceivedDate);
        foreach (var line in request.Lines)
        {
            var poLine = purchaseOrder.Lines.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId)
                ?? throw new ArgumentException($"Line {line.PurchaseOrderLineId} does not belong to purchase order '{purchaseOrder.DocumentNumber}'.");

            var alreadyReceived = alreadyReceivedByLine.GetValueOrDefault(poLine.Id);
            if (alreadyReceived + line.QuantityReceived > poLine.Quantity)
                throw new ArgumentException(
                    $"Line for item {poLine.ItemId} would receive {alreadyReceived + line.QuantityReceived}, " +
                    $"exceeding the ordered quantity of {poLine.Quantity}.");

            grn.AddLine(poLine.Id, poLine.ItemId, line.QuantityReceived, poLine.UnitPrice);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        grn.AssignNumber(documentNumber);

        _repository.Add(grn);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(grn.Id), actor,
            $"Goods receipt note '{grn.DocumentNumber}' created against '{purchaseOrder.DocumentNumber}' ({grn.Lines.Count} lines, value {grn.ReceivedValue}).", AuditSource);

        return ToDto(grn);
    }

    public async Task<GoodsReceiptNoteDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var grn = await _repository.GetAsync(id, cancellationToken);
        return grn is null ? null : ToDto(grn);
    }

    public async Task<(IReadOnlyList<GoodsReceiptNoteDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<GoodsReceiptNoteDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, GoodsReceiptNoteSecurity.MaintainPrivilegeKey);
        var grn = await RequireGrnAsync(id, cancellationToken);

        var fromStatus = grn.Status;
        grn.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(grn.Id), actor,
            $"Goods receipt note '{grn.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(grn.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(GoodsReceiptNoteWorkflow.BusinessObjectType, GoodsReceiptNoteWorkflow.SubmitTransition, grn.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(grn, actor, cancellationToken);
        }

        return ToDto(grn);
    }

    public Task<GoodsReceiptNoteDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<GoodsReceiptNoteDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<GoodsReceiptNoteDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, GoodsReceiptNoteSecurity.ApprovePrivilegeKey);
        var grn = await RequireGrnAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(GoodsReceiptNoteWorkflow.BusinessObjectType, grn.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Goods receipt note {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(grn, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(grn, actor, cancellationToken);

        return ToDto(grn);
    }

    private async Task ApproveInternalAsync(GoodsReceiptNote grn, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = grn.Status;
        grn.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(grn.Id), actor,
            $"Goods receipt note '{grn.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(grn.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(GoodsReceiptNote grn, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = grn.Status;
        grn.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(grn.Id), actor,
            $"Goods receipt note '{grn.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(grn.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<GoodsReceiptNote> RequireGrnAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goods receipt note {id} was not found.");

    private static GoodsReceiptNoteDto ToDto(GoodsReceiptNote g) => new(
        g.Id, g.DocumentNumber, g.Status.ToString(), g.PurchaseOrderId, g.ReceivedDate, g.ReceivedValue,
        g.Lines.Select(l => new GrnLineDto(l.Id, l.PurchaseOrderLineId, l.ItemId, l.QuantityReceived, l.UnitPrice, l.LineValue)).ToList(),
        g.CreatedAt, g.CreatedBy);
}
