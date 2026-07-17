using Platform.Core;

namespace Platform.Audit;

/// <summary>
/// The friendly facade modules call to record "something just happened" — the single entry point for audit,
/// the same way <c>IIntegrationEventPublisher</c> is the single entry point for cross-module events
/// (modules never call <see cref="IAuditLog"/> directly to build entries by hand). This is what keeps the
/// captured content consistent across every module (who/when/from-where populated the same way everywhere)
/// and is where a future session/user-context lookup would be wired (docs/architecture/04-platform-services.md #5:
/// "who, what (field-level before/after), when, from where, why").
/// </summary>
public interface IAuditRecorder
{
    /// <summary>Records the creation of a record.</summary>
    AuditEntry RecordCreate(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? source = null,
        Guid? correlationId = null);

    /// <summary>Records a field-level update: pass the before/after values for each changed field and only
    /// those are captured (unchanged fields don't clutter the audit trail).</summary>
    AuditEntry RecordFieldUpdate(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        IReadOnlyList<FieldValueChange> changes,
        string? source = null,
        Guid? correlationId = null);

    /// <summary>Records a lifecycle status transition (docs/architecture/02-business-object-model.md §1).
    /// Recorded distinctly from a field update because the transition itself is the audit-relevant fact.</summary>
    AuditEntry RecordStatusTransition(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? fromStatusJson,
        string? toStatusJson,
        string? source = null,
        Guid? correlationId = null);

    /// <summary>Records a delete attempt — whether it succeeded or not. An attempted deletion of a Posted
    /// financial document is itself an audit-relevant event (§2 doc 02: posted docs are reversed, never
    /// deleted).</summary>
    AuditEntry RecordDeleteAttempt(
        BusinessObjectReference businessObject,
        string actorPrincipalKey,
        string summary,
        string? source = null,
        Guid? correlationId = null);
}
