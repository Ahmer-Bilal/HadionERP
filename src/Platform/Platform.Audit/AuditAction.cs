namespace Platform.Audit;

/// <summary>
/// The kind of change an <see cref="AuditEntry"/> records — the four categories named in
/// docs/architecture/04-platform-services.md #5 ("every create/update/status-transition/delete-attempt").
/// </summary>
public enum AuditAction
{
    /// <summary>A new Business Object (or other audited entity) was created.</summary>
    Create,

    /// <summary>One or more fields on an existing record changed value (field-level before/after captured).</summary>
    Update,

    /// <summary>A Business Object moved through its lifecycle FSM (docs/architecture/02-business-object-model.md
    /// §1) — e.g. Draft → Submitted. These are recorded separately from field updates because they carry
    /// workflow/approval significance an auditor cares about distinctly.</summary>
    StatusTransition,

    /// <summary>An attempt to delete a record (which may be refused by the lifecycle, e.g. a Posted document
    /// can only be reversed, never deleted — §2 doc 02). Logged whether it succeeded or not, since an
    /// attempted deletion of a posted financial document is itself an audit-relevant event.</summary>
    DeleteAttempt,
}
