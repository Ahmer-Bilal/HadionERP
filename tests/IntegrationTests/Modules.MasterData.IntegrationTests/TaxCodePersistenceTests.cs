using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Modules.MasterData.Infrastructure;
using Platform.Core;
using Xunit;

namespace Modules.MasterData.IntegrationTests;

public class TaxCodePersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_tax_code_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var taxCode = new TaxCode("ahmer.bilal", "VAT15", "Standard VAT 15%", 15.00m, TaxType.Standard);
            taxCode.UpdateTaxCodeNameArabic("ضريبة القيمة المضافة 15%");
            taxCode.AssignNumber("MD-TAX-2026-000001");
            writeContext.TaxCodes.Add(taxCode);
            await writeContext.SaveChangesAsync();
            id = taxCode.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.TaxCodes.FirstOrDefaultAsync(t => t.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("VAT15", reloaded!.TaxCodeCode);
        Assert.Equal("Standard VAT 15%", reloaded.TaxCodeName);
        Assert.Equal("ضريبة القيمة المضافة 15%", reloaded.TaxCodeNameArabic);
        Assert.Equal(15.00m, reloaded.Rate);
        Assert.Equal(TaxType.Standard, reloaded.TaxType);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task Submit_and_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var taxCode = new TaxCode("ahmer.bilal", "ZERO", "Zero-Rated", 0m, TaxType.ZeroRated);
            taxCode.AssignNumber("MD-TAX-2026-000002");
            writeContext.TaxCodes.Add(taxCode);
            await writeContext.SaveChangesAsync();
            taxCode.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            taxCode.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = taxCode.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.TaxCodes.FirstOrDefaultAsync(t => t.Id == id);
        Assert.NotNull(reloaded);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded!.Status);
        Assert.Equal(2, reloaded.RowVersion);
    }

    [Fact]
    public async Task Tax_code_uniqueness_is_enforced_at_the_database_level()
    {
        await using var context = TestDatabase.CreateContext();
        var first = new TaxCode("ahmer.bilal", "DUPTEST", "First", 15m, TaxType.Standard);
        context.TaxCodes.Add(first);
        await context.SaveChangesAsync();

        var second = new TaxCode("ahmer.bilal", "DUPTEST", "Second", 15m, TaxType.Standard);
        context.TaxCodes.Add(second);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
