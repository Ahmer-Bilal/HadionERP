namespace Modules.Identity.Application;

public sealed record UserDto(
    Guid Id,
    string Username,
    string? Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> RoleKeys,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateUserRequest(string Username, string DisplayName, string Password, string? Email = null);

public sealed record UpdateUserProfileRequest(string DisplayName, string? Email);

public sealed record ResetPasswordRequest(string NewPassword);

/// <summary><paramref name="OverrideReason"/> is required only when the proposed assignment has an
/// unresolved Segregation of Duties conflict — supplying it grants an explicit, logged exception
/// (<c>Platform.Security.Sod.ISodExceptionLog</c>) instead of silently blocking or silently allowing.</summary>
public sealed record AssignRoleRequest(string RoleKey, string? OverrideReason = null);

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
