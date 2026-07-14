using Modules.Procurement.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Procurement.IntegrationTests;

public class VendorPrequalificationPersistenceTests : IAsyncLifetime
{
    private static readonly Guid VendorId = Guid.NewGuid();

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_prequalification_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Subcontractor", "Electrical");
            prequalification.AssignNumber("PROC-VPQ-2026-000001");
            writeContext.VendorPrequalifications.Add(prequalification);
            await writeContext.SaveChangesAsync();
            id = prequalification.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.VendorPrequalifications.FindAsync(id);

        Assert.NotNull(reloaded);
        Assert.Equal("PROC-VPQ-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(VendorId, reloaded.BusinessPartnerId);
        Assert.Equal("Subcontractor", reloaded.RoleType);
        Assert.Equal("Electrical", reloaded.Trade);
        Assert.Equal(BusinessObjectStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task Submit_approve_persist_the_new_status_and_validity_period()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var prequalification = new VendorPrequalification("ahmer.bilal", VendorId, "Supplier");
            prequalification.AssignNumber("PROC-VPQ-2026-000002");
            writeContext.VendorPrequalifications.Add(prequalification);
            await writeContext.SaveChangesAsync();
            prequalification.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            prequalification.Approve("procurement.reviewer");
            prequalification.SetValidityPeriod(new DateOnly(2026, 7, 14), 24);
            await writeContext.SaveChangesAsync();
            id = prequalification.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.VendorPrequalifications.FindAsync(id);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded!.Status);
        Assert.Equal(new DateOnly(2026, 7, 14), reloaded.ValidFrom);
        Assert.Equal(new DateOnly(2028, 7, 14), reloaded.ValidUntil);
        Assert.Equal(2, reloaded.RowVersion); // Submit, Approve = two transitions
    }
}
