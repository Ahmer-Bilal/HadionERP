namespace Modules.Finance.Application;

public sealed record JournalLineDto(
    Guid Id,
    Guid GLAccountId,
    Guid? CostCenterId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? LineDescription);

public sealed record JournalEntryDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    DateOnly PostingDate,
    string Description,
    Guid? ReversalOfEntryId,
    decimal TotalDebits,
    decimal TotalCredits,
    bool IsBalanced,
    IReadOnlyList<JournalLineDto> Lines,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateJournalLineRequest(
    Guid GLAccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    Guid? CostCenterId = null,
    string? LineDescription = null);

public sealed record CreateJournalEntryRequest(
    DateOnly PostingDate,
    string Description,
    IReadOnlyList<CreateJournalLineRequest> Lines);
