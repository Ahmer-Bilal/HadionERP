using Microsoft.EntityFrameworkCore;
using Platform.Core.NumberRanges;

namespace Modules.Finance.Infrastructure;

/// <summary>Database-backed <see cref="INumberRangeService"/> for Modules.Finance — a near-duplicate of
/// Modules.MasterData's own <c>EfCoreNumberRangeService</c> (same atomic
/// <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> pattern), backed by this module's own
/// <see cref="FinanceDbContext"/>/<c>finance.number_range_counters</c> table rather than MasterData's —
/// see <see cref="NumberRangeCounterEntity"/>'s doc comment for why this can't just be shared.</summary>
public sealed class EfCoreNumberRangeService : INumberRangeService
{
    private readonly FinanceDbContext _dbContext;
    private readonly Dictionary<string, NumberRangeDefinition> _definitions;

    public EfCoreNumberRangeService(FinanceDbContext dbContext, IEnumerable<NumberRangeDefinition> definitions)
    {
        _dbContext = dbContext;
        _definitions = definitions.ToDictionary(d => d.RangeKey);
    }

    public string GetNext(string rangeKey, string companyId, int fiscalYear)
    {
        if (!_definitions.TryGetValue(rangeKey, out var definition))
        {
            throw new InvalidOperationException($"No number range definition registered for key '{rangeKey}'.");
        }

        var sequence = _dbContext.Database
            .SqlQuery<long>($@"
                INSERT INTO finance.number_range_counters (range_key, company_id, fiscal_year, last_sequence)
                VALUES ({rangeKey}, {companyId}, {fiscalYear}, 1)
                ON CONFLICT (range_key, company_id, fiscal_year)
                DO UPDATE SET last_sequence = finance.number_range_counters.last_sequence + 1
                RETURNING last_sequence")
            .AsEnumerable()
            .Single();

        return definition.Format(fiscalYear, sequence);
    }
}
