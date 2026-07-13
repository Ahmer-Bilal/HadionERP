using Platform.Security.FieldLevel;

namespace Platform.Security.Tests;

public class FieldSecurityServiceTests
{
    private const string UnmaskSalary = "HR.Employee.Salary.Unmask";

    private static FieldSecurityService BuildService()
    {
        var unmaskDuty = new Duty("ViewSalaries", "View unmasked salary figures", new[] { PrivilegeGrant.Unconditional(UnmaskSalary) });
        var role = new Role("PayrollOfficer", "Payroll officer", new[] { unmaskDuty.Key });
        var catalog = new InMemorySecurityCatalog(new[] { role }, new[] { unmaskDuty });
        var authz = new AuthorizationService(catalog);

        var policy = new FieldSecurityPolicy("Employee.Salary", UnmaskSalary, MaskingStrategies.FullMask);
        return new FieldSecurityService(new[] { policy }, authz);
    }

    [Fact]
    public void Masks_salary_for_a_user_without_the_unmask_privilege()
    {
        var service = BuildService();
        var clerk = new SecurityPrincipal("u.clerk", Array.Empty<string>(), new Dictionary<string, IReadOnlySet<string>>());

        var result = service.Apply(clerk, "Employee.Salary", "12500.00");

        Assert.Equal(new string('*', "12500.00".Length), result);
    }

    [Fact]
    public void Shows_salary_unmasked_for_a_user_with_the_privilege()
    {
        var service = BuildService();
        var payrollOfficer = new SecurityPrincipal("u.payroll", new[] { "PayrollOfficer" }, new Dictionary<string, IReadOnlySet<string>>());

        var result = service.Apply(payrollOfficer, "Employee.Salary", "12500.00");

        Assert.Equal("12500.00", result);
    }

    [Fact]
    public void Fields_with_no_registered_policy_pass_through_unchanged()
    {
        var service = BuildService();
        var anyone = new SecurityPrincipal("u.anyone", Array.Empty<string>(), new Dictionary<string, IReadOnlySet<string>>());

        var result = service.Apply(anyone, "Employee.FirstName", "Ahmer");

        Assert.Equal("Ahmer", result);
    }

    [Theory]
    [InlineData("SA1234567890123456789012", "********************9012")]
    public void LastFourVisible_masks_all_but_the_last_four_characters(string input, string expected)
    {
        Assert.Equal(expected, MaskingStrategies.LastFourVisible(input));
    }
}
