using Platform.Security;
using Platform.Security.Sod;

namespace Modules.Procurement.Application;

/// <summary>
/// The Privileges/Duties/Roles/SoD conflict rules Vendor Prequalification registers into
/// Platform.Security at startup — same module-owned security configuration pattern as every
/// Modules.MasterData/Modules.Finance slice. Unlike every prior Business Object (one Maintainer + one
/// Approver duty), this one has five distinct reviewer duties, one per
/// <see cref="VendorPrequalificationWorkflow"/> step — Commercial/Legal/Technical/HSE/Quality are separate
/// real-world departments, each with its own reviewer, not one approver wearing five hats.
/// </summary>
public static class VendorPrequalificationSecurity
{
    public const string MaintainPrivilegeKey = "Procurement.VendorPrequalification.Maintain";

    /// <summary>Shared by all five reviewer duties below — a role holder can decide whichever step's
    /// <c>RequiredRoleKey</c> they actually hold; <see cref="Platform.Workflow.WorkflowEngine"/> itself is
    /// what enforces which specific step a given role may act on (see its eligibility check), so this
    /// coarse privilege only gates "can attempt to decide a review step at all."</summary>
    public const string ReviewPrivilegeKey = "Procurement.VendorPrequalification.Review";

    public const string MaintainerDutyKey = "Procurement.VendorPrequalification.Maintainer";
    public const string MaintainerRoleKey = "Procurement.VendorPrequalification.Maintainer";

    public static readonly Duty MaintainerDuty = new(
        MaintainerDutyKey,
        "Create and maintain Vendor Prequalifications (create, submit for review)",
        new[] { PrivilegeGrant.Unconditional(MaintainPrivilegeKey) });

    public static readonly Role MaintainerRole = new(
        MaintainerRoleKey, "Vendor Prequalification maintainer", new[] { MaintainerDutyKey });

    public static readonly Duty CommercialReviewerDuty = new(
        "Procurement.VendorPrequalification.CommercialReviewer",
        "Review the Commercial domain of a Vendor Prequalification",
        new[] { PrivilegeGrant.Unconditional(ReviewPrivilegeKey) });

    public static readonly Duty LegalReviewerDuty = new(
        "Procurement.VendorPrequalification.LegalReviewer",
        "Review the Legal domain of a Vendor Prequalification",
        new[] { PrivilegeGrant.Unconditional(ReviewPrivilegeKey) });

    public static readonly Duty TechnicalReviewerDuty = new(
        "Procurement.VendorPrequalification.TechnicalReviewer",
        "Review the Technical domain of a Vendor Prequalification",
        new[] { PrivilegeGrant.Unconditional(ReviewPrivilegeKey) });

    public static readonly Duty HseReviewerDuty = new(
        "Procurement.VendorPrequalification.HseReviewer",
        "Review the HSE domain of a Vendor Prequalification",
        new[] { PrivilegeGrant.Unconditional(ReviewPrivilegeKey) });

    public static readonly Duty QualityReviewerDuty = new(
        "Procurement.VendorPrequalification.QualityReviewer",
        "Review the Quality domain of a Vendor Prequalification",
        new[] { PrivilegeGrant.Unconditional(ReviewPrivilegeKey) });

    public static readonly Role CommercialReviewerRole = new(
        VendorPrequalificationWorkflow.CommercialReviewerRoleKey, "Vendor Prequalification commercial reviewer",
        new[] { CommercialReviewerDuty.Key });

    public static readonly Role LegalReviewerRole = new(
        VendorPrequalificationWorkflow.LegalReviewerRoleKey, "Vendor Prequalification legal reviewer",
        new[] { LegalReviewerDuty.Key });

    public static readonly Role TechnicalReviewerRole = new(
        VendorPrequalificationWorkflow.TechnicalReviewerRoleKey, "Vendor Prequalification technical reviewer",
        new[] { TechnicalReviewerDuty.Key });

    public static readonly Role HseReviewerRole = new(
        VendorPrequalificationWorkflow.HseReviewerRoleKey, "Vendor Prequalification HSE reviewer",
        new[] { HseReviewerDuty.Key });

    public static readonly Role QualityReviewerRole = new(
        VendorPrequalificationWorkflow.QualityReviewerRoleKey, "Vendor Prequalification quality reviewer",
        new[] { QualityReviewerDuty.Key });

    /// <summary>The same person should never both submit a vendor's prequalification and review one of its
    /// domains — one conflict rule per reviewer duty, same reasoning as every other module's Maintainer-vs-
    /// Approver conflict (docs/architecture/04-platform-services.md #2.2), just five times over since there
    /// are five distinct review duties here instead of one.</summary>
    public static readonly SodConflictRule MaintainerCommercialReviewerConflict = new(
        MaintainerDutyKey, CommercialReviewerDuty.Key,
        "The same person should not both submit and commercially review a Vendor Prequalification.");

    public static readonly SodConflictRule MaintainerLegalReviewerConflict = new(
        MaintainerDutyKey, LegalReviewerDuty.Key,
        "The same person should not both submit and legally review a Vendor Prequalification.");

    public static readonly SodConflictRule MaintainerTechnicalReviewerConflict = new(
        MaintainerDutyKey, TechnicalReviewerDuty.Key,
        "The same person should not both submit and technically review a Vendor Prequalification.");

    public static readonly SodConflictRule MaintainerHseReviewerConflict = new(
        MaintainerDutyKey, HseReviewerDuty.Key,
        "The same person should not both submit and HSE-review a Vendor Prequalification.");

    public static readonly SodConflictRule MaintainerQualityReviewerConflict = new(
        MaintainerDutyKey, QualityReviewerDuty.Key,
        "The same person should not both submit and quality-review a Vendor Prequalification.");
}
