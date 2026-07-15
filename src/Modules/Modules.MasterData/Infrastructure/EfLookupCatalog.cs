using Modules.MasterData.Application;
using Modules.MasterData.Contracts;

namespace Modules.MasterData.Infrastructure;

/// <summary>Implements the published <see cref="ILookupCatalog"/> contract by delegating to this module's
/// own <see cref="ILookupRepository"/> — same "thin Contracts wrapper over the real Application-layer
/// port" shape as every other <c>Ef*Lookup</c> class in this module (e.g. <c>EfItemLookup</c>).</summary>
public sealed class EfLookupCatalog : ILookupCatalog
{
    private readonly ILookupRepository _repository;

    public EfLookupCatalog(ILookupRepository repository) => _repository = repository;

    public async Task<LookupValueSummary?> GetValueAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default)
    {
        var value = await _repository.GetValueByCodeAsync(lookupTypeCode, valueCode, cancellationToken);
        return value is null ? null : new LookupValueSummary(value.LookupTypeCode, value.Code, value.Name, value.NameArabic, value.IsActive);
    }
}
