namespace Modules.Identity.Domain;

/// <summary>
/// One Role key assigned to a <see cref="User"/> — a child entity, not an independent Business Object, same
/// "0..n child collection, only exists through its parent" pattern used throughout this platform (e.g.
/// <c>Modules.MasterData.Domain.BusinessRole</c> on <c>BusinessPartner</c>). <see cref="RoleKey"/> matches a
/// <c>Platform.Security.Role.Key</c> registered in the shared <c>ISecurityCatalog</c> — this module doesn't
/// define what a Role grants, only which ones a user currently holds. Constructed only through
/// <see cref="User.AddRole"/>.
/// </summary>
public sealed class UserRole
{
    public Guid Id { get; private set; }

    public string RoleKey { get; private set; }

    internal UserRole(string roleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleKey);
        Id = Guid.NewGuid();
        RoleKey = roleKey;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private UserRole()
    {
        RoleKey = null!;
    }
}
