using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Finance.Application;

public sealed class ARInvoiceService
{
    public const string NumberRangeKey = "FIN-AR";

    private const string AuditTargetType = "ARInvoice";
    private const string AuditSource = "Modules.Finance";

    /// <summary>Mirror of <c>APInvoiceService.PayableEligibleRoles</c> — only Client is a real
    /// AR-invoiced counterparty (ROADMAP.md's Phase 2 role design); every vendor-family role AP targets is
    /// deliberately excluded here, the same way AP excludes Client.</summary>
    private static readonly HashSet<string> ReceivableEligibleRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Client",
    };

    private readonly IARInvoiceRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly IGLAccountLookup _glAccountLookup;
    private readonly ICostCenterLookup _costCenterLookup;
    private readonly ITaxCodeLookup _taxCodeLookup;
    private readonly JournalEntryService _journalEntryService;
    private readonly ICustomerReceiptRepository _customerReceiptRepository;

    public ARInvoiceService(
        IARInvoiceRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup,
        IGLAccountLookup glAccountLookup,
        ICostCenterLookup costCenterLookup,
        ITaxCodeLookup taxCodeLookup,
        JournalEntryService journalEntryService,
        ICustomerReceiptRepository customerReceiptRepository)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
        _glAccountLookup = glAccountLookup;
        _costCenterLookup = costCenterLookup;
        _taxCodeLookup = taxCodeLookup;
        _journalEntryService = journalEntryService;
        _customerReceiptRepository = customerReceiptRepository;
    }

    public async Task<ARInvoiceDto> CreateAsync(
        CreateARInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default,
        string? sourceDocumentType = null, Guid? sourceDocumentId = null)
    {
        RequireAuthorization(actor, ARInvoiceSecurity.MaintainPrivilegeKey);

        var customer = await _businessPartnerLookup.GetAsync(request.CustomerId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {request.CustomerId} was not found.");
        if (!customer.BusinessRoles.Any(ReceivableEligibleRoles.Contains))
            throw new ArgumentException(
                $"Business partner '{customer.Name}' holds no role this platform can raise an AR invoice against (needs Client).");
        if (customer.Status != "Approved")
            throw new ArgumentException($"Business partner '{customer.Name}' is not Approved (status: {customer.Status}).");

        await ValidateAccountAsync(request.RevenueAccountId, "Revenue account", cancellationToken);
        await ValidateAccountAsync(request.ReceivableAccountId, "Receivable account", cancellationToken);
        if (request.CostCenterId is { } costCenterId) await ValidateCostCenterAsync(costCenterId, cancellationToken);

        var invoice = new ARInvoice(
            actor, request.CustomerId, request.CustomerReference, request.InvoiceDate, request.Description,
            request.RevenueAccountId, request.ReceivableAccountId, request.NetAmount);
        invoice.SetCostCenter(request.CostCenterId);
        invoice.MarkSourceDocument(sourceDocumentType ?? "Manual", sourceDocumentId);

        if (request.TaxCodeId is { } taxCodeId)
        {
            var taxCode = await _taxCodeLookup.GetAsync(taxCodeId, cancellationToken)
                ?? throw new ArgumentException($"Tax code {taxCodeId} was not found.");
            if (!taxCode.IsActive)
                throw new ArgumentException($"Tax code '{taxCode.TaxCodeCode}' is not active.");
            if (request.VatAccountId is not { } vatAccountId)
                throw new ArgumentException("A VAT account is required when a tax code is specified.");
            await ValidateAccountAsync(vatAccountId, "VAT account", cancellationToken);

            invoice.SetTax(taxCodeId, taxCode.Rate, vatAccountId);
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        invoice.AssignNumber(documentNumber);

        _repository.Add(invoice);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' created for '{customer.Name}', gross {invoice.GrossAmount}.", AuditSource);

        return await ToDtoAsync(invoice, cancellationToken);
    }

    public async Task<ARInvoiceDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetAsync(id, cancellationToken);
        return invoice is null ? null : await ToDtoAsync(invoice, cancellationToken);
    }

    public async Task<(IReadOnlyList<ARInvoiceDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        var dtos = new List<ARInvoiceDto>(items.Count);
        foreach (var item in items) dtos.Add(await ToDtoAsync(item, cancellationToken));
        return (dtos, total);
    }

    /// <summary>How much of this invoice's Gross Amount is still unpaid — mirrors
    /// <c>APInvoiceService.GetOutstandingBalanceAsync</c> exactly, AR side.</summary>
    public async Task<decimal> GetOutstandingBalanceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await RequireInvoiceAsync(invoiceId, cancellationToken);
        return await ComputeOutstandingBalanceAsync(invoice, cancellationToken);
    }

    private async Task<decimal> ComputeOutstandingBalanceAsync(ARInvoice invoice, CancellationToken cancellationToken)
    {
        var receipts = await _customerReceiptRepository.ListByInvoiceAsync(invoice.Id, cancellationToken);
        var receivedSoFar = receipts
            .Where(r => r.Status == BusinessObjectStatus.Posted)
            .SelectMany(r => r.Allocations)
            .Where(a => a.ARInvoiceId == invoice.Id)
            .Sum(a => a.AllocatedAmount);
        return invoice.GrossAmount - receivedSoFar;
    }

    public async Task<ARInvoiceDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ARInvoiceSecurity.MaintainPrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        var fromStatus = invoice.Status;
        invoice.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(ARInvoiceWorkflow.BusinessObjectType, ARInvoiceWorkflow.SubmitTransition, invoice.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(invoice, actor, cancellationToken);
        }

        return await ToDtoAsync(invoice, cancellationToken);
    }

    public Task<ARInvoiceDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<ARInvoiceDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<ARInvoiceDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, ARInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(ARInvoiceWorkflow.BusinessObjectType, invoice.Id, cancellationToken)
            ?? throw new InvalidOperationException($"AR invoice {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(invoice, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(invoice, actor, cancellationToken);

        return await ToDtoAsync(invoice, cancellationToken);
    }

    /// <summary>Posts the invoice: generates a real linked G/L Journal Entry (Dr Receivable, Cr Revenue,
    /// Cr VAT Output if any — always balanced by construction since Gross = Net + Tax) via
    /// <see cref="JournalEntryService.CreateSystemGeneratedAsync"/>, then transitions the invoice itself to
    /// Posted. Mirror image of <c>APInvoiceService.PostAsync</c>'s Dr Expense/Dr VAT Input/Cr Payable.</summary>
    public async Task<ARInvoiceDto> PostAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ARInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);

        var lines = new List<CreateJournalLineRequest>
        {
            new(invoice.ReceivableAccountId, invoice.GrossAmount, 0, null, $"Receivable for {invoice.DocumentNumber}"),
        };
        lines.Add(new CreateJournalLineRequest(invoice.RevenueAccountId, 0, invoice.NetAmount, invoice.CostCenterId, $"AR Invoice {invoice.DocumentNumber}"));
        if (invoice.TaxAmount > 0 && invoice.VatAccountId is { } vatAccountId)
            lines.Add(new CreateJournalLineRequest(vatAccountId, 0, invoice.TaxAmount, null, $"VAT on {invoice.DocumentNumber}"));

        var journalEntry = await _journalEntryService.CreateSystemGeneratedAsync(
            invoice.InvoiceDate, $"AR Invoice {invoice.DocumentNumber}: {invoice.Description}", lines,
            reversalOfEntryId: null, actor, cancellationToken,
            sourceDocumentType: JournalEntrySourceDocumentTypes.ARInvoice, sourceDocumentId: invoice.Id);

        invoice.LinkJournalEntry(journalEntry.Id);
        var fromStatus = invoice.Status;
        invoice.Post(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' posted via journal entry '{journalEntry.DocumentNumber}'.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        return await ToDtoAsync(invoice, cancellationToken);
    }

    /// <summary>Reverses a Posted invoice: reverses its linked Journal Entry and transitions the invoice
    /// itself to Reversed. Same "the original is never edited or deleted" principle as everywhere else.</summary>
    public async Task<ARInvoiceDto> ReverseAsync(Guid id, string actor, DateOnly reversalDate, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ARInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        if (invoice.LinkedJournalEntryId is not { } journalEntryId)
            throw new InvalidOperationException($"AR invoice {id} has no linked journal entry to reverse.");

        await _journalEntryService.ReverseAsync(journalEntryId, actor, reversalDate, cancellationToken);

        var fromStatus = invoice.Status;
        invoice.Reverse(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' reversed.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        return await ToDtoAsync(invoice, cancellationToken);
    }

    private async Task ApproveInternalAsync(ARInvoice invoice, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = invoice.Status;
        invoice.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(ARInvoice invoice, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = invoice.Status;
        invoice.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AR invoice '{invoice.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);
    }

    private async Task ValidateAccountAsync(Guid accountId, string role, CancellationToken cancellationToken)
    {
        var account = await _glAccountLookup.GetAsync(accountId, cancellationToken)
            ?? throw new ArgumentException($"{role} {accountId} was not found.");
        if (!account.IsActive) throw new ArgumentException($"{role} '{account.AccountCode}' is not active.");
        if (!account.IsPostable) throw new ArgumentException($"{role} '{account.AccountCode}' is a header/grouping account and cannot be posted to.");
    }

    private async Task ValidateCostCenterAsync(Guid costCenterId, CancellationToken cancellationToken)
    {
        var costCenter = await _costCenterLookup.GetAsync(costCenterId, cancellationToken)
            ?? throw new ArgumentException($"Cost center {costCenterId} was not found.");
        if (!costCenter.IsActive) throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is not active.");
        if (!costCenter.IsPostable) throw new ArgumentException($"Cost center '{costCenter.CostCenterCode}' is a header/grouping cost center and cannot be posted to.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid invoiceId) => new(invoiceId, AuditTargetType, "Self");

    private async Task<ARInvoice> RequireInvoiceAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AR invoice {id} was not found.");

    private async Task<ARInvoiceDto> ToDtoAsync(ARInvoice i, CancellationToken cancellationToken)
    {
        var outstandingBalance = i.Status == BusinessObjectStatus.Posted
            ? await ComputeOutstandingBalanceAsync(i, cancellationToken)
            : 0m;

        return new(
            i.Id, i.DocumentNumber, i.Status.ToString(), i.CustomerId, i.CustomerReference, i.InvoiceDate, i.Description,
            i.RevenueAccountId, i.ReceivableAccountId, i.CostCenterId, i.TaxCodeId, i.TaxRate, i.VatAccountId,
            i.NetAmount, i.TaxAmount, i.GrossAmount, outstandingBalance, i.LinkedJournalEntryId,
            i.SourceDocumentType, i.SourceDocumentId, i.CreatedAt, i.CreatedBy);
    }
}
