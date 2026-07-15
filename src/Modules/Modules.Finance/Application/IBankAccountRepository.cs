using Modules.Finance.Domain;

namespace Modules.Finance.Application;

public interface IBankAccountRepository
{
    Task<BankAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BankAccount?> GetByCodeAsync(string accountCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankAccount>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(BankAccount bankAccount);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
