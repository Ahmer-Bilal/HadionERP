using Platform.Core;

namespace Modules.MasterData.Domain;

/// <summary>
/// One tax code (e.g. "VAT15" at 15%, "ZERO" at 0%, "EXEMPT") — the fifth and last Phase 1 Master Data
/// piece (docs/architecture/06-roadmap.md's Phase 1 list). Every AP/AR document that needs ZATCA-compliant
/// VAT references one of these; the <see cref="Rate"/> is data on this record, never a literal percentage
/// hardcoded in any module's code (CLAUDE.md's "don't hard-code business rules... that should be
/// configuration" — a rate change is a data edit here, not a code change anywhere).
///
/// Deliberately flat, no parent hierarchy, mirroring <see cref="Item"/>'s shape, not
/// <see cref="GLAccount"/>'s — a tax code list doesn't need roll-ups.
/// </summary>
public sealed class TaxCode : BusinessObject
{
    /// <summary>The business-facing tax code (e.g. "VAT15", "ZERO", "EXEMPT"), distinct from the
    /// sequential <see cref="BusinessObject.DocumentNumber"/> audit id. Must be unique — enforced by the
    /// service + a DB unique index.</summary>
    public string TaxCodeCode { get; private set; }

    public string TaxCodeName { get; private set; }

    /// <summary>The tax code's name in Arabic — same bilingual precedent as every other Phase 1
    /// master-data entity.</summary>
    public string? TaxCodeNameArabic { get; private set; }

    /// <summary>The percentage rate this code charges (e.g. 15.00m for 15%). Zero for ZeroRated/Exempt
    /// types, but stored explicitly rather than derived — a future reduced-rate category could still need
    /// a non-zero rate on a non-Standard type.</summary>
    public decimal Rate { get; private set; }

    public TaxType TaxType { get; private set; }

    /// <summary>True when the tax code accepts new use on documents. Deactivating (rather than deleting) a
    /// tax code that already has history keeps prior documents valid while preventing new ones — the same
    /// "correct by reversal, not by deletion" principle used everywhere else in this platform.</summary>
    public bool IsActive { get; private set; }

    public TaxCode(string createdBy, string taxCodeCode, string taxCodeName, decimal rate, TaxType taxType)
        : base(createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taxCodeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxCodeName);
        if (rate < 0 || rate > 100)
            throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate must be between 0 and 100.");

        TaxCodeCode = taxCodeCode;
        TaxCodeName = taxCodeName;
        Rate = rate;
        TaxType = taxType;
        IsActive = true;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="BusinessObject"/>'s parameterless
    /// constructor. Never call from application code.</summary>
    private TaxCode()
    {
        TaxCodeCode = null!;
        TaxCodeName = null!;
    }

    public void UpdateTaxCodeName(string taxCodeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taxCodeName);
        TaxCodeName = taxCodeName;
    }

    public void UpdateTaxCodeNameArabic(string? taxCodeNameArabic) => TaxCodeNameArabic = taxCodeNameArabic;

    public void UpdateRate(decimal rate)
    {
        if (rate < 0 || rate > 100)
            throw new ArgumentOutOfRangeException(nameof(rate), "Tax rate must be between 0 and 100.");
        Rate = rate;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Submit(string actor) => Transition(BusinessObjectTransition.Submit, actor);

    public void Approve(string actor) => Transition(BusinessObjectTransition.Approve, actor);

    public void Reject(string actor) => Transition(BusinessObjectTransition.Reject, actor);
}
