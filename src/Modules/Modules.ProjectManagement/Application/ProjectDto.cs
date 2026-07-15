namespace Modules.ProjectManagement.Application;

public sealed record WbsElementDto(
    Guid Id, string Code, string Name, string? NameArabic, Guid? ParentWbsElementId,
    bool IsPlanningElement, bool IsAccountAssignmentElement, bool IsBillingElement);

public sealed record ProjectDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string ProjectName,
    string? ProjectNameArabic,
    Guid? CustomerId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyList<WbsElementDto> WbsElements,
    DateTimeOffset CreatedAt,
    string CreatedBy);

/// <summary>
/// <see cref="TempId"/>/<see cref="ParentTempId"/> let the caller specify a multi-level hierarchy in one
/// request before any element has a real id yet — a WBS element referencing its own parent by a
/// caller-assigned integer, resolved against elements already processed earlier in
/// <see cref="CreateProjectRequest.WbsElements"/> (so a parent must appear before its children in the
/// list). <see cref="ParentTempId"/> null means "top-level element."
/// </summary>
public sealed record CreateWbsElementRequest(
    int TempId, int? ParentTempId, string Code, string Name, string? NameArabic,
    bool IsPlanningElement, bool IsAccountAssignmentElement, bool IsBillingElement);

public sealed record CreateProjectRequest(
    string ProjectName, string? ProjectNameArabic, Guid? CustomerId, DateOnly? StartDate, DateOnly? EndDate,
    IReadOnlyList<CreateWbsElementRequest> WbsElements);
