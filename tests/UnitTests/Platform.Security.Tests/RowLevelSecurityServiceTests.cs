using Platform.Security.RowLevel;

namespace Platform.Security.Tests;

public class RowLevelSecurityServiceTests
{
    private readonly RowLevelSecurityService _service = new();

    [Fact]
    public void Principal_scoped_to_a_company_can_access_records_in_that_company()
    {
        var principal = new SecurityPrincipal("u.branch-clerk", Array.Empty<string>(),
            new Dictionary<string, IReadOnlySet<string>> { ["CompanyId"] = new HashSet<string> { "C001" } });

        var canAccessOwnCompany = _service.CanAccess(principal, ResourceScope.Of(("CompanyId", "C001")));
        var canAccessOtherCompany = _service.CanAccess(principal, ResourceScope.Of(("CompanyId", "C002")));

        Assert.True(canAccessOwnCompany);
        Assert.False(canAccessOtherCompany);
    }

    [Fact]
    public void Principal_with_no_scope_attribute_for_a_dimension_is_unrestricted_on_it()
    {
        var admin = new SecurityPrincipal("u.admin", Array.Empty<string>(),
            new Dictionary<string, IReadOnlySet<string>>());

        Assert.True(_service.CanAccess(admin, ResourceScope.Of(("CompanyId", "C001"))));
        Assert.True(_service.CanAccess(admin, ResourceScope.Of(("CompanyId", "C999"))));
    }

    [Fact]
    public void All_scoped_dimensions_must_pass()
    {
        var principal = new SecurityPrincipal("u.project-scoped", Array.Empty<string>(),
            new Dictionary<string, IReadOnlySet<string>>
            {
                ["CompanyId"] = new HashSet<string> { "C001" },
                ["ProjectId"] = new HashSet<string> { "P100" }
            });

        var matchingBoth = ResourceScope.Of(("CompanyId", "C001"), ("ProjectId", "P100"));
        var wrongProject = ResourceScope.Of(("CompanyId", "C001"), ("ProjectId", "P999"));

        Assert.True(_service.CanAccess(principal, matchingBoth));
        Assert.False(_service.CanAccess(principal, wrongProject));
    }
}
