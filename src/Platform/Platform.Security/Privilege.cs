namespace Platform.Security;

/// <summary>
/// The smallest permission unit in the platform — e.g. "Procurement.PurchaseOrder.Approve", following
/// the naming convention "{Module}.{BO}.{Action}" in docs/architecture/05-engineering-standards.md #2.
/// Privileges are declared once by the module that owns the action they gate; they are bundled into
/// Duties, which are bundled into Roles (docs/architecture/03-platform-services.md #2.2) — an
/// individual user is never assigned a bare Privilege directly.
/// </summary>
public sealed record Privilege(string Key, string Description);
