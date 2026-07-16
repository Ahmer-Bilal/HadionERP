using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface IContractRepository
{
    Task<Contract?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Contract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Contract contract);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
