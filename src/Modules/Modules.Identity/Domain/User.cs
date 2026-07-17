namespace Modules.Identity.Domain;

/// <summary>
/// A real, logged-in user — replaces the hardcoded actor literals (<c>"system/ui"</c>, <c>"system/approver"</c>)
/// every controller used before this module existed (see `MISSING-FEATURES-AUDIT.md` Part 1 §1). <see cref="Username"/>
/// is the same <c>actor: string</c> value every Application-layer service across every module already
/// accepts — no Application service changes when this module was added, only what *produces* that string
/// changes (a real authenticated identity instead of a controller constant).
///
/// Deliberately not a <see cref="Platform.Core.BusinessObject"/> — no Draft/Submit/Approve lifecycle,
/// same reasoning as <c>Modules.MasterData.Domain.LookupType</c>: real SAP/Dynamics user administration is
/// immediate-effect, gated by a security role, not a workflow.
/// </summary>
public sealed class User
{
    private readonly List<UserRole> _roles = new();

    public Guid Id { get; private set; }

    /// <summary>The stable login/actor identifier (e.g. <c>"ahmer.bilal"</c>) — never renamed once created,
    /// since it's what every audit entry, workflow decision, and document creator is attributed to.</summary>
    public string Username { get; private set; }

    public string? Email { get; private set; }

    public string DisplayName { get; private set; }

    /// <summary>PBKDF2 hash produced by <c>Microsoft.AspNetCore.Identity.PasswordHasher&lt;User&gt;</c> at
    /// the Application layer — the Domain layer never sees or handles a plaintext password.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>True when the user can log in. Deactivating (rather than deleting) a user preserves every
    /// audit entry/document they ever created as still meaningfully attributed — the same "correct by
    /// reversal, not by deletion" principle used everywhere else in this platform.</summary>
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public string CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public User(string createdBy, string username, string displayName, string passwordHash, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Id = Guid.NewGuid();
        Username = username;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Email = email;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — see <see cref="Platform.Core.BusinessObject"/>'s
    /// parameterless constructor for the same pattern. Never call from application code.</summary>
    private User()
    {
        Username = null!;
        DisplayName = null!;
        PasswordHash = null!;
        CreatedBy = null!;
    }

    public void SetPasswordHash(string actor, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProfile(string actor, string displayName, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        Email = email;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Activate(string actor)
    {
        IsActive = true;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(string actor)
    {
        IsActive = false;
        ModifiedBy = actor;
        ModifiedAt = DateTimeOffset.UtcNow;
    }

    public UserRole AddRole(string roleKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleKey);
        if (_roles.Any(r => r.RoleKey == roleKey))
            throw new ArgumentException($"'{Username}' already holds role '{roleKey}'.");

        var role = new UserRole(roleKey);
        _roles.Add(role);
        return role;
    }

    public void RemoveRole(string roleKey) => _roles.RemoveAll(r => r.RoleKey == roleKey);
}
