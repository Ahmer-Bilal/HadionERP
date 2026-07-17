namespace Platform.Core.NumberRanges;

/// <summary>
/// Assigns human-facing document numbers, scoped per company/branch/fiscal year, per the naming
/// convention in docs/architecture/06-engineering-standards.md §2:
/// "{ModuleAbbrev}-{DocAbbrev}-{Year}-{Seq}", e.g. PROC-PO-2026-000123.
///
/// This is a platform service (Platform.Core), not something each module reimplements — Business
/// Objects call it once, at Draft creation or at Submit, depending on the configured numbering point
/// (a later configuration concern, not a kernel one).
/// </summary>
public interface INumberRangeService
{
    /// <summary>
    /// Returns the next number for the given range, formatted per that range's definition.
    /// Must be safe to call concurrently — two documents must never receive the same number.
    /// </summary>
    string GetNext(string rangeKey, string companyId, int fiscalYear);
}
