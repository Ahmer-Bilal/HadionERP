using Modules.MasterData.Contracts;

namespace Modules.ProjectManagement.Tests;

internal sealed class FakeBusinessPartnerLookup : IBusinessPartnerLookup
{
    private readonly Dictionary<Guid, BusinessPartnerSummary> _partners = new();

    public void Add(BusinessPartnerSummary partner) => _partners[partner.Id] = partner;

    public Task<BusinessPartnerSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_partners.GetValueOrDefault(id));
}
