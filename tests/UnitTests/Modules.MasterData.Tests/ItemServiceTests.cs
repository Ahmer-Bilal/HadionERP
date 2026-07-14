using Modules.MasterData.Application;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.MasterData.Tests;

public class ItemServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { ItemSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { ItemWorkflow.ApproverRoleKey },
        });

    private static ItemService BuildService(out FakeItemRepository repository) =>
        BuildService(out repository, out _);

    private static ItemService BuildService(out FakeItemRepository repository, out IAuditLog auditLog)
    {
        repository = new FakeItemRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(ItemService.NumberRangeKey, "MD", "ITM")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { ItemWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { ItemSecurity.MaintainerRole, ItemSecurity.ApproverRole },
            new[] { ItemSecurity.MaintainerDuty, ItemSecurity.ApproverDuty });

        return new ItemService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    private static CreateItemRequest ValidRequest(string code = "MAT-1010") =>
        new(code, "Portland Cement 42.5N", "Stock", "TON");

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"MD-ITM-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("MAT-1010", created.ItemCode);
        Assert.Equal("Stock", created.ItemType);
        Assert.Equal("TON", created.UnitOfMeasure);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_item_code()
    {
        var service = BuildService(out _);
        await service.CreateAsync(ValidRequest("MAT-1010"), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest("MAT-1010"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_invalid_item_type()
    {
        var service = BuildService(out _);
        var request = new CreateItemRequest("MAT-1010", "Cement", "NotAType", "TON");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "Item", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved_with_workflow()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var approved = await service.ApproveAsync(created.Id, "finance.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task ApproveAsync_throws_for_an_actor_with_no_Approver_role()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(ValidRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task Update_changes_name_and_persists()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var updated = await service.UpdateAsync(created.Id,
            new UpdateItemRequest("Portland Cement 52.5N", "TON", null, true), "ahmer.bilal");

        Assert.Equal("Portland Cement 52.5N", updated.ItemName);
    }

    [Fact]
    public async Task CreateAsync_persists_the_Arabic_name_when_provided()
    {
        var service = BuildService(out _);
        var request = new CreateItemRequest("MAT-1010", "Portland Cement", "Stock", "TON", "أسمنت بورتلاندي");

        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("أسمنت بورتلاندي", created.ItemNameArabic);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
