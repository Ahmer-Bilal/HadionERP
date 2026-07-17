namespace Modules.Construction.Domain;

/// <summary>
/// One line of a <see cref="Contract"/>'s Bill of Quantities — a child entity, not an independent Business
/// Object (same "0..n child collection, only exists through its parent" pattern as
/// <c>Modules.ProjectManagement.Domain.WbsElement</c>/<c>Modules.Procurement.Domain.PurchaseOrderLine</c>).
/// Maps a quantity/rate breakdown onto a real WBS/Controlling object (docs/architecture/
/// 07-project-accounting-and-financial-architecture.md §4) — <see cref="WbsElementId"/> must be one of the
/// Contract's own Project's WBS elements, validated at <c>ContractService.CreateAsync</c> time via
/// <c>Modules.ProjectManagement.Contracts.IProjectLookup</c>, not re-validated here (this type has no
/// dependency on that module). Not restricted to a specific WBS flag (billing vs. account-assignment) in
/// this slice — that distinction becomes load-bearing once Site Progress/Measurement (a later slice) posts
/// against it.
/// </summary>
public sealed class BoqLine
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public string? DescriptionArabic { get; private set; }
    public string UnitOfMeasure { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Rate { get; private set; }
    public decimal Amount => Quantity * Rate;
    public Guid WbsElementId { get; private set; }

    internal BoqLine(
        string code, string description, string? descriptionArabic, string unitOfMeasure,
        decimal quantity, decimal rate, Guid wbsElementId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(unitOfMeasure);
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (rate <= 0) throw new ArgumentException("Rate must be greater than zero.", nameof(rate));

        Id = Guid.NewGuid();
        Code = code;
        Description = description;
        DescriptionArabic = descriptionArabic;
        UnitOfMeasure = unitOfMeasure;
        Quantity = quantity;
        Rate = rate;
        WbsElementId = wbsElementId;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private BoqLine()
    {
        Code = null!;
        Description = null!;
        UnitOfMeasure = null!;
    }

    /// <summary>Applied by an Approved <see cref="VariationOrder"/> line against this line — the one way a
    /// BOQ line's Quantity changes after the Contract itself has left Draft. <paramref name="delta"/> may be
    /// negative (an omission), but the resulting quantity can never drop to zero or below.</summary>
    internal void AdjustQuantity(decimal delta)
    {
        var updated = Quantity + delta;
        if (updated <= 0)
            throw new ArgumentException($"Adjusting line '{Code}' by {delta} would bring its quantity to {updated}, which is not allowed.");
        Quantity = updated;
    }
}
