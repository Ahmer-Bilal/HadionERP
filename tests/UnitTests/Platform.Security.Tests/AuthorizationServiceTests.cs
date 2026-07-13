using Platform.Security;

namespace Platform.Security.Tests;

public class AuthorizationServiceTests
{
    private const string ApprovePo = "Procurement.PurchaseOrder.Approve";

    private static (InMemorySecurityCatalog Catalog, IReadOnlyCollection<Duty> Duties) BuildCatalog()
    {
        var smallApprover = new Duty(
            "ApproveSmallPurchaseOrders",
            "Approve purchase orders up to 50,000 SAR",
            new[] { PrivilegeGrant.WithMaxAmount(ApprovePo, 50_000m) });

        var largeApprover = new Duty(
            "ApproveLargePurchaseOrders",
            "Approve purchase orders of any amount",
            new[] { PrivilegeGrant.Unconditional(ApprovePo) });

        var duties = new[] { smallApprover, largeApprover };

        var roles = new[]
        {
            new Role("JuniorFinanceApprover", "Junior finance approver", new[] { smallApprover.Key }),
            new Role("SeniorFinanceApprover", "Senior finance approver", new[] { largeApprover.Key })
        };

        return (new InMemorySecurityCatalog(roles, duties), duties);
    }

    [Fact]
    public void Denies_when_principal_holds_no_granting_duty()
    {
        var (catalog, _) = BuildCatalog();
        var authz = new AuthorizationService(catalog);

        var principal = new SecurityPrincipal("u.random", Array.Empty<string>(),
            new Dictionary<string, IReadOnlySet<string>>());

        var result = authz.Authorize(principal, ApprovePo);

        Assert.False(result.Allowed);
        Assert.Contains("no Duty granting", result.Reason);
    }

    [Fact]
    public void Junior_approver_allowed_under_threshold_denied_over_threshold()
    {
        var (catalog, _) = BuildCatalog();
        var authz = new AuthorizationService(catalog);

        var junior = new SecurityPrincipal("u.junior", new[] { "JuniorFinanceApprover" },
            new Dictionary<string, IReadOnlySet<string>>());

        var underLimit = authz.Authorize(junior, ApprovePo, new Dictionary<string, string> { ["Amount"] = "45000" });
        var overLimit = authz.Authorize(junior, ApprovePo, new Dictionary<string, string> { ["Amount"] = "60000" });

        Assert.True(underLimit.Allowed);
        Assert.False(overLimit.Allowed);
    }

    [Fact]
    public void Senior_approver_allowed_regardless_of_amount()
    {
        var (catalog, _) = BuildCatalog();
        var authz = new AuthorizationService(catalog);

        var senior = new SecurityPrincipal("u.senior", new[] { "SeniorFinanceApprover" },
            new Dictionary<string, IReadOnlySet<string>>());

        var result = authz.Authorize(senior, ApprovePo, new Dictionary<string, string> { ["Amount"] = "9999999" });

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Holding_both_duties_takes_the_union_the_larger_limit_wins()
    {
        var (catalog, _) = BuildCatalog();
        var authz = new AuthorizationService(catalog);

        var both = new SecurityPrincipal("u.both", new[] { "JuniorFinanceApprover", "SeniorFinanceApprover" },
            new Dictionary<string, IReadOnlySet<string>>());

        var result = authz.Authorize(both, ApprovePo, new Dictionary<string, string> { ["Amount"] = "9999999" });

        Assert.True(result.Allowed);
    }
}
