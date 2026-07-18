namespace Modules.Finance.Application;

/// <summary>One node in a Journal Entry's document flow — see <see cref="JournalEntryDocumentFlowService"/>
/// for how each is derived.</summary>
public sealed record JournalEntryDocumentFlowNodeDto(
    string Kind,
    string Label,
    string? DocumentNumber,
    Guid? DocumentId,
    string Status,
    bool IsCurrent);
