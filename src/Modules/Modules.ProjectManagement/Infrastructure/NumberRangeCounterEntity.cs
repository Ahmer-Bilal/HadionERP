namespace Modules.ProjectManagement.Infrastructure;

/// <summary>Persisted counter backing <see cref="EfCoreNumberRangeService"/> — a near-duplicate of every
/// other module's own copy of the same shape, for the same "each module owns its own schema" reason
/// documented on <c>Modules.Finance.Infrastructure.NumberRangeCounterEntity</c>.</summary>
public sealed class NumberRangeCounterEntity
{
    public string RangeKey { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public int FiscalYear { get; set; }
    public long LastSequence { get; set; }
}
