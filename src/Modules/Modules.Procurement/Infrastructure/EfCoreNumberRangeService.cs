using Microsoft.EntityFrameworkCore;
using Platform.Core.NumberRanges;

namespace Modules.Procurement.Infrastructure;

/// <summary>Database-backed <see cref="INumberRangeService"/> for Modules.Procurement — a near-duplicate of
/// Modules.Finance's own <c>EfCoreNumberRangeService</c>, backed by this module's own
/// <see cref="ProcurementDbContext"/>/<c>procurement.number_range_counters</c> table.</summary>
public sealed class EfCoreNumberRangeService : INumberRangeService
{
    private readonly ProcurementDbContext _dbContext;
    private readonly Dictionary<string, NumberRangeDefinition> _definitions;

    public EfCoreNumberRangeService(ProcurementDbContext dbContext, IEnumerable<NumberRangeDefinition> definitions)
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
                INSERT INTO procurement.number_range_counters (range_key, company_id, fiscal_year, last_sequence)
                VALUES ({rangeKey}, {companyId}, {fiscalYear}, 1)
                ON CONFLICT (range_key, company_id, fiscal_year)
                DO UPDATE SET last_sequence = procurement.number_range_counters.last_sequence + 1
                RETURNING last_sequence")
            .AsEnumerable()
            .Single();

        return definition.Format(fiscalYear, sequence);
    }
}
