using Platform.Core;

namespace Modules.Construction.Domain;

/// <summary>
/// The Interim Payment Certificate — the periodic billing document a contractor issues against certified
/// measurement (construction-commercial-processes-spec.md §3), generated from exactly one Approved
/// <see cref="MeasurementSheet"/> (<see cref="MeasurementSheetId"/>, enforced one-IPC-per-sheet by
/// <c>IpcService.CreateAsync</c> so the same period can never be billed twice). Polymorphic over
/// "commercial document" (<see cref="CommercialDocumentType"/> + <see cref="CommercialDocumentId"/>) for
/// the same reason <see cref="MeasurementSheet"/> is — a Subcontract runs its own independent IPC cycle
/// against the main contractor (spec §6c), not a child of the main Contract's IPC.
///
/// Reuses the platform's Draft → Submitted → Approved/Rejected lifecycle unchanged for the spec's Draft
/// (contractor prepares) → Submitted (to Engineer) → Certified (Engineer issues the Payment Certificate)
/// workflow — "Approved" here IS "Certified," same convention <see cref="MeasurementSheet"/> already
/// established. Certification raises a real Draft invoice in Finance — a Contract-type IPC raises an AR
/// Invoice against the Project's Customer (<c>Modules.Finance.Contracts.ICustomerInvoicingService</c>), a
/// Subcontract-type IPC raises an AP Invoice against the Subcontract's own Subcontractor
/// (<c>Modules.Finance.Contracts.IVendorInvoicingService</c>) — the two cross-module *write* Contracts
/// interfaces in this system, mirror images of each other. Both leave the invoice in Draft, never
/// auto-posted — a human in Finance still submits/approves/posts it through the normal invoice lifecycle,
/// preserving Segregation of Duties between "Construction certifies the work" and "Finance actually books
/// the ledger effect." Still stops at Approved, not the spec's further "Paid" step — that needs a
/// real Customer Receipt (built) or vendor Payment (already built) allocation against the raised invoice,
/// a separate manual step for now.
///
/// The money waterfall is deliberately simple relative to the spec's own step numbering: Retention and
/// Advance Recovery are both calculated as a straight percentage of THIS PERIOD's certified value (not the
/// cumulative-to-date value the spec's literal wording could be read as), which is standard real-world IPC
/// practice — each certified amount has its own pro-rata retention/advance-recovery deducted, not one lump
/// deduction recalculated off a growing cumulative base every period.
/// </summary>
public sealed class Ipc : BusinessObject
{
    private readonly List<IpcLine> _lines = new();

    public Guid ProjectId { get; private set; }
    public CommercialDocumentType CommercialDocumentType { get; private set; }
    public Guid CommercialDocumentId { get; private set; }
    public Guid MeasurementSheetId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }

    /// <summary>Snapshotted from the commercial document at creation time — see <see cref="IpcLine.Rate"/>
    /// for why an IPC never reads these live from the Contract/Subcontract after the fact.</summary>
    public decimal? RetentionPercentageApplied { get; private set; }

    public decimal? AdvancePaymentPercentageApplied { get; private set; }

    /// <summary>Liquidated damages, material-on-site advance recovery, back-charges, and prior over-
    /// certification corrections all belong here per the spec's waterfall step 6 — none of those mechanisms
    /// exist yet, so this is genuinely manual entry for this slice, not yet computed from anything.</summary>
    public decimal OtherDeductions { get; private set; }

    /// <summary>Which accounts a Contract-type IPC's eventual AR Invoice should post to — required at
    /// creation for a Contract IPC (validated by <c>IpcService.CreateAsync</c>), always null for a
    /// Subcontract IPC (no AR billing happens on that side). Captured here rather than resolved fresh at
    /// certification time so the accounts a maintainer chose at Draft time are exactly what gets billed,
    /// the same "freeze financial facts" reasoning as <see cref="IpcLine.Rate"/>.</summary>
    public Guid? RevenueAccountId { get; private set; }

    public Guid? ReceivableAccountId { get; private set; }

    /// <summary>Mirror of <see cref="RevenueAccountId"/>/<see cref="ReceivableAccountId"/> for a
    /// Subcontract-type IPC's eventual AP Invoice — required at creation for a Subcontract IPC, always null
    /// for a Contract IPC (no AP billing happens on that side).</summary>
    public Guid? ExpenseAccountId { get; private set; }

    public Guid? PayableAccountId { get; private set; }

    /// <summary>Shared by both directions — the tax rate mechanics are identical, only which side of the
    /// journal entry the VAT account lands on differs (handled entirely inside ARInvoiceService/
    /// APInvoiceService, not here).</summary>
    public Guid? TaxCodeId { get; private set; }

    public Guid? VatAccountId { get; private set; }

    /// <summary>Set once, when a Contract-type IPC is certified and its AR Invoice is raised — see
    /// <c>IpcService.ApproveInternalAsync</c>. Null for a Subcontract IPC and for a Contract IPC that hasn't
    /// reached Approved yet.</summary>
    public Guid? LinkedArInvoiceId { get; private set; }

    /// <summary>Mirror of <see cref="LinkedArInvoiceId"/> for a Subcontract-type IPC's AP Invoice.</summary>
    public Guid? LinkedApInvoiceId { get; private set; }

    public IReadOnlyCollection<IpcLine> Lines => _lines.AsReadOnly();

    public decimal GrossValueToDate => _lines.Sum(l => l.ValueToDate);
    public decimal GrossValueThisPeriod => _lines.Sum(l => l.ValueThisPeriod);
    public decimal GrossValuePreviousIpc => GrossValueToDate - GrossValueThisPeriod;
    public decimal RetentionAmount => GrossValueThisPeriod * (RetentionPercentageApplied ?? 0m) / 100m;
    public decimal AdvanceRecoveryAmount => GrossValueThisPeriod * (AdvancePaymentPercentageApplied ?? 0m) / 100m;
    public decimal NetPayable => GrossValueThisPeriod - RetentionAmount - AdvanceRecoveryAmount - OtherDeductions;

    public Ipc(
        string createdBy, Guid projectId, CommercialDocumentType commercialDocumentType, Guid commercialDocumentId,
        Guid measurementSheetId, DateOnly periodStart, DateOnly periodEnd,
        decimal? retentionPercentageApplied, decimal? advancePaymentPercentageApplied, decimal otherDeductions,
        Guid? revenueAccountId = null, Guid? receivableAccountId = null, Guid? taxCodeId = null, Guid? vatAccountId = null,
        Guid? expenseAccountId = null, Guid? payableAccountId = null)
        : base(createdBy)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));
        if (retentionPercentageApplied is < 0 or > 100)
            throw new ArgumentException("Retention percentage must be between 0 and 100.", nameof(retentionPercentageApplied));
        if (advancePaymentPercentageApplied is < 0 or > 100)
            throw new ArgumentException("Advance payment percentage must be between 0 and 100.", nameof(advancePaymentPercentageApplied));
        if (otherDeductions < 0)
            throw new ArgumentException("Other deductions cannot be negative.", nameof(otherDeductions));

        ProjectId = projectId;
        CommercialDocumentType = commercialDocumentType;
        CommercialDocumentId = commercialDocumentId;
        MeasurementSheetId = measurementSheetId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RetentionPercentageApplied = retentionPercentageApplied;
        AdvancePaymentPercentageApplied = advancePaymentPercentageApplied;
        OtherDeductions = otherDeductions;
        RevenueAccountId = revenueAccountId;
        ReceivableAccountId = receivableAccountId;
        TaxCodeId = taxCodeId;
        VatAccountId = vatAccountId;
        ExpenseAccountId = expenseAccountId;
        PayableAccountId = payableAccountId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private Ipc()
    {
    }

    /// <summary>Adds one billing line. Only while in Draft, same "frozen once submitted" rule as every other
    /// module's line collections. All cross-aggregate resolution (the commercial document's Rate for this
    /// line, the cumulative QuantityToDate) happens in <c>IpcService.CreateAsync</c>, not here.</summary>
    public IpcLine AddLine(Guid commercialDocumentLineId, decimal rate, decimal quantityThisPeriod, decimal quantityToDate)
    {
        if (Status != BusinessObjectStatus.Draft)
            throw new InvalidOperationException("Lines can only be added while the IPC is in Draft.");
        if (_lines.Any(l => l.CommercialDocumentLineId == commercialDocumentLineId))
            throw new ArgumentException(
                $"Line {commercialDocumentLineId} is already billed on this IPC.", nameof(commercialDocumentLineId));

        var line = new IpcLine(commercialDocumentLineId, rate, quantityThisPeriod, quantityToDate);
        _lines.Add(line);
        return line;
    }

    /// <summary>Records the AR Invoice raised for this IPC on certification — see
    /// <c>IpcService.ApproveInternalAsync</c>. Only ever called once, for a Contract-type IPC.</summary>
    public void LinkArInvoice(Guid arInvoiceId) => LinkedArInvoiceId = arInvoiceId;

    /// <summary>Mirror of <see cref="LinkArInvoice"/> for a Subcontract-type IPC's AP Invoice.</summary>
    public void LinkApInvoice(Guid apInvoiceId) => LinkedApInvoiceId = apInvoiceId;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
