using Modules.MasterData.Contracts;

namespace Modules.Construction.Tests;

/// <summary>In-memory stand-in for the Modules.MasterData.Contracts lookup SubcontractService depends on
/// to validate the subcontractor — same pattern as Modules.Procurement.Tests's own copy.</summary>
internal sealed class FakeBusinessPartnerLookup : IBusinessPartnerLookup
{
    private readonly Dictionary<Guid, BusinessPartnerSummary> _partners = new();

    public void Add(BusinessPartnerSummary partner) => _partners[partner.Id] = partner;

    public Task<BusinessPartnerSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_partners.GetValueOrDefault(id));
}
