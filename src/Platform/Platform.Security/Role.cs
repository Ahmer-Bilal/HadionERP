namespace Platform.Security;

/// <summary>
/// A bundle of Duties assigned to a user (docs/architecture/04-platform-services.md #2.2). This is the
/// only thing ever assigned to a <see cref="SecurityPrincipal"/> — never a bare Privilege or Duty
/// directly, so that day-to-day access management stays at the "what job do they do" level.
/// </summary>
public sealed record Role(string Key, string Description, IReadOnlyCollection<string> DutyKeys);
