using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface ISubcontractRepository
{
    Task<Subcontract?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subcontract>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Subcontract subcontract);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
