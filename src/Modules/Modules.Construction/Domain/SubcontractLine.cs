namespace Modules.Construction.Domain;

/// <summary>
/// One line of a <see cref="Subcontract"/>'s scope-of-work breakdown — a child entity, not an independent
/// Business Object, same "0..n child collection, only exists through its parent" pattern as
/// <see cref="BoqLine"/>. <see cref="WbsElementId"/> must be one of the Subcontract's own Project's WBS
/// elements, validated at <c>SubcontractService.CreateAsync</c> time via
/// <c>Modules.ProjectManagement.Contracts.IProjectLookup</c>, not re-validated here (this type has no
/// dependency on that module). Not linked back to a specific parent <see cref="Contract"/>'s
/// <see cref="BoqLine"/> this slice — real back-to-back subcontracts often scope a specific BOQ item, but
/// that per-line traceability is deferred, disclosed in the module README, same judgment call as
/// <see cref="BoqLine"/>'s own "no WBS flag enforcement yet."
/// </summary>
public sealed class SubcontractLine
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

    internal SubcontractLine(
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
    private SubcontractLine()
    {
        Code = null!;
        Description = null!;
        UnitOfMeasure = null!;
    }
}
