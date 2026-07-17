namespace Modules.Construction.Domain;

/// <summary>
/// A negative line item recorded against an Approved <see cref="Subcontract"/> — a cost recovery/damage
/// deduction (rework, delay, material recovery) against what the subcontractor is ultimately owed, per
/// ROADMAP.md's Phase 3 scope ("Subcontracts... back-charges — a distinct document
/// type from a standard PO"). Not yet wired to any actual Payment deduction (`Modules.Finance.Payment` has
/// no concept of a Subcontract or its back-charges) — that integration is a later slice, once IPC/Payment
/// against Subcontracts exists; today this only reduces the computed <see cref="Subcontract.NetPayableValue"/>
/// figure shown on the document itself.
/// </summary>
public sealed class BackCharge
{
    public Guid Id { get; private set; }
    public string Description { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly DateIncurred { get; private set; }

    internal BackCharge(string description, decimal amount, DateOnly dateIncurred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (amount <= 0) throw new ArgumentException("Back charge amount must be greater than zero.", nameof(amount));

        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        DateIncurred = dateIncurred;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private BackCharge()
    {
        Description = null!;
    }
}
