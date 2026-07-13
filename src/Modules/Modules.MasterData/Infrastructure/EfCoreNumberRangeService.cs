using Microsoft.EntityFrameworkCore;
using Platform.Core.NumberRanges;

namespace Modules.MasterData.Infrastructure;

/// <summary>
/// Database-backed <see cref="INumberRangeService"/> — swaps in for Platform.Core's
/// <c>InMemoryNumberRangeService</c> (which is fine for kernel proof-of-concept use, but would duplicate
/// document numbers on every restart for real persisted business data) without any caller needing to
/// change, per the interface-swap pattern used throughout the platform.
///
/// Uses a single atomic <c>INSERT ... ON CONFLICT DO UPDATE ... RETURNING</c> statement rather than a
/// read-then-write round trip — that naive approach would let two concurrent requests both read the same
/// counter value and hand out the same document number. This is a single statement Postgres executes
/// atomically under its own row-level locking, so no application-level lock is needed.
/// </summary>
public sealed class EfCoreNumberRangeService : INumberRangeService
{
    private readonly MasterDataDbContext _dbContext;
    private readonly Dictionary<string, NumberRangeDefinition> _definitions;

    public EfCoreNumberRangeService(MasterDataDbContext dbContext, IEnumerable<NumberRangeDefinition> definitions)
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
                INSERT INTO masterdata.number_range_counters (range_key, company_id, fiscal_year, last_sequence)
                VALUES ({rangeKey}, {companyId}, {fiscalYear}, 1)
                ON CONFLICT (range_key, company_id, fiscal_year)
                DO UPDATE SET last_sequence = masterdata.number_range_counters.last_sequence + 1
                RETURNING last_sequence")
            .AsEnumerable()
            .Single();

        return definition.Format(fiscalYear, sequence);
    }
}
