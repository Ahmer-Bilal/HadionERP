using System.Collections.Concurrent;

namespace Platform.Core.NumberRanges;

/// <summary>
/// Reference implementation of <see cref="INumberRangeService"/> that counts in memory, per
/// (range, company, fiscal year). This is what proves the kernel's numbering contract works in Phase 0
/// tests; a real deployment swaps this for a database-backed implementation behind the same interface
/// (Dependency Inversion, per docs/architecture/01-overview.md §1) — module/BO code never
/// changes when that swap happens.
/// </summary>
public sealed class InMemoryNumberRangeService : INumberRangeService
{
    private readonly Dictionary<string, NumberRangeDefinition> _definitions;
    private readonly ConcurrentDictionary<string, long> _counters = new();

    public InMemoryNumberRangeService(IEnumerable<NumberRangeDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(d => d.RangeKey);
    }

    public string GetNext(string rangeKey, string companyId, int fiscalYear)
    {
        if (!_definitions.TryGetValue(rangeKey, out var definition))
        {
            throw new InvalidOperationException($"No number range definition registered for key '{rangeKey}'.");
        }

        var counterKey = $"{rangeKey}|{companyId}|{fiscalYear}";
        var sequence = _counters.AddOrUpdate(counterKey, 1, (_, current) => current + 1);

        return definition.Format(fiscalYear, sequence);
    }
}
