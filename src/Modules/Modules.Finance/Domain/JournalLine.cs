namespace Modules.Finance.Domain;

/// <summary>
/// One line of a <see cref="JournalEntry"/> — a child entity, not an independent Business Object, same
/// "0..n child collection, only exists through its parent" pattern as
/// Modules.MasterData's <c>BusinessPartnerAddress</c>
/// (docs/architecture/02-business-object-model.md #1). References a G/L Account and, optionally, a Cost
/// Center — both by <see cref="Guid"/> only, resolved and validated through
/// <c>Modules.MasterData.Contracts.IGLAccountLookup</c>/a future cost-center lookup at the Application
/// layer, never through a direct reference to Modules.MasterData's own types
/// (docs/architecture/01-architecture-foundation.md #3.2 — Finance may depend on MasterData's published
/// Contracts only).
///
/// Exactly one of <see cref="DebitAmount"/>/<see cref="CreditAmount"/> is non-zero on any given line — the
/// standard double-entry convention (a single line is either a debit or a credit, never both, never
/// neither) enforced by <see cref="JournalEntry.AddLine"/>.
/// </summary>
public sealed class JournalLine
{
    public Guid Id { get; private set; }
    public Guid GLAccountId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public string? LineDescription { get; private set; }

    internal JournalLine(Guid glAccountId, Guid? costCenterId, decimal debitAmount, decimal creditAmount, string? lineDescription)
    {
        Id = Guid.NewGuid();
        GLAccountId = glAccountId;
        CostCenterId = costCenterId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        LineDescription = lineDescription;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private JournalLine()
    {
    }
}
