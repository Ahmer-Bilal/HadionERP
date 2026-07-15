using Modules.MasterData.Application;
using Platform.Audit;
using Platform.Security;

namespace Modules.MasterData.Tests;

public class LookupServiceTests
{
    private const string Administrator = "ahmer.bilal";
    private const string NoPrivilegeActor = "random.user";

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            [Administrator] = new[] { LookupSecurity.AdministratorRoleKey },
        });

    private static LookupService BuildService(out FakeLookupRepository repository)
    {
        repository = FakeLookupRepository.WithDefaults();
        var securityCatalog = new InMemorySecurityCatalog(
            new[] { LookupSecurity.AdministratorRole }, new[] { LookupSecurity.AdministratorDuty });
        return new LookupService(
            repository, new AuditRecorder(new InMemoryAuditLog()),
            new AuthorizationService(securityCatalog), BuildActorRoles());
    }

    [Fact]
    public async Task ListTypesAsync_reports_value_counts()
    {
        var service = BuildService(out _);

        var types = await service.ListTypesAsync();

        var country = Assert.Single(types, t => t.Code == "Country");
        Assert.True(country.ValueCount > 0);
        Assert.True(country.IsSystemDefined);
    }

    [Fact]
    public async Task CreateValueAsync_adds_a_new_value_to_an_existing_type()
    {
        var service = BuildService(out _);

        var created = await service.CreateValueAsync("Country", new CreateLookupValueRequest("XX", "Testland", "بلد الاختبار"), Administrator);

        Assert.Equal("XX", created.Code);
        var values = await service.ListValuesAsync("Country");
        Assert.Contains(values, v => v.Code == "XX");
    }

    [Fact]
    public async Task CreateValueAsync_rejects_a_duplicate_code_within_the_same_type()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateValueAsync("Country", new CreateLookupValueRequest("Saudi Arabia", "Duplicate", null), Administrator));
    }

    [Fact]
    public async Task CreateValueAsync_rejects_an_unknown_lookup_type()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateValueAsync("NotARealType", new CreateLookupValueRequest("X", "X", null), Administrator));
    }

    [Fact]
    public async Task CreateValueAsync_denies_an_actor_without_the_administer_privilege()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateValueAsync("Country", new CreateLookupValueRequest("XX", "Testland", null), NoPrivilegeActor));
    }

    [Fact]
    public async Task DeactivateValueAsync_hides_a_value_without_deleting_it()
    {
        var service = BuildService(out _);
        var created = await service.CreateValueAsync("Country", new CreateLookupValueRequest("XX", "Testland", null), Administrator);

        var deactivated = await service.SetActiveAsync("Country", created.Id, isActive: false, Administrator);

        Assert.False(deactivated.IsActive);
        var activeOnly = await service.ListValuesAsync("Country", includeInactive: false);
        Assert.DoesNotContain(activeOnly, v => v.Id == created.Id);
        var all = await service.ListValuesAsync("Country", includeInactive: true);
        Assert.Contains(all, v => v.Id == created.Id);
    }

    [Fact]
    public async Task DeleteValueAsync_removes_a_value_that_is_not_referenced_by_any_record()
    {
        var service = BuildService(out _);
        var created = await service.CreateValueAsync("Country", new CreateLookupValueRequest("XX", "Testland", null), Administrator);

        await service.DeleteValueAsync("Country", created.Id, Administrator);

        var values = await service.ListValuesAsync("Country");
        Assert.DoesNotContain(values, v => v.Id == created.Id);
    }

    [Fact]
    public async Task DeleteValueAsync_refuses_to_delete_a_value_referenced_by_existing_records()
    {
        var service = BuildService(out var repository);
        repository.MarkInUse("BusinessRoleType", "Supplier");
        var value = await service.ListValuesAsync("BusinessRoleType");
        var supplier = Assert.Single(value, v => v.Code == "Supplier");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteValueAsync("BusinessRoleType", supplier.Id, Administrator));
    }

    [Fact]
    public async Task CreateTypeAsync_lets_an_administrator_add_a_brand_new_custom_lookup_type()
    {
        var service = BuildService(out _);

        var created = await service.CreateTypeAsync(new CreateLookupTypeRequest("Incoterms", "Incoterms", null), Administrator);

        Assert.False(created.IsSystemDefined);
        var value = await service.CreateValueAsync("Incoterms", new CreateLookupValueRequest("FOB", "Free on Board", null), Administrator);
        Assert.Equal("FOB", value.Code);
    }

    [Fact]
    public async Task DeleteTypeAsync_refuses_to_delete_a_system_defined_type()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteTypeAsync("Country", Administrator));
    }

    [Fact]
    public async Task DeleteTypeAsync_refuses_to_delete_a_custom_type_that_still_has_values()
    {
        var service = BuildService(out _);
        await service.CreateTypeAsync(new CreateLookupTypeRequest("Incoterms", "Incoterms", null), Administrator);
        await service.CreateValueAsync("Incoterms", new CreateLookupValueRequest("FOB", "Free on Board", null), Administrator);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteTypeAsync("Incoterms", Administrator));
    }

    [Fact]
    public async Task DeleteTypeAsync_deletes_an_empty_custom_type()
    {
        var service = BuildService(out _);
        await service.CreateTypeAsync(new CreateLookupTypeRequest("Incoterms", "Incoterms", null), Administrator);

        await service.DeleteTypeAsync("Incoterms", Administrator);

        var types = await service.ListTypesAsync();
        Assert.DoesNotContain(types, t => t.Code == "Incoterms");
    }
}
