using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Infrastructure;

public sealed class EfLookupRepository : ILookupRepository
{
    private readonly MasterDataDbContext _dbContext;

    public EfLookupRepository(MasterDataDbContext dbContext) => _dbContext = dbContext;

    public Task<LookupType?> GetTypeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.LookupTypes.FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public async Task<IReadOnlyList<LookupType>> ListTypesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.LookupTypes.AsNoTracking().OrderBy(t => t.Code).ToListAsync(cancellationToken);

    public void AddType(LookupType lookupType) => _dbContext.LookupTypes.Add(lookupType);

    public void RemoveType(LookupType lookupType) => _dbContext.LookupTypes.Remove(lookupType);

    public Task<LookupValue?> GetValueAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.LookupValues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<LookupValue?> GetValueByCodeAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default) =>
        _dbContext.LookupValues.FirstOrDefaultAsync(v => v.LookupTypeCode == lookupTypeCode && v.Code == valueCode, cancellationToken);

    public async Task<IReadOnlyList<LookupValue>> ListValuesAsync(string lookupTypeCode, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.LookupValues.AsNoTracking().Where(v => v.LookupTypeCode == lookupTypeCode);
        if (!includeInactive) query = query.Where(v => v.IsActive);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<int> CountValuesAsync(string lookupTypeCode, CancellationToken cancellationToken = default) =>
        _dbContext.LookupValues.CountAsync(v => v.LookupTypeCode == lookupTypeCode, cancellationToken);

    public void AddValue(LookupValue lookupValue) => _dbContext.LookupValues.Add(lookupValue);

    public void RemoveValue(LookupValue lookupValue) => _dbContext.LookupValues.Remove(lookupValue);

    /// <summary>Each retrofitted lookup type maps to the one column it now backs — see
    /// <c>docs/module/master-data.md</c>'s Lookup Data section for which fields
    /// are wired. A lookup type with no wired consumer (a brand-new admin-created one, or a seeded type not
    /// yet referenced by any field) always reports false — nothing blocks deleting its values.</summary>
    public async Task<bool> IsValueInUseAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default)
    {
        return lookupTypeCode switch
        {
            "Country" => await _dbContext.Set<BusinessPartnerAddress>().AsNoTracking()
                .AnyAsync(a => a.Country == valueCode, cancellationToken),
            "BusinessRoleType" => await _dbContext.Set<BusinessRole>().AsNoTracking()
                .AnyAsync(r => r.RoleType == valueCode, cancellationToken),
            "AddressType" => await _dbContext.Set<BusinessPartnerAddress>().AsNoTracking()
                .AnyAsync(a => a.AddressType == valueCode, cancellationToken),
            "UnitOfMeasure" => await _dbContext.Items.AsNoTracking()
                .AnyAsync(i => i.UnitOfMeasure == valueCode, cancellationToken),
            _ => false,
        };
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
