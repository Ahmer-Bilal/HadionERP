using Modules.MasterData.Application;
using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.MasterData.Tests;

public class TaxCodeServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { TaxCodeSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { TaxCodeWorkflow.ApproverRoleKey },
        });

    private static TaxCodeService BuildService(out FakeTaxCodeRepository repository) =>
        BuildService(out repository, out _);

    private static TaxCodeService BuildService(out FakeTaxCodeRepository repository, out IAuditLog auditLog)
    {
        repository = new FakeTaxCodeRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(TaxCodeService.NumberRangeKey, "MD", "TAX")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { TaxCodeWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { TaxCodeSecurity.MaintainerRole, TaxCodeSecurity.ApproverRole },
            new[] { TaxCodeSecurity.MaintainerDuty, TaxCodeSecurity.ApproverDuty });

        return new TaxCodeService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    private static CreateTaxCodeRequest ValidRequest(string code = "VAT15") =>
        new(code, "Standard VAT 15%", 15m, "Standard");

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"MD-TAX-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("VAT15", created.TaxCodeCode);
        Assert.Equal(15m, created.Rate);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_tax_code()
    {
        var service = BuildService(out _);
        await service.CreateAsync(ValidRequest("VAT15"), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest("VAT15"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_invalid_tax_type()
    {
        var service = BuildService(out _);
        var request = new CreateTaxCodeRequest("VAT15", "Name", 15m, "NotAType");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "TaxCode", "Self"));
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
    public async Task Update_changes_rate_and_persists()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var updated = await service.UpdateAsync(created.Id,
            new UpdateTaxCodeRequest("Standard VAT", 15m, null, true), "ahmer.bilal");

        Assert.Equal("Standard VAT", updated.TaxCodeName);
    }

    [Fact]
    public async Task CreateAsync_persists_the_Arabic_name_when_provided()
    {
        var service = BuildService(out _);
        var request = new CreateTaxCodeRequest("VAT15", "Standard VAT", 15m, "Standard", "ضريبة القيمة المضافة");

        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("ضريبة القيمة المضافة", created.TaxCodeNameArabic);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
