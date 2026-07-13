namespace Platform.Security.FieldLevel;

/// <summary>
/// Declares that a field is sensitive: seeing it unmasked requires holding <paramref name="UnmaskPrivilegeKey"/> —
/// unmasking is itself just another Privilege, resolved through the same Role/Duty mechanism as any
/// other permission (docs/architecture/03-platform-services.md #2.3).
/// </summary>
public sealed record FieldSecurityPolicy(string FieldKey, string UnmaskPrivilegeKey, Func<string, string> Mask);
