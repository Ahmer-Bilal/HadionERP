using Modules.MasterData.Infrastructure;
using Platform.Core.NumberRanges;

namespace Modules.MasterData.IntegrationTests;

/// <summary>Proves the Postgres-backed number range survives what the in-memory kernel version can't: a
/// fresh DbContext (simulating an application restart) continuing the sequence rather than restarting it
/// from 1 — which would otherwise produce duplicate document numbers.</summary>
public class EfCoreNumberRangeServiceTests : IAsyncLifetime
{
    private const string TestCompanyId = "IT-C001";
    private static readonly NumberRangeDefinition Definition = new("MD-BP", "MD", "BP");

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Sequence_continues_correctly_across_a_fresh_DbContext_simulating_a_restart()
    {
        await using (var firstContext = TestDatabase.CreateContext())
        {
            var service = new EfCoreNumberRangeService(firstContext, new[] { Definition });
            var first = service.GetNext("MD-BP", TestCompanyId, 2026);
            Assert.Equal("MD-BP-2026-000001", first);
        }

        // A brand new DbContext — nothing in memory carries over except what's in the database.
        await using var secondContext = TestDatabase.CreateContext();
        var secondService = new EfCoreNumberRangeService(secondContext, new[] { Definition });
        var second = secondService.GetNext("MD-BP", TestCompanyId, 2026);

        Assert.Equal("MD-BP-2026-000002", second); // continues from 1, doesn't restart
    }

    [Fact]
    public async Task Different_companies_get_independent_sequences()
    {
        await using var context = TestDatabase.CreateContext();
        var service = new EfCoreNumberRangeService(context, new[] { Definition });

        var companyA = service.GetNext("MD-BP", "IT-C001", 2026);
        var companyB = service.GetNext("MD-BP", "IT-C002", 2026);

        Assert.Equal("MD-BP-2026-000001", companyA);
        Assert.Equal("MD-BP-2026-000001", companyB);
    }
}
