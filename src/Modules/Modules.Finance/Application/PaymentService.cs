using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Finance.Application;

/// <summary>
/// Closes `ARCHITECTURE-AUDIT.md` Part 2 §16: this is the one place in the whole codebase that can finally
/// record that an <see cref="APInvoice"/> was actually paid. Mirrors <see cref="APInvoiceService"/>'s own
/// shape almost exactly (same Create/Submit/Approve/Reject/Post/Reverse pattern, same
/// "Post generates a real linked Journal Entry" pattern) — see that class for the pattern this one repeats.
/// </summary>
public sealed class PaymentService
{
    public const string NumberRangeKey = "FIN-PAY";

    private const string AuditTargetType = "Payment";
    private const string AuditSource = "Modules.Finance";

    /// <summary>Same set <see cref="APInvoiceService.PayableEligibleRoles"/> uses — a payment can only ever
    /// be made to a vendor an AP Invoice could already be raised against in the first place.</summary>
    private static readonly HashSet<string> PayableEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Supplier", "Subcontractor", "Consultant", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory",
    };

    private readonly IPaymentRepository _repository;
    private readonly IAPInvoiceRepository _apInvoiceRepository;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly ILookupCatalog _lookupCatalog;
    private readonly JournalEntryService _journalEntryService;

    public PaymentService(
        IPaymentRepository repository,
        IAPInvoiceRepository apInvoiceRepository,
        IBankAccountRepository bankAccountRepository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup,
        ILookupCatalog lookupCatalog,
        JournalEntryService journalEntryService)
    {
        _repository = repository;
        _apInvoiceRepository = apInvoiceRepository;
        _bankAccountRepository = bankAccountRepository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
        _lookupCatalog = lookupCatalog;
        _journalEntryService = journalEntryService;
    }

    public async Task<PaymentDto> CreateAsync(
        CreatePaymentRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PaymentSecurity.MaintainPrivilegeKey);

        var vendor = await _businessPartnerLookup.GetAsync(request.VendorId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {request.VendorId} was not found.");
        if (!vendor.BusinessRoles.Any(PayableEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{vendor.Name}' holds no role this platform can raise a payment against.");
        if (vendor.Status != "Approved")
            throw new ArgumentException($"Business partner '{vendor.Name}' is not Approved (status: {vendor.Status}).");

        var bankAccount = await _bankAccountRepository.GetAsync(request.BankAccountId, cancellationToken)
            ?? throw new ArgumentException($"Bank account {request.BankAccountId} was not found.");
        if (bankAccount.Status != BusinessObjectStatus.Approved)
            throw new ArgumentException($"Bank account '{bankAccount.AccountCode}' is not Approved.");
        if (!bankAccount.IsActive)
            throw new ArgumentException($"Bank account '{bankAccount.AccountCode}' is not active.");

        var paymentMethod = await _lookupCatalog.GetValueAsync("PaymentMethod", request.PaymentMethod, cancellationToken);
        if (paymentMethod is null || !paymentMethod.IsActive)
            throw new ArgumentException($"'{request.PaymentMethod}' is not a known, active Payment Method.");

        if (request.Allocations.Count == 0)
            throw new ArgumentException("A payment must allocate against at least one AP Invoice.");

        var payment = new Payment(actor, request.VendorId, request.BankAccountId, request.PaymentDate, request.PaymentMethod, request.Reference);

        foreach (var allocationRequest in request.Allocations)
        {
            var invoice = await _apInvoiceRepository.GetAsync(allocationRequest.APInvoiceId, cancellationToken)
                ?? throw new ArgumentException($"AP Invoice {allocationRequest.APInvoiceId} was not found.");
            if (invoice.VendorId != request.VendorId)
                throw new ArgumentException($"AP Invoice '{invoice.DocumentNumber}' does not belong to the specified vendor.");
            if (invoice.Status != BusinessObjectStatus.Posted)
                throw new ArgumentException(
                    $"AP Invoice '{invoice.DocumentNumber}' is not Posted (status: {invoice.Status}) and has no payable balance to pay against.");

            var outstanding = await ComputeOutstandingBalanceAsync(invoice, cancellationToken);
            if (allocationRequest.AllocatedAmount > outstanding)
                throw new ArgumentException(
                    $"Allocated amount {allocationRequest.AllocatedAmount} exceeds AP Invoice '{invoice.DocumentNumber}''s outstanding balance {outstanding}.");

            payment.AddAllocation(allocationRequest.APInvoiceId, allocationRequest.AllocatedAmount);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        payment.AssignNumber(documentNumber);

        _repository.Add(payment);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' created, amount {payment.Amount}.", AuditSource);

        return ToDto(payment);
    }

    public async Task<PaymentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _repository.GetAsync(id, cancellationToken);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<(IReadOnlyList<PaymentDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        var dtos = new List<PaymentDto>(items.Count);
        foreach (var item in items) dtos.Add(ToDto(item));
        return (dtos, total);
    }

    public async Task<PaymentDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PaymentSecurity.MaintainPrivilegeKey);
        var payment = await RequirePaymentAsync(id, cancellationToken);
        var fromStatus = payment.Status;
        payment.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(payment.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(PaymentWorkflow.BusinessObjectType, PaymentWorkflow.SubmitTransition, payment.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(payment, actor, cancellationToken);
        }

        return ToDto(payment);
    }

    public Task<PaymentDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<PaymentDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<PaymentDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, PaymentSecurity.ApprovePrivilegeKey);
        var payment = await RequirePaymentAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(PaymentWorkflow.BusinessObjectType, payment.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(payment, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(payment, actor, cancellationToken);

        return ToDto(payment);
    }

    /// <summary>Posts the payment: re-validates every allocation against each invoice's CURRENT outstanding
    /// balance (not just what it was at Create time — another payment could have posted against the same
    /// invoice in the meantime, since a Draft/Submitted/Approved payment can sit for a while), then
    /// generates one real linked Journal Entry — Debit each allocated invoice's own Payable account,
    /// Credit the paying Bank Account's linked G/L account — via
    /// <see cref="JournalEntryService.CreateSystemGeneratedAsync"/>, the exact same mechanism
    /// <see cref="APInvoiceService.PostAsync"/> already uses.</summary>
    public async Task<PaymentDto> PostAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PaymentSecurity.ApprovePrivilegeKey);
        var payment = await RequirePaymentAsync(id, cancellationToken);
        var bankAccount = await _bankAccountRepository.GetAsync(payment.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {payment.BankAccountId} was not found.");

        var lines = new List<CreateJournalLineRequest>();
        foreach (var allocation in payment.Allocations)
        {
            var invoice = await _apInvoiceRepository.GetAsync(allocation.APInvoiceId, cancellationToken)
                ?? throw new InvalidOperationException($"AP Invoice {allocation.APInvoiceId} was not found.");

            var outstanding = await ComputeOutstandingBalanceAsync(invoice, cancellationToken);
            if (allocation.AllocatedAmount > outstanding)
                throw new InvalidOperationException(
                    $"Allocated amount {allocation.AllocatedAmount} for invoice '{invoice.DocumentNumber}' exceeds its current " +
                    $"outstanding balance {outstanding} — another payment may have posted against it since this payment was created.");

            lines.Add(new CreateJournalLineRequest(
                invoice.PayableAccountId, allocation.AllocatedAmount, 0, null,
                $"Payment {payment.DocumentNumber} to invoice {invoice.DocumentNumber}"));
        }
        lines.Add(new CreateJournalLineRequest(bankAccount.LinkedGLAccountId, 0, payment.Amount, null, $"Payment {payment.DocumentNumber}"));

        var journalEntry = await _journalEntryService.CreateSystemGeneratedAsync(
            payment.PaymentDate, $"Payment {payment.DocumentNumber}: {payment.Reference ?? "vendor payment"}", lines,
            reversalOfEntryId: null, actor, cancellationToken);

        payment.LinkJournalEntry(journalEntry.Id);
        var fromStatus = payment.Status;
        payment.Post(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' posted via journal entry '{journalEntry.DocumentNumber}'.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(payment.Status.ToString()), AuditSource);

        return ToDto(payment);
    }

    /// <summary>Reverses a Posted payment: reverses its linked Journal Entry and transitions the payment
    /// itself to Reversed — the same "original never edited or deleted" principle as
    /// <see cref="APInvoiceService.ReverseAsync"/>. A Reversed payment's allocations no longer count toward
    /// any invoice's "amount already paid" (see <see cref="ComputeOutstandingBalanceAsync"/>'s Posted-only
    /// filter), so the invoice's outstanding balance goes back up by the reversed amount.</summary>
    public async Task<PaymentDto> ReverseAsync(Guid id, string actor, DateOnly reversalDate, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, PaymentSecurity.ApprovePrivilegeKey);
        var payment = await RequirePaymentAsync(id, cancellationToken);
        if (payment.LinkedJournalEntryId is not { } journalEntryId)
            throw new InvalidOperationException($"Payment {id} has no linked journal entry to reverse.");

        await _journalEntryService.ReverseAsync(journalEntryId, actor, reversalDate, cancellationToken);

        var fromStatus = payment.Status;
        payment.Reverse(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' reversed.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(payment.Status.ToString()), AuditSource);

        return ToDto(payment);
    }

    /// <summary>How much of <paramref name="invoice"/>'s Gross Amount is still unpaid — Gross minus every
    /// *Posted, unreversed* payment's allocation against it. A Draft/Submitted/Approved payment's
    /// allocations don't count yet (they're proposals, not real ledger effects); a Reversed payment's
    /// allocations don't count either (its ledger effect was undone). Duplicated (not shared) with
    /// <see cref="APInvoiceService.GetOutstandingBalanceAsync"/> — same small-helper-per-service tolerance
    /// this codebase already accepts for e.g. <c>RequireAuthorization</c>/<c>BuildPrincipal</c>.</summary>
    private async Task<decimal> ComputeOutstandingBalanceAsync(APInvoice invoice, CancellationToken cancellationToken)
    {
        var payments = await _repository.ListByInvoiceAsync(invoice.Id, cancellationToken);
        var paidSoFar = payments
            .Where(p => p.Status == BusinessObjectStatus.Posted)
            .SelectMany(p => p.Allocations)
            .Where(a => a.APInvoiceId == invoice.Id)
            .Sum(a => a.AllocatedAmount);
        return invoice.GrossAmount - paidSoFar;
    }

    private async Task ApproveInternalAsync(Payment payment, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = payment.Status;
        payment.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(payment.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Payment payment, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = payment.Status;
        payment.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(payment.Id), actor,
            $"Payment '{payment.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(payment.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid paymentId) => new(paymentId, AuditTargetType, "Self");

    private async Task<Payment> RequirePaymentAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment {id} was not found.");

    private static PaymentDto ToDto(Payment p) => new(
        p.Id, p.DocumentNumber, p.Status.ToString(), p.VendorId, p.BankAccountId, p.PaymentDate, p.PaymentMethod, p.Reference,
        p.Allocations.Select(a => new PaymentAllocationDto(a.Id, a.APInvoiceId, a.AllocatedAmount)).ToList(),
        p.Amount, p.LinkedJournalEntryId, p.CreatedAt, p.CreatedBy);
}
