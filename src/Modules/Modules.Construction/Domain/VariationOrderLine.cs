namespace Modules.Construction.Domain;

/// <summary>
/// One line of a <see cref="VariationOrder"/> — either an adjustment to an existing commercial document
/// line's quantity (<see cref="CommercialDocumentLineId"/> set, <see cref="QuantityDelta"/> may be negative,
/// an omission), or a wholly new line the Variation Order introduces (<see cref="CommercialDocumentLineId"/>
/// null, <see cref="QuantityDelta"/> is that new line's full quantity and must be positive, and the
/// <c>Code</c>/<c>Description</c>/<c>UnitOfMeasure</c>/<c>WbsElementId</c> fields describe it). Exactly one
/// of those two shapes, never a mix — enforced by the two separate factory methods on <see cref="VariationOrder"/>.
/// </summary>
public sealed class VariationOrderLine
{
    public Guid Id { get; private set; }
    public Guid? CommercialDocumentLineId { get; private set; }
    public string? Code { get; private set; }
    public string? Description { get; private set; }
    public string? DescriptionArabic { get; private set; }
    public string? UnitOfMeasure { get; private set; }
    public Guid? WbsElementId { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal Rate { get; private set; }
    public decimal Amount => QuantityDelta * Rate;

    internal VariationOrderLine(Guid commercialDocumentLineId, decimal quantityDelta, decimal rate)
    {
        if (quantityDelta == 0) throw new ArgumentException("Quantity delta cannot be zero.", nameof(quantityDelta));
        if (rate <= 0) throw new ArgumentException("Rate must be greater than zero.", nameof(rate));

        Id = Guid.NewGuid();
        CommercialDocumentLineId = commercialDocumentLineId;
        QuantityDelta = quantityDelta;
        Rate = rate;
    }

    internal VariationOrderLine(
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
        WbsElementId = wbsElementId;
        QuantityDelta = quantity;
        Rate = rate;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private VariationOrderLine()
    {
    }
}
