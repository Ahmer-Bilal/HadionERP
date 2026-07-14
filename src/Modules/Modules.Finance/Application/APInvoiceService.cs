using System.Text.Json;
using Modules.Finance.Domain;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;

namespace Modules.Finance.Application;

public sealed class APInvoiceService
{
    public const string NumberRangeKey = "FIN-AP";

    private const string AuditTargetType = "APInvoice";
    private const string AuditSource = "Modules.Finance";

    private readonly IAPInvoiceRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly Platform.Workflow.IWorkflowEngine _workflowEngine;
    private readonly Platform.Workflow.IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly IGLAccountLookup _glAccountLookup;
    private readonly ICostCenterLookup _costCenterLookup;
    private readonly ITaxCodeLookup _taxCodeLookup;
    private readonly JournalEntryService _journalEntryService;

    public APInvoiceService(
        IAPInvoiceRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        Platform.Workflow.IWorkflowEngine workflowEngine,
        Platform.Workflow.IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup,
        IGLAccountLookup glAccountLookup,
        ICostCenterLookup costCenterLookup,
        ITaxCodeLookup taxCodeLookup,
        JournalEntryService journalEntryService)
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
    }

    public async Task<APInvoiceDto> CreateAsync(
        CreateAPInvoiceRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, APInvoiceSecurity.MaintainPrivilegeKey);

        var vendor = await _businessPartnerLookup.GetAsync(request.VendorId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {request.VendorId} was not found.");
        if (vendor.PartnerType is not ("Vendor" or "Both"))
            throw new ArgumentException($"Business partner '{vendor.Name}' is not a Vendor.");
        if (vendor.Status != "Approved")
            throw new ArgumentException($"Business partner '{vendor.Name}' is not Approved (status: {vendor.Status}).");

        await ValidateAccountAsync(request.ExpenseAccountId, "Expense account", cancellationToken);
        await ValidateAccountAsync(request.PayableAccountId, "Payable account", cancellationToken);
        if (request.CostCenterId is { } costCenterId) await ValidateCostCenterAsync(costCenterId, cancellationToken);

        var invoice = new APInvoice(
            actor, request.VendorId, request.VendorInvoiceNumber, request.InvoiceDate, request.Description,
            request.ExpenseAccountId, request.PayableAccountId, request.NetAmount);
        invoice.SetCostCenter(request.CostCenterId);

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
            $"AP invoice '{invoice.DocumentNumber}' ({invoice.VendorInvoiceNumber}) created, gross {invoice.GrossAmount}.", AuditSource);

        return ToDto(invoice);
    }

    public async Task<APInvoiceDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _repository.GetAsync(id, cancellationToken);
        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<(IReadOnlyList<APInvoiceDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<APInvoiceDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, APInvoiceSecurity.MaintainPrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        var fromStatus = invoice.Status;
        invoice.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AP invoice '{invoice.DocumentNumber}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(APInvoiceWorkflow.BusinessObjectType, APInvoiceWorkflow.SubmitTransition, invoice.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == Platform.Workflow.WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(invoice, actor, cancellationToken);
        }

        return ToDto(invoice);
    }

    public Task<APInvoiceDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, Platform.Workflow.WorkflowDecision.Approve, cancellationToken);

    public Task<APInvoiceDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, Platform.Workflow.WorkflowDecision.Reject, cancellationToken);

    private async Task<APInvoiceDto> DecideApprovalAsync(
        Guid id, string actor, Platform.Workflow.WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, APInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(APInvoiceWorkflow.BusinessObjectType, invoice.Id, cancellationToken)
            ?? throw new InvalidOperationException($"AP invoice {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == Platform.Workflow.WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(invoice, actor, cancellationToken);
        else if (instance.Status == Platform.Workflow.WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(invoice, actor, cancellationToken);

        return ToDto(invoice);
    }

    /// <summary>Posts the invoice: generates a real linked G/L Journal Entry (Dr Expense, Dr VAT if any,
    /// Cr Payable — always balanced by construction since Gross = Net + Tax) via
    /// <see cref="JournalEntryService.CreateSystemGeneratedAsync"/>, then transitions the invoice itself to
    /// Posted. The invoice and its posting are two separate documents (each independently audited,
    /// independently reversible) linked by <see cref="APInvoice.LinkedJournalEntryId"/>, not one document
    /// pretending to be both.</summary>
    public async Task<APInvoiceDto> PostAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, APInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);

        var lines = new List<CreateJournalLineRequest>
        {
            new(invoice.ExpenseAccountId, invoice.NetAmount, 0, invoice.CostCenterId, $"AP Invoice {invoice.VendorInvoiceNumber}"),
        };
        if (invoice.TaxAmount > 0 && invoice.VatAccountId is { } vatAccountId)
            lines.Add(new CreateJournalLineRequest(vatAccountId, invoice.TaxAmount, 0, null, $"VAT on {invoice.VendorInvoiceNumber}"));
        lines.Add(new CreateJournalLineRequest(invoice.PayableAccountId, 0, invoice.GrossAmount, null, $"Payable for {invoice.VendorInvoiceNumber}"));

        var journalEntry = await _journalEntryService.CreateSystemGeneratedAsync(
            invoice.InvoiceDate, $"AP Invoice {invoice.DocumentNumber}: {invoice.Description}", lines,
            reversalOfEntryId: null, actor, cancellationToken);

        invoice.LinkJournalEntry(journalEntry.Id);
        var fromStatus = invoice.Status;
        invoice.Post(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AP invoice '{invoice.DocumentNumber}' posted via journal entry '{journalEntry.DocumentNumber}'.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        return ToDto(invoice);
    }

    /// <summary>Reverses a Posted invoice: reverses its linked Journal Entry (undoing the actual ledger
    /// effect, via <see cref="JournalEntryService.ReverseAsync"/>) and transitions the invoice itself to
    /// Reversed. Same "the original is never edited or deleted" principle as everywhere else.</summary>
    public async Task<APInvoiceDto> ReverseAsync(Guid id, string actor, DateOnly reversalDate, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, APInvoiceSecurity.ApprovePrivilegeKey);
        var invoice = await RequireInvoiceAsync(id, cancellationToken);
        if (invoice.LinkedJournalEntryId is not { } journalEntryId)
            throw new InvalidOperationException($"AP invoice {id} has no linked journal entry to reverse.");

        await _journalEntryService.ReverseAsync(journalEntryId, actor, reversalDate, cancellationToken);

        var fromStatus = invoice.Status;
        invoice.Reverse(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AP invoice '{invoice.DocumentNumber}' reversed.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);

        return ToDto(invoice);
    }

    private async Task ApproveInternalAsync(APInvoice invoice, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = invoice.Status;
        invoice.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AP invoice '{invoice.DocumentNumber}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(invoice.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(APInvoice invoice, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = invoice.Status;
        invoice.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(invoice.Id), actor,
            $"AP invoice '{invoice.DocumentNumber}' rejected.",
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

    private async Task<APInvoice> RequireInvoiceAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"AP invoice {id} was not found.");

    private static APInvoiceDto ToDto(APInvoice i) => new(
        i.Id, i.DocumentNumber, i.Status.ToString(), i.VendorId, i.VendorInvoiceNumber, i.InvoiceDate, i.Description,
        i.ExpenseAccountId, i.PayableAccountId, i.CostCenterId, i.TaxCodeId, i.TaxRate, i.VatAccountId,
        i.NetAmount, i.TaxAmount, i.GrossAmount, i.LinkedJournalEntryId, i.CreatedAt, i.CreatedBy);
}
