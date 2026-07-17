using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class ARInvoiceServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly DateOnly InvoiceDate = new(2026, 7, 17);

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid NonClientPartnerId = Guid.NewGuid();
    private static readonly Guid UnapprovedCustomerId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid ReceivableAccountId = Guid.NewGuid();
    private static readonly Guid VatAccountId = Guid.NewGuid();
    private static readonly Guid InactiveAccountId = Guid.NewGuid();
    private static readonly Guid TaxCodeId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { ARInvoiceSecurity.MaintainerRoleKey, JournalEntrySecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { ARInvoiceWorkflow.ApproverRoleKey, JournalEntryWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(RevenueAccountId, "4000", "Construction Revenue", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(ReceivableAccountId, "1210", "Accounts Receivable", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(VatAccountId, "2200", "VAT Payable", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(InactiveAccountId, "9999", "Inactive", "Debit", IsPostable: true, IsActive: false));
        return lookup;
    }

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(CustomerId, "Al Riyadh Tower Owner Co", "شركة مالك برج الرياض", new[] { "Client" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(NonClientPartnerId, "Supplier Only Co", null, new[] { "Supplier" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(UnapprovedCustomerId, "Unapproved Client Co", null, new[] { "Client" }, "Draft"));
        return lookup;
    }

    private static FakeTaxCodeLookup BuildTaxCodeLookup()
    {
        var lookup = new FakeTaxCodeLookup();
        lookup.Add(new TaxCodeSummary(TaxCodeId, "VAT15", "Standard VAT 15%", 15m, "Standard", IsActive: true));
        return lookup;
    }

    private static (ARInvoiceService Invoices, JournalEntryService Journal, FakeARInvoiceRepository InvoiceRepo, FakeJournalEntryRepository JournalRepo)
        BuildServices()
    {
        var invoiceRepo = new FakeARInvoiceRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var auditLog = new InMemoryAuditLog();
        var auditRecorder = new AuditRecorder(auditLog);
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[]
        {
            JournalEntryWorkflow.SubmitApprovalDefinition,
            ARInvoiceWorkflow.SubmitApprovalDefinition,
        });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole, ARInvoiceSecurity.MaintainerRole, ARInvoiceSecurity.ApproverRole },
            new[] { JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty, ARInvoiceSecurity.MaintainerDuty, ARInvoiceSecurity.ApproverDuty });
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
            new NumberRangeDefinition(ARInvoiceService.NumberRangeKey, "FIN", "AR")
        });
        var invoiceService = new ARInvoiceService(
            invoiceRepo, invoiceNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), glLookup, costCenterLookup,
            BuildTaxCodeLookup(), journalEntryService, new FakeCustomerReceiptRepository());

        return (invoiceService, journalEntryService, invoiceRepo, journalRepo);
    }

    private static CreateARInvoiceRequest ValidRequest() => new(
        CustomerId, "PO-99887", InvoiceDate, "IPC billing", RevenueAccountId, ReceivableAccountId, 1000m);

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var (invoices, _, _, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"FIN-AR-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(1000m, created.NetAmount);
        Assert.Equal(1000m, created.GrossAmount);
        Assert.Equal(0m, created.OutstandingBalance);
    }

    [Fact]
    public async Task Create_rejects_a_partner_that_is_not_a_client()
    {
        var (invoices, _, _, _) = BuildServices();
        var request = ValidRequest() with { CustomerId = NonClientPartnerId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_customer_that_is_not_Approved()
    {
        var (invoices, _, _, _) = BuildServices();
        var request = ValidRequest() with { CustomerId = UnapprovedCustomerId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_inactive_revenue_account()
    {
        var (invoices, _, _, _) = BuildServices();
        var request = ValidRequest() with { RevenueAccountId = InactiveAccountId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_with_a_tax_code_requires_a_VAT_account()
    {
        var (invoices, _, _, _) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId };

        await Assert.ThrowsAsync<ArgumentException>(() => invoices.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_with_a_tax_code_and_VAT_account_snapshots_the_rate()
    {
        var (invoices, _, _, _) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId, VatAccountId = VatAccountId };

        var created = await invoices.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal(15m, created.TaxRate);
        Assert.Equal(150m, created.TaxAmount);
        Assert.Equal(1150m, created.GrossAmount);
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var (invoices, _, _, _) = BuildServices();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            invoices.CreateAsync(ValidRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task Submit_approve_post_generates_a_balanced_linked_journal_entry_with_the_AR_debit_credit_direction()
    {
        var (invoices, _, _, journalRepo) = BuildServices();
        var request = ValidRequest() with { TaxCodeId = TaxCodeId, VatAccountId = VatAccountId };
        var created = await invoices.CreateAsync(request, "ahmer.bilal", "C001");

        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        Assert.Equal("Posted", posted.Status);
        Assert.NotNull(posted.LinkedJournalEntryId);
        Assert.Equal(1150m, posted.OutstandingBalance);

        var allEntries = await journalRepo.ListAsync(0, 100);
        var journalEntry = allEntries.Single(e => e.Id == posted.LinkedJournalEntryId);
        Assert.Equal("Posted", journalEntry.Status.ToString());
        Assert.True(journalEntry.IsBalanced);
        Assert.Equal(1150m, journalEntry.TotalDebits); // Receivable, gross
        Assert.Equal(1150m, journalEntry.TotalCredits); // Revenue 1000 + VAT 150
        Assert.Equal(3, journalEntry.Lines.Count); // Receivable, Revenue, VAT

        // AR's own debit line is the Receivable (Gross) — the mirror of AP's Payable credit line.
        var receivableLine = journalEntry.Lines.Single(l => l.GLAccountId == ReceivableAccountId);
        Assert.Equal(1150m, receivableLine.DebitAmount);
        Assert.Equal(0m, receivableLine.CreditAmount);

        var vatLine = journalEntry.Lines.Single(l => l.GLAccountId == VatAccountId);
        Assert.Equal(0m, vatLine.DebitAmount);
        Assert.Equal(150m, vatLine.CreditAmount); // VAT Output credited, not debited like AP's VAT Input
    }

    [Fact]
    public async Task PostAsync_without_tax_generates_a_two_line_entry()
    {
        var (invoices, _, _, journalRepo) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        var allEntries = await journalRepo.ListAsync(0, 100);
        var journalEntry = allEntries.Single(e => e.Id == posted.LinkedJournalEntryId);
        Assert.Equal(2, journalEntry.Lines.Count); // Receivable, Revenue only
        Assert.Equal(1000m, journalEntry.TotalDebits);
        Assert.Equal(1000m, journalEntry.TotalCredits);
    }

    [Fact]
    public async Task ReverseAsync_reverses_the_linked_journal_entry_and_the_invoice()
    {
        var (invoices, journal, _, journalRepo) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await invoices.SubmitAsync(created.Id, "ahmer.bilal");
        await invoices.ApproveAsync(created.Id, "finance.manager");
        var posted = await invoices.PostAsync(created.Id, "finance.manager");

        var reversed = await invoices.ReverseAsync(posted.Id, "finance.manager", InvoiceDate.AddDays(1));
        Assert.Equal("Reversed", reversed.Status);
        Assert.Equal(0m, reversed.OutstandingBalance);

        var originalEntry = await journal.GetAsync(posted.LinkedJournalEntryId!.Value);
        Assert.Equal("Reversed", originalEntry!.Status);

        var allEntries = await journalRepo.ListAsync(0, 100);
        Assert.Equal(2, allEntries.Count); // original + mirror
    }

    [Fact]
    public async Task ReverseAsync_throws_if_the_invoice_has_no_linked_journal_entry()
    {
        var (invoices, _, _, _) = BuildServices();
        var created = await invoices.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            invoices.ReverseAsync(created.Id, "finance.manager", InvoiceDate));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var (invoices, _, _, _) = BuildServices();
        Assert.Null(await invoices.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task OutstandingBalance_reduces_after_a_Posted_customer_receipt_allocates_against_it()
    {
        var invoiceRepo = new FakeARInvoiceRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var receiptRepo = new FakeCustomerReceiptRepository();
        var bankAccountRepo = new FakeBankAccountRepository();
        var auditRecorder = new AuditRecorder(new InMemoryAuditLog());
        var workflowInstances = new FakeWorkflowInstanceRepository();
        var bankGLAccountId = Guid.NewGuid();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[]
        {
            JournalEntryWorkflow.SubmitApprovalDefinition,
            ARInvoiceWorkflow.SubmitApprovalDefinition,
            CustomerReceiptWorkflow.SubmitApprovalDefinition,
        });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[]
            {
                JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole,
                ARInvoiceSecurity.MaintainerRole, ARInvoiceSecurity.ApproverRole,
                CustomerReceiptSecurity.MaintainerRole, CustomerReceiptSecurity.ApproverRole,
            },
            new[]
            {
                JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty,
                ARInvoiceSecurity.MaintainerDuty, ARInvoiceSecurity.ApproverDuty,
                CustomerReceiptSecurity.MaintainerDuty, CustomerReceiptSecurity.ApproverDuty,
            });
        var authorizationService = new AuthorizationService(securityCatalog);
        var actorRoles = new InMemoryActorRoleAssignmentStore(new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { ARInvoiceSecurity.MaintainerRoleKey, JournalEntrySecurity.MaintainerRoleKey, CustomerReceiptSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { ARInvoiceWorkflow.ApproverRoleKey, JournalEntryWorkflow.ApproverRoleKey, CustomerReceiptWorkflow.ApproverRoleKey },
        });

        var glLookup = BuildGLAccountLookup();
        glLookup.Add(new GLAccountSummary(bankGLAccountId, "1010", "Bank - Al Rajhi Current", "Debit", IsPostable: true, IsActive: true));
        var costCenterLookup = new FakeCostCenterLookup();

        var journalNumberRanges = new InMemoryNumberRangeService(new[] { new NumberRangeDefinition(JournalEntryService.NumberRangeKey, "FIN", "JE") });
        var journalEntryService = new JournalEntryService(
            journalRepo, journalNumberRanges, auditRecorder, workflowEngine, workflowInstances, authorizationService, actorRoles, glLookup, costCenterLookup);

        var invoiceNumberRanges = new InMemoryNumberRangeService(new[] { new NumberRangeDefinition(ARInvoiceService.NumberRangeKey, "FIN", "AR") });
        var invoiceService = new ARInvoiceService(
            invoiceRepo, invoiceNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), glLookup, costCenterLookup,
            BuildTaxCodeLookup(), journalEntryService, receiptRepo);

        var receiptNumberRanges = new InMemoryNumberRangeService(new[] { new NumberRangeDefinition(CustomerReceiptService.NumberRangeKey, "FIN", "CR") });
        var receiptService = new CustomerReceiptService(
            receiptRepo, invoiceRepo, bankAccountRepo, receiptNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), new FakeLookupCatalog(), journalEntryService);

        var invoice = await invoiceService.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await invoiceService.SubmitAsync(invoice.Id, "ahmer.bilal");
        await invoiceService.ApproveAsync(invoice.Id, "finance.manager");
        var postedInvoice = await invoiceService.PostAsync(invoice.Id, "finance.manager");
        Assert.Equal(1000m, postedInvoice.OutstandingBalance);

        var bankAccount = new BankAccount("ahmer.bilal", "BANK-001", "Al Rajhi Current Account", "Al Rajhi Bank", bankGLAccountId);
        bankAccount.Submit("ahmer.bilal");
        bankAccount.Approve("finance.manager");
        bankAccountRepo.Add(bankAccount);

        var receiptRequest = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, InvoiceDate.AddDays(1), "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 400m) });
        var receipt = await receiptService.CreateAsync(receiptRequest, "ahmer.bilal", "C001");
        await receiptService.SubmitAsync(receipt.Id, "ahmer.bilal");
        await receiptService.ApproveAsync(receipt.Id, "finance.manager");
        await receiptService.PostAsync(receipt.Id, "finance.manager");

        var reloaded = await invoiceService.GetAsync(invoice.Id);
        Assert.Equal(600m, reloaded!.OutstandingBalance);
    }
}
