namespace Modules.ProjectManagement.Contracts;

/// <summary>
/// The published, read-only view of one WBS element another module (Construction) may depend on when
/// validating a reference onto it (e.g. a BOQ line) — same Contracts-package rule as
/// <see cref="Modules.MasterData.Contracts.IBusinessPartnerLookup"/>. Deliberately not the full
/// <c>Modules.ProjectManagement.Domain.WbsElement</c> shape — a consumer needs to know which Controlling
/// flags are set, not this module's own maintenance concerns.
/// </summary>
public sealed record WbsElementSummary(
    Guid Id,
    string Code,
    string Name,
    string? NameArabic,
    bool IsPlanningElement,
    bool IsAccountAssignmentElement,
    bool IsBillingElement);

/// <summary>
/// The published, read-only view of a Project another module (Construction) may depend on — same
/// Contracts-package rule as <see cref="Modules.MasterData.Contracts.IBusinessPartnerLookup"/>. Construction
/// needs to know "is this project released, and what WBS elements does it have to map a BOQ line onto,"
/// not Project's own maintenance concerns (workflow state history, audit trail).
/// </summary>
public sealed record ProjectSummary(
    Guid Id,
    string? DocumentNumber,
    string ProjectName,
    string? ProjectNameArabic,
    Guid? CustomerId,
    string Status,
    IReadOnlyList<WbsElementSummary> WbsElements);

/// <summary>Read-only lookup another module calls to validate a Project/WBS element reference (e.g. a
/// Construction Contract's <c>ProjectId</c> and each of its BOQ lines' <c>WbsElementId</c>) before accepting
/// it. Implemented in Modules.ProjectManagement.Infrastructure, registered in Gateway.Api's DI container.</summary>
public interface IProjectLookup
{
    Task<ProjectSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
