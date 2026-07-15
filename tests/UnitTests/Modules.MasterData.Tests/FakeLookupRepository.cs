using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

internal sealed class FakeLookupRepository : ILookupRepository
{
    private readonly Dictionary<string, LookupType> _types = new();
    private readonly Dictionary<Guid, LookupValue> _values = new();
    private readonly HashSet<(string TypeCode, string Code)> _inUse = new();

    /// <summary>Test hook: pretends a real business record references this (type, code) pair, so
    /// <see cref="IsValueInUseAsync"/> reports true for it — exercises <c>LookupService.DeleteValueAsync</c>'s
    /// in-use protection without a real EF-backed cross-table query.</summary>
    public void MarkInUse(string lookupTypeCode, string valueCode) => _inUse.Add((lookupTypeCode, valueCode));

    /// <summary>Seeded with the same values Gateway.Api's real startup seeder provisions for the four
    /// retrofitted lookup types, trimmed to what these unit tests actually reference — enough for
    /// BusinessPartnerServiceTests/ItemServiceTests to exercise real validation instead of a stub that
    /// always says "valid."</summary>
    public static FakeLookupRepository WithDefaults()
    {
        var repo = new FakeLookupRepository();
        repo.SeedType("BusinessRoleType", "Business Role Type", isSystemDefined: true,
            "Client", "Supplier", "Subcontractor", "Consultant", "JointVenturePartner",
            "GovernmentAuthority", "RentalCompany", "Manufacturer", "ManpowerSupplier", "TestingLaboratory");
        repo.SeedType("AddressType", "Address Type", isSystemDefined: true,
            "HeadOffice", "Billing", "Shipping", "SiteOffice");
        repo.SeedType("Country", "Country", isSystemDefined: true,
            "Saudi Arabia", "United Arab Emirates", "Kuwait", "Bahrain", "Qatar", "Oman", "Egypt");
        repo.SeedType("UnitOfMeasure", "Unit of Measure", isSystemDefined: true,
            "EA", "KG", "M3", "M2", "TON", "LM", "HR", "BAG", "LTR");
        return repo;
    }

    private void SeedType(string code, string name, bool isSystemDefined, params string[] valueCodes)
    {
        var type = new LookupType("seed", code, name, null, isSystemDefined);
        _types[code] = type;
        var sortOrder = 0;
        foreach (var valueCode in valueCodes)
        {
            var value = new LookupValue("seed", code, valueCode, valueCode, null, sortOrder++);
            _values[value.Id] = value;
        }
    }

    public Task<LookupType?> GetTypeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_types.GetValueOrDefault(code));

    public Task<IReadOnlyList<LookupType>> ListTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LookupType>>(_types.Values.OrderBy(t => t.Code).ToList());

    public void AddType(LookupType lookupType) => _types[lookupType.Code] = lookupType;

    public void RemoveType(LookupType lookupType) => _types.Remove(lookupType.Code);

    public Task<LookupValue?> GetValueAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_values.GetValueOrDefault(id));

    public Task<LookupValue?> GetValueByCodeAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_values.Values.FirstOrDefault(v => v.LookupTypeCode == lookupTypeCode && v.Code == valueCode));

    public Task<IReadOnlyList<LookupValue>> ListValuesAsync(string lookupTypeCode, bool includeInactive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LookupValue>>(
            _values.Values.Where(v => v.LookupTypeCode == lookupTypeCode && (includeInactive || v.IsActive)).ToList());

    public Task<int> CountValuesAsync(string lookupTypeCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_values.Values.Count(v => v.LookupTypeCode == lookupTypeCode));

    public void AddValue(LookupValue lookupValue) => _values[lookupValue.Id] = lookupValue;

    public void RemoveValue(LookupValue lookupValue) => _values.Remove(lookupValue.Id);

    public Task<bool> IsValueInUseAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_inUse.Contains((lookupTypeCode, valueCode)));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
