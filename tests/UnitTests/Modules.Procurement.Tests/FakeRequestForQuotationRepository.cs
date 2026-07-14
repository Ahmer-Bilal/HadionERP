using Modules.Procurement.Application;
using Modules.Procurement.Domain;

namespace Modules.Procurement.Tests;

internal sealed class FakeRequestForQuotationRepository : IRequestForQuotationRepository
{
    private readonly Dictionary<Guid, RequestForQuotation> _items = new();

    public Task<RequestForQuotation?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<RequestForQuotation>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RequestForQuotation>>(
            _items.Values.OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);

    public void Add(RequestForQuotation rfq) => _items[rfq.Id] = rfq;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
