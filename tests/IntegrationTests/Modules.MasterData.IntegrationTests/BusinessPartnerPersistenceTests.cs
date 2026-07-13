using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Modules.MasterData.Infrastructure;
using Platform.Core;

namespace Modules.MasterData.IntegrationTests;

/// <summary>
/// Proves EF Core actually persists a Business Partner to the real database and reads it back
/// correctly through a BRAND NEW DbContext instance — simulating an application restart, which is
/// exactly the scenario that would expose a mapping mistake (e.g. a private setter EF can't reach) that
/// an in-memory-only test could never catch.
/// </summary>
public class BusinessPartnerPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_partner_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var partner = new BusinessPartner("ahmer.bilal", "Gulf Falcon Trading Co", PartnerType.Vendor);
            partner.UpdateTaxRegistrationNumber("300000000000003");
            partner.UpdateContactDetails("info@gulffalcon.example", "+966500000000", "Saudi Arabia", "Riyadh", "King Fahd Road");
            partner.AssignNumber("MD-BP-2026-000001");
            partner.ExtensionFields.Set("preferredLanguage", "ar");

            writeContext.BusinessPartners.Add(partner);
            await writeContext.SaveChangesAsync();
            id = partner.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.BusinessPartners.FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("Gulf Falcon Trading Co", reloaded!.Name);
        Assert.Equal(PartnerType.Vendor, reloaded.PartnerType);
        Assert.Equal("300000000000003", reloaded.TaxRegistrationNumber);
        Assert.Equal("Riyadh", reloaded.City);
        Assert.Equal("MD-BP-2026-000001", reloaded.DocumentNumber);
        Assert.Equal(BusinessObjectStatus.Draft, reloaded.Status);
        Assert.Equal("ahmer.bilal", reloaded.CreatedBy);
        Assert.Equal("ar", reloaded.ExtensionFields.Get<string>("preferredLanguage"));
    }

    [Fact]
    public async Task Submitting_and_approving_persists_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var partner = new BusinessPartner("ahmer.bilal", "Approve Me Co", PartnerType.Customer);
            writeContext.BusinessPartners.Add(partner);
            await writeContext.SaveChangesAsync();
            id = partner.Id;
        }

        await using (var updateContext = TestDatabase.CreateContext())
        {
            var partner = await updateContext.BusinessPartners.FirstAsync(p => p.Id == id);
            partner.Submit("ahmer.bilal");
            partner.Approve("finance.manager");
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.BusinessPartners.FirstAsync(p => p.Id == id);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(2, reloaded.RowVersion); // two transitions = two RowVersion increments, persisted
        Assert.Equal("finance.manager", reloaded.ModifiedBy);
    }

    [Fact]
    public async Task A_stale_concurrent_update_is_rejected_by_the_row_version_token()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var partner = new BusinessPartner("ahmer.bilal", "Concurrency Test Co", PartnerType.Vendor);
            writeContext.BusinessPartners.Add(partner);
            await writeContext.SaveChangesAsync();
            id = partner.Id;
        }

        // Two different "users" load the same record independently...
        await using var firstContext = TestDatabase.CreateContext();
        await using var secondContext = TestDatabase.CreateContext();
        var firstView = await firstContext.BusinessPartners.FirstAsync(p => p.Id == id);
        var secondView = await secondContext.BusinessPartners.FirstAsync(p => p.Id == id);

        // ...the first one saves a change...
        firstView.UpdateContactDetails("first@change.example", null, null, null, null);
        await firstContext.SaveChangesAsync();

        // ...the second, now working from a stale row_version, must be rejected rather than silently
        // overwrite the first change.
        secondView.UpdateContactDetails("second@change.example", null, null, null, null);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }
}
