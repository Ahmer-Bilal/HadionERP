namespace Modules.Finance.Infrastructure;

/// <summary>Persisted counter backing <see cref="EfCoreNumberRangeService"/> — one row per (range, company,
/// fiscal year). A near-duplicate of Modules.MasterData's own copy of the same shape, deliberately: number
/// ranges are Platform.Core kernel infrastructure, but the only real implementation lives per-module
/// against that module's own DbContext (each module owns its own schema — docs/architecture/01
/// §3.2 — so Finance cannot share MasterData's `masterdata.number_range_counters` table without reaching
/// into MasterData's Infrastructure directly, which the Contracts-package rule forbids).</summary>
public sealed class NumberRangeCounterEntity
{
    public string RangeKey { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public int FiscalYear { get; set; }
    public long LastSequence { get; set; }
}
