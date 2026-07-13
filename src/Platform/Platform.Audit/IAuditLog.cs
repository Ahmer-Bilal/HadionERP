using Platform.Core;

namespace Platform.Audit;

/// <summary>
/// The permanent, append-only, tamper-evident record of changes
/// (docs/architecture/03-platform-services.md #5). This is the read/append surface only — modules don't
/// construct <see cref="AuditEntry"/> values by hand; they go through <see cref="IAuditRecorder"/>, which is
/// the friendly facade for "record what just happened."
///
/// Append-only is enforced by contract here and at the DB-role level in a real deployment (§5: "no
/// UPDATE/DELETE grants on the audit schema"). There is no Update or Delete method on this interface by
/// design.
/// </summary>
public interface IAuditLog
{
    /// <summary>Appends one record to the end of the chain, computing its <see cref="AuditEntry.Hash"/> from
    /// the current tail's hash. Returns the stored entry (with its hash filled in).</summary>
    AuditEntry Append(AuditEntry entry);

    /// <summary>All entries, in chain order (oldest first). Read-only snapshot.</summary>
    IReadOnlyList<AuditEntry> GetAll();

    /// <summary>The subset of entries touching a specific Business Object, in chain order. This is what
    /// powers the "Change history / document changes" facet on a record's page (doc 02 §2).</summary>
    IReadOnlyList<AuditEntry> GetFor(BusinessObjectReference businessObject);

    /// <summary>Recomputes every hash from the genesis entry forward and returns the first entry whose
    /// stored hash disagrees with its recomputed hash — the tamper-evidence check (§5). Returns null when
    /// the entire chain is intact.</summary>
    AuditEntry? VerifyChain();
}
