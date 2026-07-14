namespace Platform.Notes;

/// <summary>
/// One free-text note attached to a Business Object, identified by a flat
/// (<see cref="BusinessObjectType"/>, <see cref="BusinessObjectId"/>) pair — the same polymorphic-link
/// shape <c>Platform.Attachments.AttachmentMetadata</c> and <c>Platform.Workflow.WorkflowInstance</c>
/// already use.
///
/// Deliberately append-only/delete-only — there is no <c>UpdateText</c> — matching the same "correct by
/// reversal, not by silent edit" principle the rest of this platform already applies to posted financial
/// documents (docs/architecture/02-business-object-model.md §1.1): a note is what someone actually said at
/// the time; if it was wrong, delete it and add a new one, don't rewrite history.
/// </summary>
public sealed class Note
{
    public Guid Id { get; private set; }
    public string BusinessObjectType { get; private set; }
    public Guid BusinessObjectId { get; private set; }
    public string Text { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal Note(string businessObjectType, Guid businessObjectId, string text, string createdBy)
    {
        Id = Guid.NewGuid();
        BusinessObjectType = businessObjectType;
        BusinessObjectId = businessObjectId;
        Text = text;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserved for ORM materialization — mirrors <c>Platform.Core.BusinessObject</c>'s own
    /// parameterless constructor. Never call from application code; use <see cref="INoteService.AddAsync"/>.</summary>
    private Note()
    {
        BusinessObjectType = null!;
        Text = null!;
        CreatedBy = null!;
    }
}
