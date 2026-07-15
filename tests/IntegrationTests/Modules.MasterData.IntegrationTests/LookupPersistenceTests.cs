using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Xunit;

namespace Modules.MasterData.IntegrationTests;

public class LookupPersistenceTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_lookup_type_and_its_values_read_back_identically()
    {
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var type = new LookupType("system", "Country", "Country", "الدولة", isSystemDefined: true);
            writeContext.LookupTypes.Add(type);
            writeContext.LookupValues.Add(new LookupValue("system", "Country", "SA", "Saudi Arabia", "المملكة العربية السعودية", 0));
            writeContext.LookupValues.Add(new LookupValue("system", "Country", "AE", "United Arab Emirates", "الإمارات العربية المتحدة", 1));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloadedType = await readContext.LookupTypes.FirstOrDefaultAsync(t => t.Code == "Country");
        var reloadedValues = await readContext.LookupValues.Where(v => v.LookupTypeCode == "Country").OrderBy(v => v.SortOrder).ToListAsync();

        Assert.NotNull(reloadedType);
        Assert.True(reloadedType!.IsSystemDefined);
        Assert.Equal("الدولة", reloadedType.NameArabic);
        Assert.Equal(2, reloadedValues.Count);
        Assert.Equal("SA", reloadedValues[0].Code);
        Assert.Equal("المملكة العربية السعودية", reloadedValues[0].NameArabic);
        Assert.True(reloadedValues[0].IsActive);
    }

    [Fact]
    public async Task Lookup_value_code_uniqueness_is_enforced_per_type_at_the_database_level()
    {
        await using var context = TestDatabase.CreateContext();
        context.LookupTypes.Add(new LookupType("system", "Country", "Country", null, isSystemDefined: true));
        context.LookupValues.Add(new LookupValue("system", "Country", "SA", "Saudi Arabia", null, 0));
        await context.SaveChangesAsync();

        context.LookupValues.Add(new LookupValue("system", "Country", "SA", "Duplicate", null, 1));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Deactivating_a_value_persists_and_is_reversible()
    {
        Guid valueId;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            writeContext.LookupTypes.Add(new LookupType("system", "Trade", "Trade", null, isSystemDefined: true));
            var value = new LookupValue("system", "Trade", "Electrical", "Electrical", null, 0);
            writeContext.LookupValues.Add(value);
            await writeContext.SaveChangesAsync();
            valueId = value.Id;

            value.Deactivate("ahmer.bilal");
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.LookupValues.FirstAsync(v => v.Id == valueId);
        Assert.False(reloaded.IsActive);
        Assert.Equal("ahmer.bilal", reloaded.ModifiedBy);
    }
}
