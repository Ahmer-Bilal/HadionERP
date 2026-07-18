using Modules.Finance.Domain;

namespace Modules.Finance.Application;

/// <summary>The persistence port for Closing Activities — same dependency-inversion shape as
/// <see cref="IBudgetRepository"/>.</summary>
public interface IClosingActivityRepository
{
    Task<ClosingActivity?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClosingActivity>> ListForPeriodAsync(Guid fiscalPeriodId, CancellationToken cancellationToken = default);

    void Add(ClosingActivity activity);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
