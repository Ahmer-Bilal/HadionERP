namespace Platform.Configuration;

/// <summary>The scope a configuration value is being resolved for — as many of these as are known at the
/// call site; resolution walks from the most specific level that has a value down to System.</summary>
public sealed record ConfigurationContext(string? TenantId = null, string? CompanyId = null, string? BranchId = null, string? UserId = null)
{
    public static readonly ConfigurationContext SystemOnly = new();
}
