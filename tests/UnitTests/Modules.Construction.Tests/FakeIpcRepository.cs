using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeIpcRepository : IIpcRepository
{
    private readonly Dictionary<Guid, Ipc> _items = new();

    public Task<Ipc?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Ipc>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Ipc>>(_items.Values.OrderByDescending(i => i.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public Task<bool> ExistsForMeasurementSheetAsync(Guid measurementSheetId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Any(i => i.MeasurementSheetId == measurementSheetId));

    public Task<IReadOnlyList<Ipc>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Ipc>>(_items.Values
            .Where(i => i.CommercialDocumentType == commercialDocumentType && i.CommercialDocumentId == commercialDocumentId)
            .ToList());

    public void Add(Ipc ipc) => _items[ipc.Id] = ipc;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
