using Microsoft.AspNetCore.Identity;
using Modules.Identity.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Identity.Application;

/// <summary>
/// Application service for real user administration — see <see cref="Modules.Identity.Domain.User"/>'s doc
/// comment for why this has no workflow/lifecycle. This is also the one place in the whole codebase that
/// finally calls <see cref="ISodEngine.FindUnresolvedConflicts"/> in a live request path (see
/// <see cref="AssignRoleAsync"/>) — every prior module registered real SoD conflict rules, but nothing ever
/// checked them, since there was no role-<em>assignment</em> action to guard (per
/// `MISSING-FEATURES-AUDIT.md` Part 1 §3).
/// </summary>
public sealed class UserService
{
    private const string AuditTargetType = "User";
    private const string AuditSource = "Modules.Identity";

    private readonly IUserRepository _repository;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly ISecurityCatalog _securityCatalog;
    private readonly ISodEngine _sodEngine;
    private readonly ISodExceptionLog _sodExceptionLog;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public UserService(
        IUserRepository repository,
        IAuditRecorder auditRecorder,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        ISecurityCatalog securityCatalog,
        ISodEngine sodEngine,
        ISodExceptionLog sodExceptionLog)
    {
        _repository = repository;
        _auditRecorder = auditRecorder;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _securityCatalog = securityCatalog;
        _sodEngine = sodEngine;
        _sodExceptionLog = sodExceptionLog;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);

        var existing = await _repository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Username '{request.Username}' is already in use.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var user = new User(actor, request.Username, request.DisplayName, "pending", request.Email);
        var hash = _passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(actor, hash);

        _repository.Add(user);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(user.Id), actor,
            $"User '{user.Username}' created.", AuditSource);

        return ToDto(user);
    }

    /// <summary>The login path — deliberately NOT gated by <see cref="RequireAuthorization"/> (there is no
    /// actor yet; this call is what produces one). Returns null on any failure (unknown username, wrong
    /// password, deactivated account) — never distinguishes which, so a login form can't be used to
    /// enumerate valid usernames.</summary>
    public async Task<UserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByUsernameAsync(username, cancellationToken);
        if (user is null || !user.IsActive) return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(user.Username, _passwordHasher.HashPassword(user, password));
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return ToDto(user);
    }

    public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetAsync(id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByUsernameAsync(username, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await _repository.ListAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<UserDto> UpdateProfileAsync(Guid id, UpdateUserProfileRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var user = await RequireUserAsync(id, cancellationToken);

        user.UpdateProfile(actor, request.DisplayName, request.Email);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(user.Id), actor,
            $"User '{user.Username}' profile updated.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(user);
    }

    public async Task<UserDto> ResetPasswordAsync(Guid id, ResetPasswordRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var user = await RequireUserAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        user.SetPasswordHash(actor, _passwordHasher.HashPassword(user, request.NewPassword));
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(user.Id), actor,
            $"Password reset for user '{user.Username}'.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(user);
    }

    public async Task<UserDto> SetActiveAsync(Guid id, bool isActive, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var user = await RequireUserAsync(id, cancellationToken);

        if (isActive) user.Activate(actor); else user.Deactivate(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(user.Id), actor,
            $"User '{user.Username}' {(isActive ? "activated" : "deactivated")}.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(user);
    }

    /// <summary>
    /// The real Segregation of Duties enforcement point — see this class's own doc comment. Resolves the
    /// user's role set as it would be *after* this assignment, translates it to Duty keys via the same
    /// <see cref="ISecurityCatalog.ResolveDutyKeys"/> every authorization check already uses, then checks
    /// for unresolved conflicts. A conflict throws <see cref="SodConflictException"/> unless
    /// <paramref name="request"/>.OverrideReason is supplied, in which case the conflict is logged as an
    /// explicit, permanent exception (<see cref="ISodExceptionLog.Grant"/>) before the assignment proceeds —
    /// never silently allowed, never silently blocked.
    /// </summary>
    public async Task<UserDto> AssignRoleAsync(Guid id, AssignRoleRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var user = await RequireUserAsync(id, cancellationToken);

        var proposedRoleKeys = user.Roles.Select(r => r.RoleKey).Append(request.RoleKey).Distinct().ToList();
        var dutyKeys = _securityCatalog.ResolveDutyKeys(proposedRoleKeys);
        var conflicts = _sodEngine.FindUnresolvedConflicts(user.Id.ToString(), dutyKeys);

        if (conflicts.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(request.OverrideReason))
                throw new SodConflictException(conflicts);

            foreach (var conflict in conflicts)
                _sodExceptionLog.Grant(user.Id.ToString(), conflict, actor, request.OverrideReason);
        }

        user.AddRole(request.RoleKey);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(user.Id), actor,
            $"Role '{request.RoleKey}' assigned to user '{user.Username}'.",
            new[] { new FieldValueChange("Roles", OldValueJson: null, NewValueJson: System.Text.Json.JsonSerializer.Serialize(request.RoleKey)) },
            AuditSource);

        return ToDto(user);
    }

    public async Task<UserDto> RemoveRoleAsync(Guid id, string roleKey, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var user = await RequireUserAsync(id, cancellationToken);

        user.RemoveRole(roleKey);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(user.Id), actor,
            $"Role '{roleKey}' removed from user '{user.Username}'.",
            new[] { new FieldValueChange("Roles", OldValueJson: System.Text.Json.JsonSerializer.Serialize(roleKey), NewValueJson: null) },
            AuditSource);

        return ToDto(user);
    }

    private void RequireAuthorization(string actor)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), IdentitySecurity.AdministerPrivilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private async Task<User> RequireUserAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User {id} was not found.");

    private static BusinessObjectReference AuditReference(Guid userId) => new(userId, AuditTargetType, "Self");

    private static UserDto ToDto(User u) => new(
        u.Id, u.Username, u.Email, u.DisplayName, u.IsActive,
        u.Roles.Select(r => r.RoleKey).ToList(), u.CreatedAt, u.CreatedBy);
}
