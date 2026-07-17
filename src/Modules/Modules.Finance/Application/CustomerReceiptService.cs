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
/// The AR mirror of <see cref="PaymentService"/> — finally makes <see cref="ARInvoiceDto.OutstandingBalance"/>
/// mean something real rather than always equalling Gross Amount. Mirrors <see cref="PaymentService"/>'s
/// shape almost exactly (Create/Submit/Approve/Reject/Post/Reverse, Post generates a real linked Journal
/// Entry) — see that class for the pattern this one repeats.
/// </summary>
public sealed class CustomerReceiptService
{
    public const string NumberRangeKey = "FIN-CR";

    private const string AuditTargetType = "CustomerReceipt";
    private const string AuditSource = "Modules.Finance";

    private static readonly HashSet<string> ReceivableEligibleRoles = new(StringComparer.OrdinalIgnoreCase) { "Client" };

    private readonly ICustomerReceiptRepository _repository;
    private readonly IARInvoiceRepository _arInvoiceRepository;
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

    public CustomerReceiptService(
        ICustomerReceiptRepository repository,
        IARInvoiceRepository arInvoiceRepository,
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
        _arInvoiceRepository = arInvoiceRepository;
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

    public async Task<CustomerReceiptDto> CreateAsync(
        CreateCustomerReceiptRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CustomerReceiptSecurity.MaintainPrivilegeKey);

        var customer = await _businessPartnerLookup.GetAsync(request.CustomerId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {request.CustomerId} was not found.");
        if (!customer.BusinessRoles.Any(ReceivableEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{customer.Name}' holds no role this platform can raise a receipt against.");
        if (customer.Status != "Approved")
            throw new ArgumentException($"Business partner '{customer.Name}' is not Approved (status: {customer.Status}).");

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
            throw new ArgumentException("A receipt must allocate against at least one AR Invoice.");

        var receipt = new CustomerReceipt(actor, request.CustomerId, request.BankAccountId, request.ReceiptDate, request.PaymentMethod, request.Reference);

        foreach (var allocationRequest in request.Allocations)
        {
            var invoice = await _arInvoiceRepository.GetAsync(allocationRequest.ARInvoiceId, cancellationToken)
                ?? throw new ArgumentException($"AR Invoice {allocationRequest.ARInvoiceId} was not found.");
            if (invoice.CustomerId != request.CustomerId)
                throw new ArgumentException($"AR Invoice '{invoice.DocumentNumber}' does not belong to the specified customer.");
            if (invoice.Status != BusinessObjectStatus.Posted)
                throw new ArgumentException(
                    $"AR Invoice '{invoice.DocumentNumber}' is not Posted (status: {invoice.Status}) and has no receivable balance to receive against.");

            var outstanding = await ComputeOutstandingBalanceAsync(invoice, cancellationToken);
            if (allocationRequest.AllocatedAmount > outstanding)
                throw new ArgumentException(
                    $"Allocated amount {allocationRequest.AllocatedAmount} exceeds AR Invoice '{invoice.DocumentNumber}''s outstanding balance {outstanding}.");

            receipt.AddAllocation(allocationRequest.ARInvoiceId, allocationRequest.AllocatedAmount);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        receipt.AssignNumber(documentNumber);

        _repository.Add(receipt);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' created, amount {receipt.Amount}.", AuditSource);

        return ToDto(receipt);
    }

    public async Task<CustomerReceiptDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var receipt = await _repository.GetAsync(id, cancellationToken);
        return receipt is null ? null : ToDto(receipt);
    }

    public async Task<(IReadOnlyList<CustomerReceiptDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<IReadOnlyList<CustomerReceiptDto>> ListByInvoiceAsync(Guid arInvoiceId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListByInvoiceAsync(arInvoiceId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<CustomerReceiptDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CustomerReceiptSecurity.MaintainPrivilegeKey);
        var receipt = await RequireReceiptAsync(id, cancellationToken);
        var fromStatus = receipt.Status;
        receipt.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(receipt.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(CustomerReceiptWorkflow.BusinessObjectType, CustomerReceiptWorkflow.SubmitTransition, receipt.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(receipt, actor, cancellationToken);
        }

        return ToDto(receipt);
    }

    public Task<CustomerReceiptDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<CustomerReceiptDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<CustomerReceiptDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, CustomerReceiptSecurity.ApprovePrivilegeKey);
        var receipt = await RequireReceiptAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(CustomerReceiptWorkflow.BusinessObjectType, receipt.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Customer receipt {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(receipt, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(receipt, actor, cancellationToken);

        return ToDto(receipt);
    }

    /// <summary>Posts the receipt: re-validates every allocation against each invoice's CURRENT outstanding
    /// balance, then generates one real linked Journal Entry — Debit the receiving Bank Account's linked G/L
    /// account, Credit each allocated invoice's own Receivable account (the mirror image of
    /// <see cref="PaymentService.PostAsync"/>'s Debit Payable/Credit Bank).</summary>
    public async Task<CustomerReceiptDto> PostAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CustomerReceiptSecurity.ApprovePrivilegeKey);
        var receipt = await RequireReceiptAsync(id, cancellationToken);
        var bankAccount = await _bankAccountRepository.GetAsync(receipt.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {receipt.BankAccountId} was not found.");

        var lines = new List<CreateJournalLineRequest> { new(bankAccount.LinkedGLAccountId, receipt.Amount, 0, null, $"Receipt {receipt.DocumentNumber}") };
        foreach (var allocation in receipt.Allocations)
        {
            var invoice = await _arInvoiceRepository.GetAsync(allocation.ARInvoiceId, cancellationToken)
                ?? throw new InvalidOperationException($"AR Invoice {allocation.ARInvoiceId} was not found.");

            var outstanding = await ComputeOutstandingBalanceAsync(invoice, cancellationToken);
            if (allocation.AllocatedAmount > outstanding)
                throw new InvalidOperationException(
                    $"Allocated amount {allocation.AllocatedAmount} for invoice '{invoice.DocumentNumber}' exceeds its current " +
                    $"outstanding balance {outstanding} — another receipt may have posted against it since this receipt was created.");

            lines.Add(new CreateJournalLineRequest(
                invoice.ReceivableAccountId, 0, allocation.AllocatedAmount,
                null, $"Receipt {receipt.DocumentNumber} against invoice {invoice.DocumentNumber}"));
        }

        var journalEntry = await _journalEntryService.CreateSystemGeneratedAsync(
            receipt.ReceiptDate, $"Customer Receipt {receipt.DocumentNumber}: {receipt.Reference ?? "customer receipt"}", lines,
            reversalOfEntryId: null, actor, cancellationToken);

        receipt.LinkJournalEntry(journalEntry.Id);
        var fromStatus = receipt.Status;
        receipt.Post(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' posted via journal entry '{journalEntry.DocumentNumber}'.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(receipt.Status.ToString()), AuditSource);

        return ToDto(receipt);
    }

    public async Task<CustomerReceiptDto> ReverseAsync(Guid id, string actor, DateOnly reversalDate, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, CustomerReceiptSecurity.ApprovePrivilegeKey);
        var receipt = await RequireReceiptAsync(id, cancellationToken);
        if (receipt.LinkedJournalEntryId is not { } journalEntryId)
            throw new InvalidOperationException($"Customer receipt {id} has no linked journal entry to reverse.");

        await _journalEntryService.ReverseAsync(journalEntryId, actor, reversalDate, cancellationToken);

        var fromStatus = receipt.Status;
        receipt.Reverse(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' reversed.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(receipt.Status.ToString()), AuditSource);

        return ToDto(receipt);
    }

    private async Task<decimal> ComputeOutstandingBalanceAsync(ARInvoice invoice, CancellationToken cancellationToken)
    {
        var receipts = await _repository.ListByInvoiceAsync(invoice.Id, cancellationToken);
        var receivedSoFar = receipts
            .Where(r => r.Status == BusinessObjectStatus.Posted)
            .SelectMany(r => r.Allocations)
            .Where(a => a.ARInvoiceId == invoice.Id)
            .Sum(a => a.AllocatedAmount);
        return invoice.GrossAmount - receivedSoFar;
    }

    private async Task ApproveInternalAsync(CustomerReceipt receipt, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = receipt.Status;
        receipt.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(receipt.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(CustomerReceipt receipt, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = receipt.Status;
        receipt.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(receipt.Id), actor,
            $"Customer receipt '{receipt.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(receipt.Status.ToString()), AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid receiptId) => new(receiptId, AuditTargetType, "Self");

    private async Task<CustomerReceipt> RequireReceiptAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer receipt {id} was not found.");

    private static CustomerReceiptDto ToDto(CustomerReceipt r) => new(
        r.Id, r.DocumentNumber, r.Status.ToString(), r.CustomerId, r.BankAccountId, r.ReceiptDate, r.PaymentMethod, r.Reference,
        r.Allocations.Select(a => new CustomerReceiptAllocationDto(a.Id, a.ARInvoiceId, a.AllocatedAmount)).ToList(),
        r.Amount, r.LinkedJournalEntryId, r.CreatedAt, r.CreatedBy);
}
