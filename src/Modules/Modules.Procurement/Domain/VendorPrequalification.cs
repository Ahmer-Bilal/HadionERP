using Platform.Core;

namespace Modules.Procurement.Domain;

/// <summary>
/// One vendor's qualification to act in a specific <see cref="RoleType"/>+<see cref="Trade"/> combination —
/// the first Phase 2 Business Object (ROADMAP.md), and the reason
/// Modules.MasterData.Domain.BusinessRole allows the same role type twice with a different trade (a
/// Subcontractor–Electrical prequalification is entirely separate from a Subcontractor–Concrete one on the
/// same company). <see cref="RoleType"/> is a plain string, not a shared enum reference, because
/// Modules.Procurement depends only on Modules.MasterData.Contracts (docs/architecture/01-architecture-
/// foundation.md §3.2), never on MasterData's own Domain — the service layer validates it against the
/// vendor's actual <c>BusinessPartnerSummary.BusinessRoles</c> at creation time instead.
///
/// Deliberately stops at Approved, like every Master Data-ish BO so far (GLAccount/Item/CostCenter/TaxCode)
/// — there is no Post/Reverse here, since a prequalification is a certification record, not a financial
/// document with a ledger effect.
/// </summary>
public sealed class VendorPrequalification : BusinessObject
{
    public Guid BusinessPartnerId { get; private set; }

    /// <summary>The <c>BusinessRoleType</c> string (e.g. "Subcontractor") being qualified for — the
    /// service validates this against the vendor's actual <c>BusinessRoles</c> and rejects
    /// "GovernmentAuthority" outright (the roadmap's explicit "not prequalified at all" exclusion).</summary>
    public string RoleType { get; private set; }

    /// <summary>Matches the specific <c>BusinessRole.Trade</c> being qualified for, when the role carries
    /// one (e.g. Subcontractor–Electrical) — null for roles that don't carry a trade.</summary>
    public string? Trade { get; private set; }

    /// <summary>Set together with <see cref="ValidUntil"/> the moment the fifth (Quality) review step
    /// approves — never before, since a prequalification isn't valid until every domain has signed off.</summary>
    public DateOnly? ValidFrom { get; private set; }

    /// <summary>Computed once, at approval time, from a configured validity period (see
    /// <c>VendorPrequalificationService.ValidityMonthsConfigurationKey</c>) — not re-derived later, so a
    /// future change to the configured period never retroactively shifts an already-approved certificate's
    /// expiry (same "snapshot, don't re-derive" reasoning as <c>Modules.Finance.Domain.APInvoice.TaxRate</c>).</summary>
    public DateOnly? ValidUntil { get; private set; }

    public VendorPrequalification(string createdBy, Guid businessPartnerId, string roleType, string? trade = null)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleType);
        BusinessPartnerId = businessPartnerId;
        RoleType = roleType;
        Trade = trade;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private VendorPrequalification()
    {
        RoleType = null!;
    }

    /// <summary>Called once, by the service, right after the final review step approves. Rejects a second
    /// call — the validity period is set exactly once per certificate, same "assign exactly once" guarantee
    /// <see cref="BusinessObject.AssignNumber"/> already enforces for the document number.</summary>
    public void SetValidityPeriod(DateOnly validFrom, int validityMonths)
    {
        if (validityMonths <= 0)
            throw new ArgumentOutOfRangeException(nameof(validityMonths), "Validity period must be at least one month.");
        if (ValidFrom is not null)
            throw new InvalidOperationException($"Vendor prequalification {Id} already has a validity period set.");

        ValidFrom = validFrom;
        ValidUntil = validFrom.AddMonths(validityMonths);
    }

    /// <summary>Whether this certificate covers <paramref name="asOf"/> — the check Procurement's future PR
    /// (task #100) and PO (task #102) slices will call before letting a vendor onto a document. Takes an
    /// explicit date rather than reading the clock itself, so it stays a pure, testable domain method.</summary>
    public bool IsValidAsOf(DateOnly asOf) =>
        Status == BusinessObjectStatus.Approved && ValidFrom is { } from && ValidUntil is { } until && asOf >= from && asOf <= until;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
