using Modules.Finance.Domain;

namespace Modules.Finance.Application;

/// <summary>The persistence port for Fiscal Years (and, through them, their Periods) — same
/// dependency-inversion shape as <see cref="IBudgetRepository"/>.</summary>
public interface IFiscalYearRepository
{
    Task<FiscalYear?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FiscalYear?> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalYear>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>The one <see cref="FiscalPeriod"/> (across every <see cref="FiscalYear"/> on file) whose
    /// date range contains <paramref name="date"/>, if any — the exact lookup
    /// <c>JournalEntryService</c>'s posting-time period check needs, without loading a full
    /// <see cref="FiscalYear"/> aggregate just to ask "is this one date's period open."</summary>
    Task<FiscalPeriod?> GetPeriodByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    void Add(FiscalYear fiscalYear);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
