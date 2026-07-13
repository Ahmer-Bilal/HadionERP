using Platform.Core;

namespace Platform.Audit.Tests;

/// <summary>
/// Proves <see cref="IAuditRecorder"/> — the single entry point modules call — captures the right action
/// type, field-level diffs, and metadata for each kind of recorded change, and that the chain stays valid
/// after each append. This is the contract every future business module will rely on.
/// </summary>
public class AuditRecorderTests
{
    private static BusinessObjectReference Po() =>
        new(Guid.NewGuid(), "PurchaseOrder", "Self");

    private static (AuditRecorder recorder, InMemoryAuditLog log) NewRecorder()
    {
        var log = new InMemoryAuditLog();
        return (new AuditRecorder(log), log);
    }

    [Fact]
    public void RecordCreate_appends_a_create_entry_with_no_field_changes()
    {
        var (recorder, log) = NewRecorder();
        var bo = Po();

        var entry = recorder.RecordCreate(bo, "user/alice", "Created PO PROC-PO-2026-000123");

        Assert.Equal(AuditAction.Create, entry.Action);
        Assert.Empty(entry.FieldValueChanges);
        Assert.Equal("user/alice", entry.ActorPrincipalKey);
        Assert.Null(log.VerifyChain());
    }

    [Fact]
    public void RecordFieldUpdate_captures_the_before_after_of_each_changed_field()
    {
        var (recorder, log) = NewRecorder();
        var bo = Po();

        var entry = recorder.RecordFieldUpdate(
            bo,
            "user/bob",
            "Edited vendor and amount",
            new[]
            {
                new FieldValueChange("VendorId", "\"V001\"", "\"V002\""),
                new FieldValueChange("Amount", "\"1000\"", "\"1500\"")
            });

        Assert.Equal(AuditAction.Update, entry.Action);
        Assert.Equal(2, entry.FieldValueChanges.Count);
        Assert.Equal("Amount", entry.FieldValueChanges[1].FieldName);
        Assert.Equal("\"1000\"", entry.FieldValueChanges[1].OldValueJson);
        Assert.Equal("\"1500\"", entry.FieldValueChanges[1].NewValueJson);
        Assert.Null(log.VerifyChain());
    }

    [Fact]
    public void RecordStatusTransition_captures_the_from_and_to_status()
    {
        var (recorder, log) = NewRecorder();
        var bo = Po();

        var entry = recorder.RecordStatusTransition(
            bo, "user/alice", "Approved", fromStatusJson: "\"Submitted\"", toStatusJson: "\"Approved\"");

        Assert.Equal(AuditAction.StatusTransition, entry.Action);
        var statusChange = Assert.Single(entry.FieldValueChanges);
        Assert.Equal("Status", statusChange.FieldName);
        Assert.Equal("\"Submitted\"", statusChange.OldValueJson);
        Assert.Equal("\"Approved\"", statusChange.NewValueJson);
        Assert.Null(log.VerifyChain());
    }

    [Fact]
    public void RecordDeleteAttempt_appends_a_delete_attempt_entry()
    {
        var (recorder, log) = NewRecorder();
        var bo = Po();

        var entry = recorder.RecordDeleteAttempt(bo, "user/carol", "Attempted to delete a Posted PO");

        Assert.Equal(AuditAction.DeleteAttempt, entry.Action);
        Assert.Equal("Attempted to delete a Posted PO", entry.Summary);
        Assert.Null(log.VerifyChain());
    }

    [Fact]
    public void Every_recorded_entry_carries_a_timestamp_and_a_filled_in_hash()
    {
        var (recorder, _) = NewRecorder();

        var entry = recorder.RecordCreate(Po(), "user/alice", "Created");

        // The recorder never sets the hash — the log fills it in at append. A null hash here would mean
        // an entry slipped into the world without chain protection.
        Assert.NotNull(entry.Hash);
        Assert.NotEqual(default, entry.OccurredAt);
    }

    [Fact]
    public void A_sequence_of_recorded_actions_produces_an_intact_chain()
    {
        var (recorder, log) = NewRecorder();
        var bo = Po();

        recorder.RecordCreate(bo, "user/alice", "Created");
        recorder.RecordFieldUpdate(bo, "user/alice", "Edited amount",
            new[] { new FieldValueChange("Amount", "\"1000\"", "\"1500\"") });
        recorder.RecordStatusTransition(bo, "user/bob", "Approved", "\"Submitted\"", "\"Approved\"");
        recorder.RecordDeleteAttempt(bo, "user/carol", "Attempted delete");

        Assert.Equal(4, log.GetAll().Count);
        Assert.Null(log.VerifyChain());
    }
}
