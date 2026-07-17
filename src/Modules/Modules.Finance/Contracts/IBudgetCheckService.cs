namespace Modules.Finance.Contracts;

/// <summary>
/// The outcome of a budget check — <see cref="Allowed"/> false means the caller must not proceed (e.g. must
/// not release a Purchase Order) without an override; <see cref="Reason"/> is the human-readable explanation
/// shown to the user, populated whenever <see cref="Allowed"/> is false.
/// </summary>
public sealed record BudgetCheckResult(bool Allowed, string? Reason);

/// <summary>
/// The synchronous cross-module contract call docs/architecture/01-overview.md §3.2 names as
/// its own worked example: "Procurement asks Finance's <c>IBudgetCheckService</c> before releasing a PO."
/// Implemented in Modules.Finance.Infrastructure, registered in Gateway.Api's DI container — Procurement
/// depends on this Contracts package only, never on Finance's own Domain/Infrastructure/Application
/// internals (docs/architecture/01-overview.md §3.2 rule 2).
/// </summary>
public interface IBudgetCheckService
{
    Task<BudgetCheckResult> CheckAsync(Guid costCenterId, decimal amount, CancellationToken cancellationToken = default);
}
