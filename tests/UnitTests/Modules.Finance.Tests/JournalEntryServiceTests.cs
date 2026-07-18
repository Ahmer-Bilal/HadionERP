using Modules.Finance.Application;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class JournalEntryServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly DateOnly PostingDate = new(2026, 7, 14);
    private static readonly Guid CashAccountId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid InactiveAccountId = Guid.NewGuid();
    private static readonly Guid HeaderAccountId = Guid.NewGuid();
    private static readonly Guid CostCenterId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { JournalEntrySecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { JournalEntryWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(CashAccountId, "1010", "Cash on Hand", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(RevenueAccountId, "4000", "Consulting Revenue", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(InactiveAccountId, "9999", "Inactive Account", "Debit", IsPostable: true, IsActive: false));
        lookup.Add(new GLAccountSummary(HeaderAccountId, "1000", "Current Assets", "Debit", IsPostable: false, IsActive: true));
        return lookup;
    }

    private static FakeCostCenterLookup BuildCostCenterLookup()
    {
        var lookup = new FakeCostCenterLookup();
        lookup.Add(new CostCenterSummary(CostCenterId, "CC-1000", "Head Office", IsPostable: true, IsActive: true));
        return lookup;
    }

    private static JournalEntryService BuildService(out FakeJournalEntryRepository repository) =>
        BuildService(out repository, out _);

    private static JournalEntryService BuildService(out FakeJournalEntryRepository repository, out IAuditLog auditLog) =>
        BuildService(out repository, out auditLog, new FakeFiscalYearRepository());

    private static JournalEntryService BuildService(
        out FakeJournalEntryRepository repository, out IAuditLog auditLog, IFiscalYearRepository fiscalYearRepository)
    {
        repository = new FakeJournalEntryRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(JournalEntryService.NumberRangeKey, "FIN", "JE")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { JournalEntryWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole },
            new[] { JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty });

        return new JournalEntryService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(),
            BuildGLAccountLookup(), BuildCostCenterLookup(), fiscalYearRepository);
    }

    private static CreateJournalEntryRequest BalancedRequest() => new(
        PostingDate, "Consulting revenue received in cash",
        new[]
        {
            new CreateJournalLineRequest(CashAccountId, 1000, 0),
            new CreateJournalLineRequest(RevenueAccountId, 0, 1000),
        });

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"FIN-JE-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.True(created.IsBalanced);
        Assert.Equal(2, created.Lines.Count);
    }

    [Fact]
    public async Task Create_tags_a_human_created_entry_as_Manual_with_no_source_document_id()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");

        Assert.Equal(JournalEntrySourceDocumentTypes.Manual, created.SourceDocumentType);
        Assert.Null(created.SourceDocumentId);
    }

    [Fact]
    public async Task CreateSystemGeneratedAsync_carries_the_callers_source_document_through()
    {
        var service = BuildService(out _);
        var sourceId = Guid.NewGuid();
        var lines = new[] { new CreateJournalLineRequest(CashAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) };

        var created = await service.CreateSystemGeneratedAsync(
            PostingDate, "AP Invoice AP-2026-000001", lines, reversalOfEntryId: null, "system",
            sourceDocumentType: JournalEntrySourceDocumentTypes.APInvoice, sourceDocumentId: sourceId);

        Assert.Equal(JournalEntrySourceDocumentTypes.APInvoice, created.SourceDocumentType);
        Assert.Equal(sourceId, created.SourceDocumentId);
    }

    [Fact]
    public async Task ReverseAsync_carries_the_originals_source_document_onto_the_mirror()
    {
        var service = BuildService(out _);
        var sourceId = Guid.NewGuid();
        var lines = new[] { new CreateJournalLineRequest(CashAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) };
        var original = await service.CreateSystemGeneratedAsync(
            PostingDate, "AP Invoice AP-2026-000001", lines, reversalOfEntryId: null, "system",
            sourceDocumentType: JournalEntrySourceDocumentTypes.APInvoice, sourceDocumentId: sourceId);

        await service.ReverseAsync(original.Id, "finance.manager", PostingDate);

        var (allEntries, _) = await service.ListAsync(0, 10);
        var mirror = allEntries.Single(e => e.Id != original.Id);

        Assert.Equal(JournalEntrySourceDocumentTypes.APInvoice, mirror.SourceDocumentType);
        Assert.Equal(sourceId, mirror.SourceDocumentId);
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_an_unknown_GL_account()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[] { new CreateJournalLineRequest(Guid.NewGuid(), 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_an_inactive_GL_account()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[] { new CreateJournalLineRequest(InactiveAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_line_referencing_a_non_postable_header_account()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[] { new CreateJournalLineRequest(HeaderAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_accepts_a_valid_cost_center_reference()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[]
            {
                new CreateJournalLineRequest(CashAccountId, 100, 0, CostCenterId),
                new CreateJournalLineRequest(RevenueAccountId, 0, 100),
            });

        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");
        Assert.Equal(CostCenterId, created.Lines[0].CostCenterId);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_cost_center()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[]
            {
                new CreateJournalLineRequest(CashAccountId, 100, 0, Guid.NewGuid()),
                new CreateJournalLineRequest(RevenueAccountId, 0, 100),
            });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "JournalEntry", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(BalancedRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task SubmitAsync_rejects_an_unbalanced_entry()
    {
        var service = BuildService(out _);
        var request = new CreateJournalEntryRequest(PostingDate, "Test",
            new[] { new CreateJournalLineRequest(CashAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 50) });
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task Submit_approve_post_reaches_posted_with_workflow()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        var posted = await service.PostAsync(created.Id, "finance.manager");
        Assert.Equal("Posted", posted.Status);
    }

    [Fact]
    public async Task PostAsync_throws_for_an_actor_with_no_Approver_role()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.PostAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task ReverseAsync_creates_a_mirror_entry_with_flipped_debit_credit_and_marks_the_original_reversed()
    {
        var service = BuildService(out var repository);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");
        await service.PostAsync(created.Id, "finance.manager");

        var reversed = await service.ReverseAsync(created.Id, "finance.manager", PostingDate.AddDays(1));
        Assert.Equal("Reversed", reversed.Status);

        var (allEntries, total) = await service.ListAsync(0, 10);
        Assert.Equal(2, total);
        var mirror = allEntries.Single(e => e.Id != created.Id);

        Assert.Equal("Posted", mirror.Status);
        Assert.Equal(created.Id, mirror.ReversalOfEntryId);
        Assert.True(mirror.IsBalanced);
        var mirrorCashLine = mirror.Lines.Single(l => l.GLAccountId == CashAccountId);
        var mirrorRevenueLine = mirror.Lines.Single(l => l.GLAccountId == RevenueAccountId);
        Assert.Equal(1000, mirrorCashLine.CreditAmount); // was a debit on the original
        Assert.Equal(0, mirrorCashLine.DebitAmount);
        Assert.Equal(1000, mirrorRevenueLine.DebitAmount); // was a credit on the original
        Assert.Equal(0, mirrorRevenueLine.CreditAmount);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PostAsync_is_blocked_when_the_posting_dates_period_is_closed()
    {
        var fiscalYears = new FakeFiscalYearRepository();
        var year = new Modules.Finance.Domain.FiscalYear("finance.manager", PostingDate.Year);
        var period = year.Periods.Single(p => p.PeriodNumber == PostingDate.Month);
        period.Close("finance.manager");
        fiscalYears.Add(year);

        var service = BuildService(out _, out _, fiscalYears);
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PostAsync(created.Id, "finance.manager"));
    }

    [Fact]
    public async Task PostAsync_succeeds_when_no_fiscal_year_covers_the_posting_date_at_all()
    {
        // No FiscalYear on file for PostingDate.Year — opt-in enforcement, same as RealBudgetCheckService's
        // "no budget on file" reasoning.
        var service = BuildService(out _, out _, new FakeFiscalYearRepository());
        var created = await service.CreateAsync(BalancedRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        var posted = await service.PostAsync(created.Id, "finance.manager");
        Assert.Equal("Posted", posted.Status);
    }

    [Fact]
    public async Task CreateSystemGeneratedAsync_is_blocked_when_the_posting_dates_period_is_closed()
    {
        var fiscalYears = new FakeFiscalYearRepository();
        var year = new Modules.Finance.Domain.FiscalYear("finance.manager", PostingDate.Year);
        var period = year.Periods.Single(p => p.PeriodNumber == PostingDate.Month);
        period.Close("finance.manager");
        fiscalYears.Add(year);

        var service = BuildService(out _, out _, fiscalYears);
        var lines = new[] { new CreateJournalLineRequest(CashAccountId, 100, 0), new CreateJournalLineRequest(RevenueAccountId, 0, 100) };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSystemGeneratedAsync(PostingDate, "AP Invoice test", lines, reversalOfEntryId: null, "system"));
    }
}
