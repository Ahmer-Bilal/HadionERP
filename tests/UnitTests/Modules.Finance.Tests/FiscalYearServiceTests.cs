using Modules.Finance.Application;
using Platform.Audit;
using Platform.Security;

namespace Modules.Finance.Tests;

public class FiscalYearServiceTests
{
    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["finance.manager"] = new[] { FiscalYearSecurity.AdministratorRoleKey },
            ["ahmer.bilal"] = Array.Empty<string>(),
        });

    private static FiscalYearService BuildService(out FakeFiscalYearRepository repository)
    {
        repository = new FakeFiscalYearRepository();
        var auditLog = new InMemoryAuditLog();
        var securityCatalog = new InMemorySecurityCatalog(
            new[] { FiscalYearSecurity.AdministratorRole },
            new[] { FiscalYearSecurity.AdministratorDuty });

        return new FiscalYearService(repository, new AuditRecorder(auditLog), new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    [Fact]
    public async Task Create_generates_12_periods()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager");

        Assert.Equal(2026, created.Year);
        Assert.Equal(12, created.Periods.Count);
    }

    [Fact]
    public async Task Create_rejects_a_non_administrator()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(new CreateFiscalYearRequest(2026), "ahmer.bilal"));
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_year()
    {
        var service = BuildService(out _);
        await service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager"));
    }

    [Fact]
    public async Task ClosePeriod_then_ReopenPeriod_round_trips_IsOpen()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager");

        var closed = await service.ClosePeriodAsync(created.Id, 5, "finance.manager");
        Assert.False(closed.Periods.Single(p => p.PeriodNumber == 5).IsOpen);

        var reopened = await service.ReopenPeriodAsync(created.Id, 5, "finance.manager");
        Assert.True(reopened.Periods.Single(p => p.PeriodNumber == 5).IsOpen);
    }

    [Fact]
    public async Task ClosePeriod_rejects_an_unknown_period_number()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager");

        await Assert.ThrowsAsync<ArgumentException>(() => service.ClosePeriodAsync(created.Id, 13, "finance.manager"));
    }

    [Fact]
    public async Task SetTargetCloseDate_updates_the_periods_own_date()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(new CreateFiscalYearRequest(2026), "finance.manager");
        var newDate = new DateOnly(2026, 6, 10);

        var updated = await service.SetTargetCloseDateAsync(created.Id, 5, newDate, "finance.manager");

        Assert.Equal(newDate, updated.Periods.Single(p => p.PeriodNumber == 5).TargetCloseDate);
    }
}
