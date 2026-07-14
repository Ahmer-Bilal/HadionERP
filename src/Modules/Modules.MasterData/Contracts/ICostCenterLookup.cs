namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of a Cost Center another module (Finance) may depend on — same
/// Contracts-package rule as <see cref="IGLAccountLookup"/>. Finance needs to know "does this cost center
/// exist, is it postable," not Cost Center's own maintenance concerns (parent hierarchy, lifecycle status).
/// </summary>
public sealed record CostCenterSummary(
    Guid Id,
    string CostCenterCode,
    string CostCenterName,
    bool IsPostable,
    bool IsActive);

/// <summary>Read-only lookup Finance calls to validate a Cost Center reference (e.g. on a Journal Line)
/// before posting. Implemented in Modules.MasterData.Infrastructure, registered in Gateway.Api's DI
/// container.</summary>
public interface ICostCenterLookup
{
    Task<CostCenterSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
