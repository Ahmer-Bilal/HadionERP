namespace Modules.Procurement.Infrastructure;

/// <summary>Persisted counter backing <see cref="EfCoreNumberRangeService"/> — a near-duplicate of
/// Modules.MasterData's and Modules.Finance's own copies of the same shape, for the same "each module owns
/// its own schema" reason documented on <c>Modules.Finance.Infrastructure.NumberRangeCounterEntity</c>.</summary>
public sealed class NumberRangeCounterEntity
{
    public string RangeKey { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public int FiscalYear { get; set; }
    public long LastSequence { get; set; }
}
