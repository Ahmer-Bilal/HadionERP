using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class CustomerReceiptServiceTests
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);
    private static readonly DateOnly ReceiptDate = new(2026, 7, 15);

    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid ReceivableAccountId = Guid.NewGuid();
    private static readonly Guid BankGLAccountId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { CustomerReceiptSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { CustomerReceiptWorkflow.ApproverRoleKey, JournalEntryWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(RevenueAccountId, "4000", "Construction Revenue", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(ReceivableAccountId, "1210", "Accounts Receivable", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(BankGLAccountId, "1010", "Bank - Al Rajhi Current", "Debit", IsPostable: true, IsActive: true));
        return lookup;
    }

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(CustomerId, "Al Riyadh Tower Owner Co", "شركة مالك برج الرياض", new[] { "Client" }, "Approved"));
        return lookup;
    }

    /// <summary>Builds a Posted AR Invoice directly against the fake repository, bypassing ARInvoiceService —
    /// CustomerReceiptService only ever reads status/ReceivableAccountId/GrossAmount off an invoice, so driving
    /// the domain object's own lifecycle methods directly is sufficient and avoids wiring a second full service.</summary>
    private static ARInvoice AddPostedInvoice(FakeARInvoiceRepository invoiceRepo, decimal netAmount, string customerReference = "PO-0001")
    {
        var invoice = new ARInvoice("ahmer.bilal", CustomerId, customerReference, InvoiceDate, "IPC billing", RevenueAccountId, ReceivableAccountId, netAmount);
        invoice.Submit("ahmer.bilal");
        invoice.Approve("finance.manager");
        invoice.Post("finance.manager");
        invoiceRepo.Add(invoice);
        return invoice;
    }

    private static BankAccount AddApprovedBankAccount(FakeBankAccountRepository bankAccountRepo)
    {
        var bankAccount = new BankAccount("ahmer.bilal", "BANK-001", "Al Rajhi Current Account", "Al Rajhi Bank", BankGLAccountId);
        bankAccount.Submit("ahmer.bilal");
        bankAccount.Approve("finance.manager");
        bankAccountRepo.Add(bankAccount);
        return bankAccount;
    }

    private static (CustomerReceiptService Receipts, FakeARInvoiceRepository InvoiceRepo, FakeBankAccountRepository BankAccountRepo, FakeCustomerReceiptRepository ReceiptRepo, FakeJournalEntryRepository JournalRepo)
        BuildServices()
    {
        var invoiceRepo = new FakeARInvoiceRepository();
        var bankAccountRepo = new FakeBankAccountRepository();
        var receiptRepo = new FakeCustomerReceiptRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var auditRecorder = new AuditRecorder(new InMemoryAuditLog());
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[]
        {
            JournalEntryWorkflow.SubmitApprovalDefinition,
            CustomerReceiptWorkflow.SubmitApprovalDefinition,
        });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole, CustomerReceiptSecurity.MaintainerRole, CustomerReceiptSecurity.ApproverRole },
            new[] { JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty, CustomerReceiptSecurity.MaintainerDuty, CustomerReceiptSecurity.ApproverDuty });
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
            authorizationService, actorRoles, glLookup, costCenterLookup, new FakeFiscalYearRepository());

        var receiptNumberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(CustomerReceiptService.NumberRangeKey, "FIN", "CR")
        });
        var receiptService = new CustomerReceiptService(
            receiptRepo, invoiceRepo, bankAccountRepo, receiptNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), new FakeLookupCatalog(), journalEntryService);

        return (receiptService, invoiceRepo, bankAccountRepo, receiptRepo, journalRepo);
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_computes_amount_from_allocations()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 1000m) });
        var created = await receipts.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("Draft", created.Status);
        Assert.Equal(1000m, created.Amount);
        Assert.Single(created.Allocations);
        Assert.NotNull(created.DocumentNumber);
    }

    [Fact]
    public async Task Create_rejects_an_allocation_that_exceeds_the_invoice_outstanding_balance()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 1500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => receipts.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_payment_method()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "Crypto",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => receipts.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_non_Approved_bank_account()
    {
        var (receipts, invoiceRepo, _, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var draftBankAccount = new BankAccount("ahmer.bilal", "BANK-002", "Draft Account", "SNB", BankGLAccountId);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, draftBankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => receipts.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            receipts.CreateAsync(request, "finance.manager", "C001"));
    }

    [Fact]
    public async Task Submit_approve_post_generates_a_balanced_linked_journal_entry_Dr_Bank_Cr_Receivable()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, journalRepo) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 1000m) }, "First installment");
        var created = await receipts.CreateAsync(request, "ahmer.bilal", "C001");

        await receipts.SubmitAsync(created.Id, "ahmer.bilal");
        await receipts.ApproveAsync(created.Id, "finance.manager");
        var posted = await receipts.PostAsync(created.Id, "finance.manager");

        Assert.Equal("Posted", posted.Status);
        Assert.NotNull(posted.LinkedJournalEntryId);

        var allEntries = await journalRepo.ListAsync(0, 100);
        var journalEntry = allEntries.Single(e => e.Id == posted.LinkedJournalEntryId);
        Assert.True(journalEntry.IsBalanced);

        var bankLine = journalEntry.Lines.Single(l => l.GLAccountId == BankGLAccountId);
        Assert.Equal(1000m, bankLine.DebitAmount);
        Assert.Equal(0m, bankLine.CreditAmount);

        var receivableLine = journalEntry.Lines.Single(l => l.GLAccountId == ReceivableAccountId);
        Assert.Equal(0m, receivableLine.DebitAmount);
        Assert.Equal(1000m, receivableLine.CreditAmount);
    }

    [Fact]
    public async Task Two_receipts_against_the_same_invoice_cannot_together_exceed_its_Gross_Amount()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var firstRequest = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 600m) });
        var first = await receipts.CreateAsync(firstRequest, "ahmer.bilal", "C001");
        await receipts.SubmitAsync(first.Id, "ahmer.bilal");
        await receipts.ApproveAsync(first.Id, "finance.manager");
        await receipts.PostAsync(first.Id, "finance.manager");

        var secondRequest = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => receipts.CreateAsync(secondRequest, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Reversing_a_receipt_releases_its_allocation_so_a_new_receipt_can_use_it()
    {
        var (receipts, invoiceRepo, bankAccountRepo, _, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var firstRequest = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate, "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 1000m) });
        var first = await receipts.CreateAsync(firstRequest, "ahmer.bilal", "C001");
        await receipts.SubmitAsync(first.Id, "ahmer.bilal");
        await receipts.ApproveAsync(first.Id, "finance.manager");
        var posted = await receipts.PostAsync(first.Id, "finance.manager");

        var reversed = await receipts.ReverseAsync(posted.Id, "finance.manager", ReceiptDate.AddDays(1));
        Assert.Equal("Reversed", reversed.Status);

        var secondRequest = new CreateCustomerReceiptRequest(
            CustomerId, bankAccount.Id, ReceiptDate.AddDays(2), "BankTransfer",
            new[] { new CreateCustomerReceiptAllocationRequest(invoice.Id, 1000m) });
        var second = await receipts.CreateAsync(secondRequest, "ahmer.bilal", "C001");

        Assert.Equal(1000m, second.Amount);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var (receipts, _, _, _, _) = BuildServices();
        Assert.Null(await receipts.GetAsync(Guid.NewGuid()));
    }
}
