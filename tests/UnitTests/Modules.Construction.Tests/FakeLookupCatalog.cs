using Modules.MasterData.Contracts;

namespace Modules.Construction.Tests;

/// <summary>Seeded with the same "ContractType"/"UnitOfMeasure" values the real startup seeder provisions
/// (<c>Modules.MasterData.Infrastructure.LookupSeeder</c>), trimmed to what these unit tests reference.</summary>
internal sealed class FakeLookupCatalog : ILookupCatalog
{
    private readonly Dictionary<(string, string), LookupValueSummary> _values = new()
    {
        [("ContractType", "LumpSum")] = new LookupValueSummary("ContractType", "LumpSum", "Lump Sum", null, true),
        [("ContractType", "UnitPrice")] = new LookupValueSummary("ContractType", "UnitPrice", "Unit Price", null, true),
        [("UnitOfMeasure", "M2")] = new LookupValueSummary("UnitOfMeasure", "M2", "Square Meter", null, true),
        [("UnitOfMeasure", "M3")] = new LookupValueSummary("UnitOfMeasure", "M3", "Cubic Meter", null, true),
    };

    public Task<LookupValueSummary?> GetValueAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_values.GetValueOrDefault((lookupTypeCode, valueCode)));
}
