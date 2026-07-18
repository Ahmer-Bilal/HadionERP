using Modules.MasterData.Contracts;

namespace Modules.Finance.Application;

/// <summary>
/// The Trial Balance report — the first Finance reporting slice (see UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md),
/// and deliberately not a new Business Object: it's a pure aggregation over data two existing Business
/// Objects already produce — <see cref="JournalEntry"/>'s posted lines, classified against
/// <c>Modules.MasterData</c>'s Chart of Accounts (read here only through the published
/// <see cref="IGLAccountLookup"/> contract, never MasterData's own Domain/Infrastructure —
/// docs/architecture/01-overview.md §3.2). Everything else in this report family (Income Statement, Balance
/// Sheet) will reuse this same opening/period/ending aggregation, just presented differently.
/// </summary>
public sealed class TrialBalanceService
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IGLAccountLookup _glAccountLookup;

    public TrialBalanceService(IJournalEntryRepository journalEntryRepository, IGLAccountLookup glAccountLookup)
    {
        _journalEntryRepository = journalEntryRepository;
        _glAccountLookup = glAccountLookup;
    }

    public async Task<TrialBalanceDto> GetAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default)
    {
        if (periodEnd < periodStart)
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));

        var accounts = await _glAccountLookup.ListAllAsync(cancellationToken);
        var entries = await _journalEntryRepository.ListPostedAsync(periodEnd, cancellationToken);

        // Own (line-level, not rolled up) Opening/Period Debit/Credit per account — only postable (leaf)
        // accounts ever have lines, since JournalEntryService only allows posting to a postable account.
        var own = accounts.ToDictionary(a => a.Id, _ => (OpeningDr: 0m, OpeningCr: 0m, PeriodDr: 0m, PeriodCr: 0m));
        foreach (var entry in entries)
        {
            var isOpening = entry.PostingDate < periodStart;
            foreach (var line in entry.Lines)
            {
                if (!own.TryGetValue(line.GLAccountId, out var totals)) continue; // orphaned reference — skip rather than throw on a report
                own[line.GLAccountId] = isOpening
                    ? (totals.OpeningDr + line.DebitAmount, totals.OpeningCr + line.CreditAmount, totals.PeriodDr, totals.PeriodCr)
                    : (totals.OpeningDr, totals.OpeningCr, totals.PeriodDr + line.DebitAmount, totals.PeriodCr + line.CreditAmount);
            }
        }

        // Roll each account's own totals up through its parent chain — process deepest accounts first so a
        // grandchild's amounts are already folded into its parent before that parent folds into its own
        // parent in turn (single O(n log n) pass, no recursion).
        var childrenByParent = accounts.Where(a => a.ParentAccountId.HasValue).ToLookup(a => a.ParentAccountId!.Value);
        var level = ComputeLevels(accounts);
        var rolled = new Dictionary<Guid, (decimal OpeningDr, decimal OpeningCr, decimal PeriodDr, decimal PeriodCr)>(own);
        foreach (var account in accounts.OrderByDescending(a => level[a.Id]))
        {
            if (account.ParentAccountId is not { } parentId || !rolled.ContainsKey(parentId)) continue;
            var mine = rolled[account.Id];
            var parent = rolled[parentId];
            rolled[parentId] = (parent.OpeningDr + mine.OpeningDr, parent.OpeningCr + mine.OpeningCr,
                                 parent.PeriodDr + mine.PeriodDr, parent.PeriodCr + mine.PeriodCr);
        }

        var rows = accounts
            .Select(a =>
            {
                var t = rolled[a.Id];
                return new TrialBalanceAccountDto(
                    a.Id, a.AccountCode, a.AccountName, a.AccountType, level[a.Id], IsHeader: !a.IsPostable,
                    t.OpeningDr, t.OpeningCr, t.PeriodDr, t.PeriodCr,
                    t.OpeningDr + t.PeriodDr, t.OpeningCr + t.PeriodCr);
            })
            .OrderBy(r => r.AccountCode, StringComparer.Ordinal)
            .ToList();

        // Totals are summed from each account's own (unrolled) amounts, not the rolled ones — summing rolled
        // header totals too would double-count everything already folded up from their leaves.
        var totalDebit = accounts.Sum(a => own[a.Id].OpeningDr + own[a.Id].PeriodDr);
        var totalCredit = accounts.Sum(a => own[a.Id].OpeningCr + own[a.Id].PeriodCr);

        return new TrialBalanceDto(periodStart, periodEnd, rows, totalDebit, totalCredit);
    }

    /// <summary>0 for a top-level account, parent's level + 1 otherwise. Walks each account's parent chain
    /// iteratively (never recursively) collecting not-yet-leveled ancestors into <c>chain</c>, stopping at
    /// whichever comes first: a root (no parent), an already-leveled ancestor, or a repeated id — the last
    /// case means a parent cycle, a data-integrity bug this report tolerates (bounds it to a level) rather
    /// than looping forever over.</summary>
    private static Dictionary<Guid, int> ComputeLevels(IReadOnlyList<GLAccountSummary> accounts)
    {
        var parentOf = accounts.ToDictionary(a => a.Id, a => a.ParentAccountId);
        var levels = new Dictionary<Guid, int>();

        foreach (var account in accounts)
        {
            if (levels.ContainsKey(account.Id)) continue;

            var chain = new List<Guid>();
            var visited = new HashSet<Guid>();
            Guid? currentId = account.Id;
            while (currentId is { } id && !levels.ContainsKey(id) && visited.Add(id))
            {
                chain.Add(id);
                currentId = parentOf.TryGetValue(id, out var parentId) ? parentId : null;
            }

            var startLevel = currentId is { } knownId && levels.TryGetValue(knownId, out var known) ? known : -1;
            for (var i = chain.Count - 1; i >= 0; i--)
                levels[chain[i]] = startLevel + (chain.Count - i);
        }

        return levels;
    }
}
