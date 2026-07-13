using Modules.MasterData.Application;
using Modules.MasterData.Domain;

namespace Modules.MasterData.Tests;

/// <summary>An in-memory stand-in for <see cref="IBusinessPartnerRepository"/> — proves the Application
/// layer's own logic (validation, orchestration) without needing a real database, the same way the
/// platform kernel's own unit tests use in-memory reference implementations.</summary>
internal sealed class FakeBusinessPartnerRepository : IBusinessPartnerRepository
{
    private readonly Dictionary<Guid, BusinessPartner> _partners = new();

    public Task<BusinessPartner?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_partners.GetValueOrDefault(id));

    public Task<IReadOnlyList<BusinessPartner>> ListAsync(int skip, int top, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessPartner>>(
            _partners.Values.OrderBy(p => p.CreatedAt).Skip(skip).Take(top).ToList());

    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_partners.Count);

    public void Add(BusinessPartner partner) => _partners[partner.Id] = partner;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
