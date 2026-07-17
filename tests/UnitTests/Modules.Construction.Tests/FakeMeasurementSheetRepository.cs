using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeMeasurementSheetRepository : IMeasurementSheetRepository
{
    private readonly Dictionary<Guid, MeasurementSheet> _items = new();

    public Task<MeasurementSheet?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<MeasurementSheet>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MeasurementSheet>>(
            _items.Values.OrderByDescending(s => s.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public Task<IReadOnlyList<MeasurementSheet>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MeasurementSheet>>(
            _items.Values.Where(s => s.CommercialDocumentType == commercialDocumentType && s.CommercialDocumentId == commercialDocumentId).ToList());

    public void Add(MeasurementSheet measurementSheet) => _items[measurementSheet.Id] = measurementSheet;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
