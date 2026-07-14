using Modules.MasterData.Contracts;

namespace Modules.Procurement.Tests;

/// <summary>In-memory stand-in for the Modules.MasterData.Contracts lookup Modules.Procurement depends on —
/// proves VendorPrequalificationService's own cross-module validation logic without a real MasterData
/// database. The real adapter (<c>Modules.MasterData.Infrastructure.EfBusinessPartnerLookup</c>) is proved
/// separately by MasterData's own tests and by this slice's integration test.</summary>
internal sealed class FakeBusinessPartnerLookup : IBusinessPartnerLookup
{
    private readonly Dictionary<Guid, BusinessPartnerSummary> _partners = new();

    public void Add(BusinessPartnerSummary partner) => _partners[partner.Id] = partner;

    public Task<BusinessPartnerSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_partners.GetValueOrDefault(id));
}
