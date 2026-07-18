namespace Modules.Finance.Application;

public sealed record ClosingActivityStepDto(
    Guid Id,
    string Description,
    bool IsAutoTracked,
    bool IsCompleted,
    string? CompletedBy,
    DateTimeOffset? CompletedAt);

public sealed record ClosingActivityDto(
    Guid Id,
    Guid FiscalPeriodId,
    string ActivityKey,
    int SequenceNumber,
    string Title,
    string Description,
    Guid? AssignedToUserId,
    string? AssignedToDisplayName,
    string? AssignedToRoleKey,
    DateOnly? DueDate,
    string Status,
    int CompletedSteps,
    int TotalSteps,
    IReadOnlyList<ClosingActivityStepDto> Steps,
    string? LastActionBy,
    DateTimeOffset? LastActionAt);

public sealed record AssignClosingActivityRequest(Guid UserId, DateOnly? DueDate);

/// <summary>A single real fact reported back to the Period Closing Center's "Closing Insights" panel — see
/// <c>ClosingActivityService.GetInsightsAsync</c> for how each one is derived from actual checklist state,
/// never fabricated.</summary>
public sealed record ClosingInsightDto(string Severity, string Title, string Message);

public sealed record ClosingActivityLogEntryDto(DateTimeOffset At, string Actor, string Message);

public sealed record CompletionTrendPointDto(DateOnly Date, decimal PercentComplete);
