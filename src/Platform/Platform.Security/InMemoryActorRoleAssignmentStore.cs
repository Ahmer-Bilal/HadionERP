namespace Platform.Security;

/// <summary>Reference implementation of <see cref="IActorRoleAssignmentStore"/> — a fixed lookup table
/// seeded at startup, the same "prove the mechanism, swap for real later" pattern as
/// <see cref="InMemorySecurityCatalog"/>. An actor with no entry resolves to no roles (denied by default,
/// not granted by default).</summary>
public sealed class InMemoryActorRoleAssignmentStore : IActorRoleAssignmentStore
{
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _assignments;

    public InMemoryActorRoleAssignmentStore(IReadOnlyDictionary<string, IReadOnlyCollection<string>> assignments)
    {
        _assignments = assignments;
    }

    public IReadOnlyCollection<string> ResolveRoleKeys(string actorId) =>
        _assignments.TryGetValue(actorId, out var roles) ? roles : Array.Empty<string>();
}
