namespace Modules.MasterData.Infrastructure;

/// <summary>Persisted counter backing <see cref="EfCoreNumberRangeService"/> — one row per
/// (range, company, fiscal year), so document numbers survive an application restart (the in-memory
/// version in Platform.Core is fine for the kernel's own proof-of-concept use, but would duplicate
/// numbers on every restart for real persisted business data).</summary>
public sealed class NumberRangeCounterEntity
{
    public string RangeKey { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public int FiscalYear { get; set; }
    public long LastSequence { get; set; }
}
