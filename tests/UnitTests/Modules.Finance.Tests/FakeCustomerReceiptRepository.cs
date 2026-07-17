using Modules.Finance.Application;
using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

internal sealed class FakeCustomerReceiptRepository : ICustomerReceiptRepository
{
    private readonly Dictionary<Guid, CustomerReceipt> _receipts = new();

    public Task<CustomerReceipt?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_receipts.GetValueOrDefault(id));

    public Task<IReadOnlyList<CustomerReceipt>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerReceipt>>(
            _receipts.Values.OrderByDescending(r => r.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_receipts.Count);

    public Task<IReadOnlyList<CustomerReceipt>> ListByInvoiceAsync(Guid arInvoiceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CustomerReceipt>>(
            _receipts.Values.Where(r => r.Allocations.Any(a => a.ARInvoiceId == arInvoiceId)).ToList());

    public void Add(CustomerReceipt receipt) => _receipts[receipt.Id] = receipt;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
