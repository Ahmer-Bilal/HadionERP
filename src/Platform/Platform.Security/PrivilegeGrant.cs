namespace Platform.Security;

/// <summary>
/// One Privilege as granted by a specific Duty, optionally narrowed by attribute constraints — this is
/// the ABAC half of the "hybrid RBAC + ABAC" model in docs/architecture/03-platform-services.md #2.2.
/// Example: the Duty "Approve Small Purchase Orders" grants "Procurement.PurchaseOrder.Approve" with
/// constraint MaxAmount=50000, while "Approve Large Purchase Orders" grants the same Privilege with no
/// constraint (unlimited). A user holding either Duty can approve; which amounts they can approve
/// depends on which grant(s) they hold — see <see cref="AuthorizationService"/>.
/// </summary>
public sealed record PrivilegeGrant(string PrivilegeKey, IReadOnlyDictionary<string, string>? Constraints = null)
{
    public static PrivilegeGrant Unconditional(string privilegeKey) => new(privilegeKey, null);

    public static PrivilegeGrant WithMaxAmount(string privilegeKey, decimal maxAmount) =>
        new(privilegeKey, new Dictionary<string, string> { ["MaxAmount"] = maxAmount.ToString("F2") });
}
