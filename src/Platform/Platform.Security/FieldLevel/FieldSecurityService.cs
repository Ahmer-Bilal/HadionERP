using Platform.Security;

namespace Platform.Security.FieldLevel;

public sealed class FieldSecurityService : IFieldSecurityService
{
    private readonly Dictionary<string, FieldSecurityPolicy> _policies;
    private readonly IAuthorizationService _authorizationService;

    public FieldSecurityService(IEnumerable<FieldSecurityPolicy> policies, IAuthorizationService authorizationService)
    {
        _policies = policies.ToDictionary(p => p.FieldKey);
        _authorizationService = authorizationService;
    }

    public string Apply(SecurityPrincipal principal, string fieldKey, string rawValue)
    {
        if (!_policies.TryGetValue(fieldKey, out var policy))
        {
            return rawValue;
        }

        var result = _authorizationService.Authorize(principal, policy.UnmaskPrivilegeKey);
        return result.Allowed ? rawValue : policy.Mask(rawValue);
    }
}
