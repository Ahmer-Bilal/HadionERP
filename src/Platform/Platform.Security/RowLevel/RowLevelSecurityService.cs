using Platform.Security;

namespace Platform.Security.RowLevel;

public sealed class RowLevelSecurityService : IRowLevelSecurityService
{
    public bool CanAccess(SecurityPrincipal principal, ResourceScope resourceScope)
    {
        foreach (var (dimension, value) in resourceScope.Attributes)
        {
            if (principal.ScopeAttributes.TryGetValue(dimension, out var allowedValues) && !allowedValues.Contains(value))
            {
                return false;
            }
        }

        return true;
    }
}
