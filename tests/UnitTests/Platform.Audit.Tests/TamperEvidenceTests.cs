using Platform.Core;

namespace Platform.Audit.Tests;

/// <summary>
/// THE headline guarantee of docs/architecture/03-platform-services.md #5: "audit records are hash-chained
/// ... so undetected retroactive edits are computationally evident." Proved here, not assumed — an attacker
/// with write access to the audit store edits an old record's content, and <see cref="IAuditLog.VerifyChain"/>
/// must surface it. This mirrors the spirit of SAP's change-document framework: you cannot silently rewrite
/// history without the chain breaking.
/// </summary>
public class TamperEvidenceTests
{
    private static AuditEntry Entry(string summary) => new(
        Id: Guid.NewGuid(),
        Action: AuditAction.Update,
        OccurredAt: DateTimeOffset.UtcNow,
        ActorPrincipalKey: "user/alice",
        BusinessObject: new BusinessObjectReference(Guid.NewGuid(), "PurchaseOrder", "Self"),
        Summary: summary,
        FieldValueChanges: new[] { new FieldValueChange("Amount", "\"100\"", "\"200\"") },
        Source: null,
        CorrelationId: null,
        PreviousHash: null,
        Hash: null);

    [Fact]
    public void Editing_a_middle_records_content_is_detected_at_that_record()
    {
        var log = new InMemoryAuditLog();
        log.Append(Entry("Created"));
        var tampered = log.Append(Entry("Amount changed from 100 to 200"));
        log.Append(Entry("Approved"));

        // Sanity: the chain is intact before the tampering.
        Assert.Null(log.VerifyChain());

        // Simulate a retroactive edit of the stored record's content (e.g. an attacker rewriting the
        // summary to hide that an amount ever changed). The record type's setters are init-only, so use
        // reflection — this stands in for a direct table UPDATE in a real DB, which is exactly the threat
        // the hash chain defends against (and which DB-role hardening would additionally block at the
        // source — §5).
        MutateField(tampered, nameof(AuditEntry.Summary), "Amount changed from 100 to 100");

        var broken = log.VerifyChain();

        Assert.NotNull(broken);
        Assert.Equal(tampered.Id, broken!.Id);
    }

    [Fact]
    public void Editing_the_first_record_is_detected()
    {
        var log = new InMemoryAuditLog();
        var first = log.Append(Entry("Created"));
        log.Append(Entry("Submitted"));

        MutateField(first, nameof(AuditEntry.Summary), "Created (forged)");

        Assert.NotNull(log.VerifyChain());
    }

    [Fact]
    public void Editing_the_last_record_is_detected()
    {
        var log = new InMemoryAuditLog();
        log.Append(Entry("Created"));
        var last = log.Append(Entry("Submitted"));

        MutateField(last, nameof(AuditEntry.ActorPrincipalKey), "user/attacker");

        Assert.NotNull(log.VerifyChain());
    }

    [Fact]
    public void Swapping_two_records_is_detected()
    {
        // A reordering (e.g. swapping two records so a later event appears to predate an earlier one) must
        // break the chain even though no record's own content changed — the PreviousHash links no longer
        // line up. This is why VerifyChain checks the stored link, not just each entry's own hash.
        var log = new InMemoryAuditLog();
        log.Append(Entry("Created"));
        log.Append(Entry("Submitted"));

        SwapFirstTwoStoredEntries(log);

        Assert.NotNull(log.VerifyChain());
    }

    private static void MutateField(AuditEntry entry, string fieldName, object newValue)
    {
        // AuditEntry is a positional record; its backing properties are init-only. Reflecting over the
        // underlying field gives the test a way to simulate a hostile in-place mutation without giving
        // production code that ability.
        var property = typeof(AuditEntry).GetProperty(fieldName)!;
        var backingField = typeof(AuditEntry).GetField(
            $"<{fieldName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        backingField.SetValue(entry, newValue);
    }

    private static void SwapFirstTwoStoredEntries(InMemoryAuditLog log)
    {
        var entriesField = typeof(InMemoryAuditLog).GetField(
            "_entries",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var list = (List<AuditEntry>)entriesField.GetValue(log)!;
        (list[0], list[1]) = (list[1], list[0]);
    }
}
