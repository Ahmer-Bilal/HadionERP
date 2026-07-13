using Platform.Security;

namespace Platform.Security.RowLevel;

public interface IRowLevelSecurityService
{
    /// <summary>
    /// True if <paramref name="principal"/> may access a record with the given scope. For each
    /// dimension present on the resource (e.g. "CompanyId"), the principal must either be unrestricted
    /// on that dimension (no entry in their ScopeAttributes — e.g. a platform admin) or have the
    /// resource's value in their allowed set — see the convention documented on
    /// <see cref="SecurityPrincipal"/>.
    /// </summary>
    bool CanAccess(SecurityPrincipal principal, ResourceScope resourceScope);
}
