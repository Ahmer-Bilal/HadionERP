using Modules.Identity.Domain;

namespace Modules.Identity.Application;

/// <summary>The persistence port for Users — same dependency-inversion shape as every other module
/// repository (e.g. <c>Modules.MasterData.Application.ILookupRepository</c>).</summary>
public interface IUserRepository
{
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
