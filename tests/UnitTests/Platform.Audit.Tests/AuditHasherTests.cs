using Platform.Audit.Hashing;
using Platform.Core;

namespace Platform.Audit.Tests;

/// <summary>
/// The hash is the foundation of tamper-evidence: if it weren't deterministic, or if changing content
/// didn't change the hash, the whole chain-check would be meaningless. These tests pin those properties
/// directly on <see cref="AuditHasher"/> before anything else relies on it.
/// </summary>
public class AuditHasherTests
{
    private static AuditEntry NewEntry(string summary = "summary") => new(
        Id: Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Action: AuditAction.Create,
        OccurredAt: new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero),
        ActorPrincipalKey: "user/alice",
        BusinessObject: new BusinessObjectReference(Guid.Parse("00000000-0000-0000-0000-000000000009"), "PurchaseOrder", "Self"),
        Summary: summary,
        FieldValueChanges: Array.Empty<FieldValueChange>(),
        Source: "127.0.0.1",
        CorrelationId: null,
        PreviousHash: null,
        Hash: null);

    [Fact]
    public void Same_content_and_previous_hash_produces_the_same_hash()
    {
        var entry = NewEntry();

        var hash1 = AuditHasher.ComputeHash(entry, previousHash: null);
        var hash2 = AuditHasher.ComputeHash(entry, previousHash: null);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Different_summary_produces_a_different_hash()
    {
        var a = AuditHasher.ComputeHash(NewEntry(summary: "Created PO"), previousHash: null);
        var b = AuditHasher.ComputeHash(NewEntry(summary: "Created PO (edited)"), previousHash: null);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Different_previous_hash_produces_a_different_hash()
    {
        var entry = NewEntry();

        var withNoPredecessor = AuditHasher.ComputeHash(entry, previousHash: null);
        var withPredecessor = AuditHasher.ComputeHash(entry, previousHash: "ABCDEF");

        Assert.NotEqual(withNoPredecessor, withPredecessor);
    }

    [Fact]
    public void Different_field_value_change_produces_a_different_hash()
    {
        var before = NewEntry() with
        {
            FieldValueChanges = new[] { new FieldValueChange("Amount", "100", "100") }
        };
        var after = NewEntry() with
        {
            FieldValueChanges = new[] { new FieldValueChange("Amount", "100", "200") }
        };

        Assert.NotEqual(
            AuditHasher.ComputeHash(before, previousHash: null),
            AuditHasher.ComputeHash(after, previousHash: null));
    }

    [Fact]
    public void The_hash_is_sha256_length_hex()
    {
        var hash = AuditHasher.ComputeHash(NewEntry(), previousHash: null);

        // SHA-256 = 32 bytes = 64 hex chars.
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]{64}$", hash);
    }
}
