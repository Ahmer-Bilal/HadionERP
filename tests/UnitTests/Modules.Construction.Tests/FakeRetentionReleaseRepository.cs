using Modules.Construction.Application;
using Modules.Construction.Domain;

namespace Modules.Construction.Tests;

internal sealed class FakeRetentionReleaseRepository : IRetentionReleaseRepository
{
    private readonly Dictionary<Guid, RetentionRelease> _items = new();

    public Task<RetentionRelease?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<RetentionRelease>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RetentionRelease>>(_items.Values.OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public Task<IReadOnlyList<RetentionRelease>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RetentionRelease>>(_items.Values
            .Where(r => r.CommercialDocumentType == commercialDocumentType && r.CommercialDocumentId == commercialDocumentId)
            .ToList());

    public void Add(RetentionRelease retentionRelease) => _items[retentionRelease.Id] = retentionRelease;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
