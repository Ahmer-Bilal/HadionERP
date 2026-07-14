using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.MasterData.Application;

public sealed class ItemService
{
    public const string NumberRangeKey = "MD-ITEM";

    private const string AuditTargetType = "Item";
    private const string AuditSource = "Modules.MasterData";

    private readonly IItemRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public ItemService(
        IItemRepository repository,
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

    public async Task<ItemDto> CreateAsync(
        CreateItemRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ItemSecurity.MaintainPrivilegeKey);

        if (!Enum.TryParse<ItemType>(request.ItemType, ignoreCase: true, out var itemType))
            throw new ArgumentException($"Invalid item type '{request.ItemType}'. Expected Stock, NonStock, or Service.");

        var existing = await _repository.GetByCodeAsync(request.ItemCode, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Item code '{request.ItemCode}' is already in use.");

        var item = new Item(actor, request.ItemCode, request.ItemName, itemType, request.UnitOfMeasure);
        item.UpdateItemNameArabic(request.ItemNameArabic);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        item.AssignNumber(documentNumber);

        _repository.Add(item);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(item.Id), actor,
            $"Item '{item.ItemCode}' ({item.ItemName}) created.", AuditSource);

        return ToDto(item);
    }

    public async Task<ItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetAsync(id, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<(IReadOnlyList<ItemDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<ItemDto> UpdateAsync(
        Guid id, UpdateItemRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ItemSecurity.MaintainPrivilegeKey);
        var item = await RequireItemAsync(id, cancellationToken);

        item.UpdateItemName(request.ItemName);
        item.UpdateItemNameArabic(request.ItemNameArabic);
        item.UpdateUnitOfMeasure(request.UnitOfMeasure);
        if (request.IsActive) item.Activate(); else item.Deactivate();

        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(item.Id), actor,
            $"Item '{item.ItemCode}' updated.",
            new[]
            {
                new FieldValueChange("ItemName", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.ItemName)),
                new FieldValueChange("UnitOfMeasure", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.UnitOfMeasure)),
                new FieldValueChange("IsActive", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(request.IsActive)),
            },
            AuditSource);

        return ToDto(item);
    }

    public async Task<ItemDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ItemSecurity.MaintainPrivilegeKey);
        var item = await RequireItemAsync(id, cancellationToken);
        var fromStatus = item.Status;
        item.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(item.Id), actor,
            $"Item '{item.ItemCode}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(item.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(ItemWorkflow.BusinessObjectType, ItemWorkflow.SubmitTransition, item.Id);
        if (instance is null) { await ApproveInternalAsync(item, actor, cancellationToken); return ToDto(item); }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(item, actor, cancellationToken);

        return ToDto(item);
    }

    public Task<ItemDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<ItemDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<ItemDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, ItemSecurity.ApprovePrivilegeKey);
        var item = await RequireItemAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(ItemWorkflow.BusinessObjectType, item.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Item {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(item, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(item, actor, cancellationToken);

        return ToDto(item);
    }

    private async Task ApproveInternalAsync(Item item, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = item.Status;
        item.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(item.Id), actor,
            $"Item '{item.ItemCode}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(item.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Item item, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = item.Status;
        item.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(item.Id), actor,
            $"Item '{item.ItemCode}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(item.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid itemId) => new(itemId, AuditTargetType, "Self");

    private async Task<Item> RequireItemAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Item {id} was not found.");

    private static ItemDto ToDto(Item i) => new(
        i.Id, i.DocumentNumber, i.Status.ToString(), i.ItemCode, i.ItemName, i.ItemNameArabic,
        i.ItemType.ToString(), i.UnitOfMeasure, i.IsActive, i.CreatedAt, i.CreatedBy);
}
