using Modules.Finance.Application;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class APInvoiceServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);

    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid NonVendorPartnerId = Guid.NewGuid();
    private static readonly Guid UnapprovedVendorId = Guid.NewGuid();
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();
    private static readonly Guid PayableAccountId = Guid.NewGuid();
    private static readonly Guid VatAccountId = Guid.NewGuid();
    private static readonly Guid InactiveAccountId = Guid.NewGuid();
    private static readonly Guid TaxCodeId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { APInvoiceSecurity.MaintainerRoleKey, JournalEntrySecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { APInvoiceWorkflow.ApproverRoleKey, JournalEntryWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(ExpenseAccountId, "5000", "Office Supplies Expense", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(PayableAccountId, "2010", "Accounts Payable", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(VatAccountId, "1200", "VAT Recoverable", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(InactiveAccountId, "9999", "Inactive", "Debit", IsPostable: true, IsActive: false));
        return lookup;
    }

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(VendorId, "Gulf Falcon Trading Co", "شركة صقر الخليج", new[] { "Supplier" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(NonVendorPartnerId, "Client Only Co", null, new[] { "Client" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(UnapprovedVendorId, "Unapproved Vendor Co", null, new[] { "Supplier" }, "Draft"));
        return lookup;
    }

    private static FakeTaxCodeLookup BuildTaxCodeLookup()
    {
        var lookup = new FakeTaxCodeLookup();
        lookup.Add(new TaxCodeSummary(TaxCodeId, "VAT15", "Standard VAT 15%", 15m, "Standard", IsActive: true));
        return lookup;
    }

    private static (APInvoiceService Invoices, JournalEntryService Journal, FakeAPInvoiceRepository InvoiceRepo, FakeJournalEntryRepository JournalRepo, IAuditLog AuditLog)
        BuildServices()
    {
        var invoiceRepo = new FakeAPInvoiceRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var auditLog = new InMemoryAuditLog();
        var auditRecorder = new AuditRecorder(auditLog);
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[]
        {
            JournalEntryWorkflow.SubmitApprovalDefinition,
            APInvoiceWorkflow.SubmitApprovalDefinition,
        });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole, APInvoiceSecurity.MaintainerRole, APInvoiceSecurity.ApproverRole },
            new[] { JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty, APInvoiceSecurity.MaintainerDuty, APInvoiceSecurity.ApproverDuty });
        var authorizationService = new AuthorizationService(securityCatalog);
        var actorRoles = BuildActorRoles();

        var glLookup = BuildGLAccountLookup();
        var costCenterLookup = new FakeCostCenterLookup();

        var journalNumberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(JournalEntryService.NumberRangeKey, "FIN", "JE")
        });
        var journalEntryService = new JournalEntryService(
            journalRepo, journalNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, glLookup, costCenterLookup);

        var invoiceNumberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(APInvoiceService.NumberRangeKey, "FIN", "AP")
        });
        var invoiceService = new APInvoiceService(
            invoiceRepo, invoiceNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), glLookup, costCenterLookup,
            BuildTaxCodeLookup(), journalEntryService);

        return (invoiceService, journalEntryService, invoiceRepo, journalRepo, auditLog);
    }

    private static CreateAPInvoiceRequest ValidRequest() => new(
        VendorId, "INV-2026-0456", InvoiceDate, "Office supplies", ExpenseAccountId, PayableAccountId, 1000m);

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"FIN-AP-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(1000m, created.NetAmount);
        Assert.Equal(1000m, created.GrossAmount);
    }

    [Fact]
    public async Task Create_rejects_a_partner_that_is_not_a_vendor()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var request = ValidRequest() with { VendorId = NonVendorPartnerId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_vendor_that_is_not_Approved()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var request = ValidRequest() with { VendorId = UnapprovedVendorId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_inactive_expense_account()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var request = ValidRequest() with { ExpenseAccountId = InactiveAccountId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_with_a_tax_code_requires_a_VAT_account()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_with_a_tax_code_and_VAT_account_snapshots_the_rate()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId, VatAccountId = VatAccountId };

        var created = await invoices.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal(15m, created.TaxRate);
        Assert.Equal(150m, created.TaxAmount);
        Assert.Equal(1150m, created.GrossAmount);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var (invoices, _, _, _, _) = BuildServices();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            invoices.CreateAsync(ValidRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task Submit_approve_post_generates_a_balanced_linked_journal_entry()
    {
        var (invoices, _, _, journalRepo, _) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId, VatAccountId = VatAccountId };
        var created = await invoices.CreateAsync(request, "ahmer.bilal", "C001");

        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        Assert.Equal("Posted", posted.Status);
        Assert.NotNull(posted.LinkedJournalEntryId);

        var allEntries = await GetAllAsync(journalRepo);
        var journalEntry = allEntries.Single(e => e.Id == posted.LinkedJournalEntryId);
        Assert.Equal("Posted", journalEntry.Status.ToString());
        Assert.True(journalEntry.IsBalanced);
        Assert.Equal(1150m, journalEntry.TotalDebits); // Net 1000 + VAT 150
        Assert.Equal(1150m, journalEntry.TotalCredits); // Payable, gross
        Assert.Equal(3, journalEntry.Lines.Count); // Expense, VAT, Payable
    }

    private static async Task<List<Modules.Finance.Domain.JournalEntry>> GetAllAsync(FakeJournalEntryRepository repo)
    {
        var items = await repo.ListAsync(0, 100);
        return items.ToList();
    }

    [Fact]
    public async Task PostAsync_without_tax_generates_a_two_line_entry()
    {
        var (invoices, _, _, journalRepo, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        var allEntries = await GetAllAsync(journalRepo);
        var journalEntry = allEntries.Single(e => e.Id == posted.LinkedJournalEntryId);
        Assert.Equal(2, journalEntry.Lines.Count); // Expense, Payable only
        Assert.Equal(1000m, journalEntry.TotalDebits);
        Assert.Equal(1000m, journalEntry.TotalCredits);
    }

    [Fact]
    public async Task ReverseAsync_reverses_the_linked_journal_entry_and_the_invoice()
    {
        var (invoices, journal, _, journalRepo, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        var reversed = await invoices.ReverseAsync(posted.Id, "finance.manager", InvoiceDate.AddDays(1));
        Assert.Equal("Reversed", reversed.Status);

        var originalEntry = await journal.GetAsync(posted.LinkedJournalEntryId!.Value);
        Assert.Equal("Reversed", originalEntry!.Status);

        var allEntries = await GetAllAsync(journalRepo);
        Assert.Equal(2, allEntries.Count); // original + mirror
    }

    [Fact]
    public async Task ReverseAsync_throws_if_the_invoice_has_no_linked_journal_entry()
    {
        var (invoices, _, _, _, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoices.ReverseAsync(created.Id, "finance.manager", InvoiceDate));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var (invoices, _, _, _, _) = BuildServices();
        Assert.Null(await invoices.GetAsync(Guid.NewGuid()));
    }
}
