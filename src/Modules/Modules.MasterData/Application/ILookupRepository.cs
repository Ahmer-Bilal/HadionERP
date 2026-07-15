using Modules.MasterData.Domain;

namespace Modules.MasterData.Application;

/// <summary>The persistence port for the admin-configurable lookup engine — same dependency-inversion
/// shape as every other module repository (e.g. <see cref="ITaxCodeRepository"/>), plus
/// <see cref="IsValueInUseAsync"/>, which lets <c>LookupService.DeleteValueAsync</c> refuse to delete a
/// value still referenced by real business data (the same "block, don't corrupt" behavior real SAP domain-
/// value maintenance enforces) without the Application layer needing to know each consuming table's shape.</summary>
public interface ILookupRepository
{
    Task<LookupType?> GetTypeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupType>> ListTypesAsync(CancellationToken cancellationToken = default);

    void AddType(LookupType lookupType);

    void RemoveType(LookupType lookupType);

    Task<LookupValue?> GetValueAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LookupValue?> GetValueByCodeAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupValue>> ListValuesAsync(string lookupTypeCode, bool includeInactive, CancellationToken cancellationToken = default);

    Task<int> CountValuesAsync(string lookupTypeCode, CancellationToken cancellationToken = default);

    void AddValue(LookupValue lookupValue);

    void RemoveValue(LookupValue lookupValue);

    /// <summary>True if any real business record currently references this value's code — e.g. a
    /// <c>Country</c> value used on a Business Partner address, a <c>BusinessRoleType</c> value held by a
    /// partner's role, an <c>AddressType</c>/<c>UnitOfMeasure</c> value in active use. Types with no wired
    /// consumer yet (a brand-new admin-created lookup type) always report false.</summary>
    Task<bool> IsValueInUseAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
