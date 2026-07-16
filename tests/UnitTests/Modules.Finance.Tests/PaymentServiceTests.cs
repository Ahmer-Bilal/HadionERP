using Modules.Finance.Application;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class PaymentServiceTests
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);
    private static readonly DateOnly PaymentDate = new(2026, 7, 15);

    private static readonly Guid VendorId = Guid.NewGuid();
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();
    private static readonly Guid PayableAccountId = Guid.NewGuid();
    private static readonly Guid BankGLAccountId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { PaymentSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { PaymentWorkflow.ApproverRoleKey, JournalEntryWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(ExpenseAccountId, "5000", "Office Supplies Expense", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(PayableAccountId, "2010", "Accounts Payable", "Credit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(BankGLAccountId, "1010", "Bank - Al Rajhi Current", "Debit", IsPostable: true, IsActive: true));
        return lookup;
    }

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(VendorId, "Gulf Falcon Trading Co", "شركة صقر الخليج", new[] { "Supplier" }, "Approved"));
        return lookup;
    }

    /// <summary>Builds a Posted AP Invoice directly against the fake repository, bypassing APInvoiceService —
    /// PaymentService only ever reads status/PayableAccountId/GrossAmount off an invoice, so driving the
    /// domain object's own lifecycle methods directly is sufficient and avoids wiring a second full service.</summary>
    private static APInvoice AddPostedInvoice(FakeAPInvoiceRepository invoiceRepo, decimal netAmount, string vendorInvoiceNumber = "INV-0001")
    {
        var invoice = new APInvoice("ahmer.bilal", VendorId, vendorInvoiceNumber, InvoiceDate, "Office supplies", ExpenseAccountId, PayableAccountId, netAmount);
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

    private static (PaymentService Payments, FakeAPInvoiceRepository InvoiceRepo, FakeBankAccountRepository BankAccountRepo, FakePaymentRepository PaymentRepo)
        BuildServices()
    {
        var invoiceRepo = new FakeAPInvoiceRepository();
        var bankAccountRepo = new FakeBankAccountRepository();
        var paymentRepo = new FakePaymentRepository();
        var journalRepo = new FakeJournalEntryRepository();
        var auditRecorder = new AuditRecorder(new InMemoryAuditLog());
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[]
        {
            JournalEntryWorkflow.SubmitApprovalDefinition,
            PaymentWorkflow.SubmitApprovalDefinition,
        });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole, PaymentSecurity.MaintainerRole, PaymentSecurity.ApproverRole },
            new[] { JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty, PaymentSecurity.MaintainerDuty, PaymentSecurity.ApproverDuty });
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

        var paymentNumberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(PaymentService.NumberRangeKey, "FIN", "PAY")
        });
        var paymentService = new PaymentService(
            paymentRepo, invoiceRepo, bankAccountRepo, paymentNumberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, actorRoles, BuildBusinessPartnerLookup(), new FakeLookupCatalog(), journalEntryService);

        return (paymentService, invoiceRepo, bankAccountRepo, paymentRepo);
    }

    [Fact]
    public async Task Create_assigns_a_document_number_and_computes_amount_from_allocations()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 1000m) });
        var created = await payments.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal("Draft", created.Status);
        Assert.Equal(1000m, created.Amount);
        Assert.Single(created.Allocations);
        Assert.NotNull(created.DocumentNumber);
    }

    [Fact]
    public async Task Create_rejects_an_allocation_that_exceeds_the_invoice_outstanding_balance()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 1500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => payments.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_payment_method()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "Crypto",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => payments.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_non_Approved_bank_account()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var draftBankAccount = new BankAccount("ahmer.bilal", "BANK-002", "Draft Account", "SNB", BankGLAccountId);
        bankAccountRepo.Add(draftBankAccount);

        var request = new CreatePaymentRequest(
            VendorId, draftBankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => payments.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            payments.CreateAsync(request, "finance.manager", "C001"));
    }

    [Fact]
    public async Task Submit_approve_post_generates_a_balanced_linked_journal_entry_Dr_Payable_Cr_Bank()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var request = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 1000m) }, "First installment");
        var created = await payments.CreateAsync(request, "ahmer.bilal", "C001");

        await payments.SubmitAsync(created.Id, "ahmer.bilal");
        await payments.ApproveAsync(created.Id, "finance.manager");
        var posted = await payments.PostAsync(created.Id, "finance.manager");

        Assert.Equal("Posted", posted.Status);
        Assert.NotNull(posted.LinkedJournalEntryId);
    }

    [Fact]
    public async Task Two_payments_against_the_same_invoice_cannot_together_exceed_its_Gross_Amount()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var firstRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 600m) });
        var first = await payments.CreateAsync(firstRequest, "ahmer.bilal", "C001");
        await payments.SubmitAsync(first.Id, "ahmer.bilal");
        await payments.ApproveAsync(first.Id, "finance.manager");
        await payments.PostAsync(first.Id, "finance.manager");

        var secondRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 500m) });

        await Assert.ThrowsAsync<ArgumentException>(() => payments.CreateAsync(secondRequest, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task A_second_installment_within_the_remaining_balance_succeeds()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var firstRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 600m) });
        var first = await payments.CreateAsync(firstRequest, "ahmer.bilal", "C001");
        await payments.SubmitAsync(first.Id, "ahmer.bilal");
        await payments.ApproveAsync(first.Id, "finance.manager");
        await payments.PostAsync(first.Id, "finance.manager");

        var secondRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 400m) });
        var second = await payments.CreateAsync(secondRequest, "ahmer.bilal", "C001");
        await payments.SubmitAsync(second.Id, "ahmer.bilal");
        await payments.ApproveAsync(second.Id, "finance.manager");
        var posted = await payments.PostAsync(second.Id, "finance.manager");

        Assert.Equal("Posted", posted.Status);
    }

    [Fact]
    public async Task Reversing_a_payment_releases_its_allocation_so_a_new_payment_can_use_it()
    {
        var (payments, invoiceRepo, bankAccountRepo, _) = BuildServices();
        var invoice = AddPostedInvoice(invoiceRepo, 1000m);
        var bankAccount = AddApprovedBankAccount(bankAccountRepo);

        var firstRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate, "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 1000m) });
        var first = await payments.CreateAsync(firstRequest, "ahmer.bilal", "C001");
        await payments.SubmitAsync(first.Id, "ahmer.bilal");
        await payments.ApproveAsync(first.Id, "finance.manager");
        var posted = await payments.PostAsync(first.Id, "finance.manager");

        var reversed = await payments.ReverseAsync(posted.Id, "finance.manager", PaymentDate.AddDays(1));
        Assert.Equal("Reversed", reversed.Status);

        var secondRequest = new CreatePaymentRequest(
            VendorId, bankAccount.Id, PaymentDate.AddDays(2), "BankTransfer",
            new[] { new CreatePaymentAllocationRequest(invoice.Id, 1000m) });
        var second = await payments.CreateAsync(secondRequest, "ahmer.bilal", "C001");

        Assert.Equal(1000m, second.Amount);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var (payments, _, _, _) = BuildServices();
        Assert.Null(await payments.GetAsync(Guid.NewGuid()));
    }
}
