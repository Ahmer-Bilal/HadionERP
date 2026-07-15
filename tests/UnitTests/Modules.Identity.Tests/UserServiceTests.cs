using Modules.Identity.Application;
using Platform.Audit;
using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Identity.Tests;

public class UserServiceTests
{
    private const string Administrator = "admin";
    private const string NoPrivilegeActor = "random.user";

    private const string RoleA = "RoleA";
    private const string RoleB = "RoleB";
    private const string DutyA = "DutyA";
    private const string DutyB = "DutyB";

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            [Administrator] = new[] { IdentitySecurity.AdministratorRoleKey },
        });

    private static UserService BuildService(out ISodExceptionLog sodExceptionLog)
    {
        var securityCatalog = new InMemorySecurityCatalog(
            new[]
            {
                IdentitySecurity.AdministratorRole,
                new Role(RoleA, "Role A", new[] { DutyA }),
                new Role(RoleB, "Role B", new[] { DutyB }),
            },
            new[]
            {
                IdentitySecurity.AdministratorDuty,
                new Duty(DutyA, "Duty A", new[] { PrivilegeGrant.Unconditional("DutyA.Grant") }),
                new Duty(DutyB, "Duty B", new[] { PrivilegeGrant.Unconditional("DutyB.Grant") }),
            });

        sodExceptionLog = new InMemorySodExceptionLog();
        var sodEngine = new SodEngine(
            new[] { new SodConflictRule(DutyA, DutyB, "A and B cannot both be held by the same user.") },
            sodExceptionLog);

        return new UserService(
            new FakeUserRepository(),
            new AuditRecorder(new InMemoryAuditLog()),
            new AuthorizationService(securityCatalog),
            BuildActorRoles(),
            securityCatalog,
            sodEngine,
            sodExceptionLog);
    }

    private static UserService BuildService() => BuildService(out _);

    [Fact]
    public async Task CreateAsync_creates_a_user_with_a_hashed_password()
    {
        var service = BuildService();

        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        Assert.Equal("ahmer.bilal", created.Username);
        Assert.True(created.IsActive);
        Assert.Empty(created.RoleKeys);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_username()
    {
        var service = BuildService();
        await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Someone Else", "AnotherPass123"), Administrator));
    }

    [Fact]
    public async Task CreateAsync_denies_an_actor_without_the_administer_privilege()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), NoPrivilegeActor));
    }

    [Fact]
    public async Task AuthenticateAsync_succeeds_with_the_correct_password()
    {
        var service = BuildService();
        await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        var authenticated = await service.AuthenticateAsync("ahmer.bilal", "P@ssw0rd123");

        Assert.NotNull(authenticated);
        Assert.Equal("ahmer.bilal", authenticated!.Username);
    }

    [Fact]
    public async Task AuthenticateAsync_fails_with_the_wrong_password()
    {
        var service = BuildService();
        await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        Assert.Null(await service.AuthenticateAsync("ahmer.bilal", "WrongPassword"));
    }

    [Fact]
    public async Task AuthenticateAsync_fails_for_an_unknown_username()
    {
        var service = BuildService();

        Assert.Null(await service.AuthenticateAsync("nobody", "AnyPassword123"));
    }

    [Fact]
    public async Task Deactivating_a_user_makes_authentication_fail()
    {
        var service = BuildService();
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        await service.SetActiveAsync(created.Id, isActive: false, Administrator);

        Assert.Null(await service.AuthenticateAsync("ahmer.bilal", "P@ssw0rd123"));
    }

    [Fact]
    public async Task AssignRoleAsync_succeeds_for_a_non_conflicting_role()
    {
        var service = BuildService();
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        var updated = await service.AssignRoleAsync(created.Id, new AssignRoleRequest(RoleA), Administrator);

        Assert.Contains(RoleA, updated.RoleKeys);
    }

    [Fact]
    public async Task AssignRoleAsync_is_blocked_by_an_unresolved_Segregation_of_Duties_conflict()
    {
        var service = BuildService();
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);
        await service.AssignRoleAsync(created.Id, new AssignRoleRequest(RoleA), Administrator);

        var ex = await Assert.ThrowsAsync<SodConflictException>(() =>
            service.AssignRoleAsync(created.Id, new AssignRoleRequest(RoleB), Administrator));

        Assert.Single(ex.Conflicts);
    }

    [Fact]
    public async Task AssignRoleAsync_succeeds_once_an_override_reason_grants_an_exception()
    {
        var service = BuildService(out var sodExceptionLog);
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);
        await service.AssignRoleAsync(created.Id, new AssignRoleRequest(RoleA), Administrator);

        var updated = await service.AssignRoleAsync(
            created.Id, new AssignRoleRequest(RoleB, OverrideReason: "Small team, accepted risk."), Administrator);

        Assert.Contains(RoleA, updated.RoleKeys);
        Assert.Contains(RoleB, updated.RoleKeys);
        Assert.NotEmpty(sodExceptionLog.History);
    }

    [Fact]
    public async Task RemoveRoleAsync_removes_a_previously_assigned_role()
    {
        var service = BuildService();
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);
        await service.AssignRoleAsync(created.Id, new AssignRoleRequest(RoleA), Administrator);

        var updated = await service.RemoveRoleAsync(created.Id, RoleA, Administrator);

        Assert.DoesNotContain(RoleA, updated.RoleKeys);
    }

    [Fact]
    public async Task ResetPasswordAsync_lets_the_user_authenticate_with_the_new_password()
    {
        var service = BuildService();
        var created = await service.CreateAsync(new CreateUserRequest("ahmer.bilal", "Ahmer Bilal", "P@ssw0rd123"), Administrator);

        await service.ResetPasswordAsync(created.Id, new ResetPasswordRequest("NewP@ssw0rd456"), Administrator);

        Assert.Null(await service.AuthenticateAsync("ahmer.bilal", "P@ssw0rd123"));
        Assert.NotNull(await service.AuthenticateAsync("ahmer.bilal", "NewP@ssw0rd456"));
    }

    [Fact]
    public async Task Acting_on_an_unknown_user_id_throws_KeyNotFoundException()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.SetActiveAsync(Guid.NewGuid(), isActive: false, Administrator));
    }
}
