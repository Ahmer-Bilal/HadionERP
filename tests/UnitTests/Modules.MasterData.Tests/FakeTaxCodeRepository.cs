using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

internal sealed class FakeTaxCodeRepository : ITaxCodeRepository
{
    private readonly Dictionary<Guid, TaxCode> _taxCodes = new();

    public Task<TaxCode?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_taxCodes.GetValueOrDefault(id));

    public Task<TaxCode?> GetByCodeAsync(string taxCodeCode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_taxCodes.Values.FirstOrDefault(t => t.TaxCodeCode == taxCodeCode));

    public Task<IReadOnlyList<TaxCode>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TaxCode>>(
            _taxCodes.Values.OrderBy(t => t.TaxCodeCode).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_taxCodes.Count);

    public void Add(TaxCode taxCode) => _taxCodes[taxCode.Id] = taxCode;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
