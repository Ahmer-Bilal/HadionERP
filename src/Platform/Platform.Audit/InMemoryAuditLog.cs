using Platform.Audit.Hashing;
using Platform.Core;

namespace Platform.Audit;

/// <summary>Reference implementation of <see cref="IAuditLog"/> — same "swap for a database-backed append-only
/// table later behind the same interface" pattern as the rest of the kernel. A real deployment writes the
/// entry in the same database transaction as the business change (so a failed business write can't leave an
/// orphaned audit record, and a successful one can't silently escape audit), with the audit DB role denied
/// UPDATE/DELETE (§5). This in-memory version proves the chain mechanics first.</summary>
public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly object _lock = new();
    private readonly List<AuditEntry> _entries = new();
    private string? _tailHash;

    public AuditEntry Append(AuditEntry entry)
    {
        lock (_lock)
        {
            // The hash is derived from the entry's content AND the previous entry's hash, so this entry
            // becomes part of an unbreakable chain: mutating it later invalidates its own hash, and
            // mutating any earlier entry invalidates this one's PreviousHash link.
            var previousHash = _tailHash;
            var hash = AuditHasher.ComputeHash(entry, previousHash);

            var stored = entry with { PreviousHash = previousHash, Hash = hash };
            _entries.Add(stored);
            _tailHash = hash;
            return stored;
        }
    }

    public IReadOnlyList<AuditEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    public IReadOnlyList<AuditEntry> GetFor(BusinessObjectReference businessObject)
    {
        lock (_lock)
        {
            return _entries
                .Where(e => e.BusinessObject.TargetId == businessObject.TargetId
                         && e.BusinessObject.TargetType == businessObject.TargetType)
                .ToList();
        }
    }

    public AuditEntry? VerifyChain()
    {
        lock (_lock)
        {
            string? expectedPreviousHash = null;
            foreach (var entry in _entries)
            {
                // Both the stored PreviousHash link and the entry's own hash must hold. Checking the link
                // catches a reordering or swap; recomputing the entry hash catches an in-place mutation.
                if (entry.PreviousHash != expectedPreviousHash || !AuditHasher.IsValid(entry, entry.PreviousHash))
                {
                    return entry;
                }

                expectedPreviousHash = entry.Hash;
            }

            return null;
        }
    }
}
