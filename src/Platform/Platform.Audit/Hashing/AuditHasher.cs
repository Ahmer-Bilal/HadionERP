using System.Security.Cryptography;
using System.Text;

namespace Platform.Audit.Hashing;

/// <summary>
/// The tamper-evidence mechanism docs/architecture/03-platform-services.md #5 calls for ("audit records are
/// hash-chained (each record's hash includes the previous record's hash) so undetected retroactive edits are
/// computationally evident").
///
/// Each entry's hash is SHA-256 over a deterministic canonical string built from the entry's immutable
/// content plus the PREVIOUS entry's hash. That linkage is what makes tampering detectable: changing any
/// field of an old record changes its hash, which changes the PreviousHash of every entry after it, so
/// <see cref="IAuditLog.VerifyChain"/> (which recomputes every hash from scratch) will fail at the first
/// altered entry. This is the same spirit as SAP's change-document framework and any hash-chained/genuine
/// ledger design.
///
/// Deliberately not a Merkle tree or a registered hash algorithm — a flat chain is simpler, and the goal
/// here is detecting a tampered record in-place, not producing a compact proof of inclusion.
/// </summary>
public static class AuditHasher
{
    /// <summary>Computes the hash for an entry given its predecessor's hash. The entry's own
    /// <see cref="AuditEntry.Hash"/> field is deliberately excluded (it's the output, not an input).</summary>
    public static string ComputeHash(AuditEntry entry, string? previousHash)
    {
        var canonical = BuildCanonicalString(entry, previousHash);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Verifies a single entry's stored hash against a recomputation, given its stored previous-hash.
    /// Used by <see cref="IAuditLog.VerifyChain"/> to walk the chain. Keeping this separate from the
    /// recomputation in the append path means the verify path can't accidentally reuse stale state.</summary>
    public static bool IsValid(AuditEntry entry, string? expectedPreviousHash) =>
        entry.Hash == ComputeHash(entry, expectedPreviousHash);

    private static string BuildCanonicalString(AuditEntry entry, string? previousHash)
    {
        // Field order is fixed and deliberate: any reordering would change every hash in an existing log.
        // Field-value changes are joined by a delimiter that can't appear inside a JSON value's outer
        // structure ambiguously; we include each change's name and both values so a changed value (the
        // actual thing we're protecting against) feeds the hash directly.
        var sb = new StringBuilder();
        sb.Append(entry.Id).Append('|');
        sb.Append(entry.Action).Append('|');
        sb.Append(entry.OccurredAt.Ticks).Append('|');
        sb.Append(entry.ActorPrincipalKey).Append('|');
        sb.Append(entry.BusinessObject.TargetId).Append('|');
        sb.Append(entry.BusinessObject.TargetType).Append('|');
        sb.Append(entry.BusinessObject.RelationKind).Append('|');
        sb.Append(entry.Summary).Append('|');
        sb.Append(entry.Source ?? string.Empty).Append('|');
        sb.Append(entry.CorrelationId).Append('|');
        sb.Append(previousHash ?? string.Empty).Append('|');

        foreach (var change in entry.FieldValueChanges)
        {
            sb.Append('[').Append(change.FieldName)
              .Append(':').Append(change.OldValueJson ?? string.Empty)
              .Append("->").Append(change.NewValueJson ?? string.Empty).Append(']');
        }

        return sb.ToString();
    }
}
