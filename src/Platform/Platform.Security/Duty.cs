namespace Platform.Security;

/// <summary>
/// A job function — a bundle of Privilege grants (docs/architecture/03-platform-services.md #2.2).
/// Duties are the unit Segregation of Duties conflict rules are defined against (see Sod/SodConflictRule.cs) —
/// SoD is meaningless at the raw-Privilege level (a single approval action isn't a conflict by itself);
/// it becomes meaningful at the Duty level ("Create Vendor" vs. "Approve Vendor Payment").
/// </summary>
public sealed record Duty(string Key, string Description, IReadOnlyCollection<PrivilegeGrant> Grants);
