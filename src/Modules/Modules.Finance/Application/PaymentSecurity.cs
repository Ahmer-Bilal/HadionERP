using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Finance.Application;

/// <summary>Same module-owned Security configuration pattern as <see cref="APInvoiceSecurity"/> — a
/// deliberately separate Duty/Role pair, since raising a payment and approving/posting it (real money
/// leaving a real bank account) are different real-world authorities, arguably the single most classic
/// Segregation of Duties example in all of Finance.</summary>
public static class PaymentSecurity
{
    public const string MaintainPrivilegeKey = "Finance.Payment.Maintain";
    public const string ApprovePrivilegeKey = "Finance.Payment.Approve";

    public const string MaintainerDutyKey = "Finance.Payment.Maintainer";
    public const string ApproverDutyKey = "Finance.Payment.Approver";

    public const string MaintainerRoleKey = "Finance.Payment.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Payments (create, add/remove allocations, submit for approval)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Duty ApproverDuty = new(
        ApproverDutyKey,
        "Approve, post, or reverse a Payment",
        new[] { PrivilegeGrant.Unconditional(ApprovePrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Payment maintainer", new[] { MaintainerDutyKey });

    public static readonly Role ApproverRole = new(
        PaymentWorkflow.ApproverRoleKey, "Payment approver", new[] { ApproverDutyKey });

    public static readonly SodConflictRule MaintainerApproverConflict = new(
        MaintainerDutyKey,
        ApproverDutyKey,
        "The same person should not both create/maintain and approve/post a Payment " +
        "(Segregation of Duties, docs/architecture/03-platform-services.md #2.2) — the textbook " +
        "\"Create Vendor Invoice vs. Approve Vendor Payment\" example, this time on the payment side itself.");
}
