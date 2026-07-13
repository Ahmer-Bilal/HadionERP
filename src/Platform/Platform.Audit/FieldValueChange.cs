namespace Platform.Audit;

/// <summary>
/// One field's before/after on an <see cref="AuditAction.Update"/> entry — the field-level change capture
/// docs/architecture/03-platform-services.md #5 requires ("field-level before/after"). Values are stored as
/// JSON strings rather than typed objects so the audit log records exactly what was written, not a
/// lossy/normalized reinterpretation — important for statutory defensibility (an auditor needs to see the
/// literal value that was on the record, not a reformatted one).
/// </summary>
public sealed record FieldValueChange(string FieldName, string? OldValueJson, string? NewValueJson);
