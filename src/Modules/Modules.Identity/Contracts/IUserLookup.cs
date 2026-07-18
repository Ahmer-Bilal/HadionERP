namespace Modules.Identity.Contracts;

/// <summary>
/// The published, read-only view of a User another module (Finance) may depend on — same Contracts-package
/// rule as <c>Modules.MasterData.Contracts.IBusinessPartnerLookup</c>. Finance needs to know "does this
/// person exist, are they active, what are they authorized to do" (the Period Closing Center's real
/// per-person duty assignment — `UI/Finance/d1f20165-...png`) — not User's own maintenance concerns
/// (password hash, SoD-conflicted role assignment).
/// </summary>
public sealed record UserSummary(
    Guid Id,
    string Username,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> RoleKeys);

/// <summary>Read-only lookup another module calls to validate/display a User reference (e.g. a Closing
/// Activity's assignee). Implemented in Modules.Identity.Infrastructure, registered in Gateway.Api's DI
/// container.</summary>
public interface IUserLookup
{
    Task<UserSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every active User — the pool a Finance Manager assigns a Closing Activity's ownership from.
    /// Small enough (real companies run this on tens, not millions, of users) that a flat list beats paging
    /// for this specific "pick an assignee" use, the same reasoning
    /// <c>IGLAccountLookup.ListAllAsync</c> already established for Trial Balance's own account list.</summary>
    Task<IReadOnlyList<UserSummary>> ListActiveAsync(CancellationToken cancellationToken = default);
}
