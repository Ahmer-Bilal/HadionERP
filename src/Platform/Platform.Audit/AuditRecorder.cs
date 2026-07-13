using Platform.Core;

namespace Platform.Audit;

/// <summary>Builds and appends a complete <see cref="AuditEntry"/> for each recorded action. Keeps the
/// capture consistent (timestamp, actor, source populated uniformly) so a module's code reads like
/// "audit.RecordFieldUpdate(...)" rather than each module reinventing entry construction.</summary>
public sealed class AuditRecorder : IAuditRecorder
{
    private readonly IAuditLog _log;

    public AuditRecorder(IAuditLog log) => _log = log;

    public AuditEntry RecordCreate(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? source = null,
        Guid? correlationId = null) =>
        Append(AuditAction.Create, businessObject, actorPrincipalKey, summary, changes: null, source, correlationId);

    public AuditEntry RecordFieldUpdate(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        IReadOnlyList<FieldValueChange> changes,
        string? source = null,
        Guid? correlationId = null) =>
        Append(AuditAction.Update, businessObject, actorPrincipalKey, summary, changes, source, correlationId);

    public AuditEntry RecordStatusTransition(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? fromStatusJson,
        string? toStatusJson,
        string? source = null,
        Guid? correlationId = null)
    {
        var changes = new[]
        {
            new FieldValueChange("Status", fromStatusJson, toStatusJson)
        };

        return Append(AuditAction.StatusTransition, businessObject, actorPrincipalKey, summary, changes, source, correlationId);
    }

    public AuditEntry RecordDeleteAttempt(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? source = null,
        Guid? correlationId = null) =>
        Append(AuditAction.DeleteAttempt, businessObject, actorPrincipalKey, summary, changes: null, source, correlationId);

    private AuditEntry Append(
        AuditAction action,
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        IReadOnlyList<FieldValueChange>? changes,
        string? source,
        Guid? correlationId)
    {
        var entry = new AuditEntry(
            Id: Guid.NewGuid(),
            Action: action,
            OccurredAt: DateTimeOffset.UtcNow,
            ActorPrincipalKey: actorPrincipalKey,
            BusinessObject: businessObject,
            Summary: summary,
            FieldValueChanges: changes ?? Array.Empty<FieldValueChange>(),
            Source: source,
            CorrelationId: correlationId,
            // PreviousHash/Hash are filled in by the log at append time — never by the caller.
            PreviousHash: null,
            Hash: null);

        return _log.Append(entry);
    }
}
