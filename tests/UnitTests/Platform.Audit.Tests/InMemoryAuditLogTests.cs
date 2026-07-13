using Platform.Core;

namespace Platform.Audit.Tests;

/// <summary>
/// Proves the log builds a chain on append, filters by Business Object, and — on a clean, untampered log —
/// <see cref="IAuditLog.VerifyChain"/> passes. The "tampering is actually detected" case lives in
/// <see cref="TamperEvidenceTests"/>; these tests are the happy path.
/// </summary>
public class InMemoryAuditLogTests
{
    private static BusinessObjectReference Po(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "PurchaseOrder", "Self");

    private static AuditEntry Entry(BusinessObjectReference bo, string summary) => new(
        Id: Guid.NewGuid(),
        Action: AuditAction.Create,
        OccurredAt: DateTimeOffset.UtcNow,
        ActorPrincipalKey: "user/alice",
        BusinessObject: bo,
        Summary: summary,
        FieldValueChanges: Array.Empty<FieldValueChange>(),
        Source: null,
        CorrelationId: null,
        PreviousHash: null,
        Hash: null);

    [Fact]
    public void First_entry_links_to_null_previous_hash()
    {
        var log = new InMemoryAuditLog();

        var stored = log.Append(Entry(Po(), "Created"));

        Assert.Null(stored.PreviousHash);
        Assert.NotNull(stored.Hash);
        Assert.Single(log.GetAll());
    }

    [Fact]
    public void Each_entry_links_to_the_previous_entrys_hash()
    {
        var log = new InMemoryAuditLog();
        var bo = Po();

        var first = log.Append(Entry(bo, "Created"));
        var second = log.Append(Entry(bo, "Submitted"));

        Assert.Null(first.PreviousHash);
        Assert.Equal(first.Hash, second.PreviousHash);
        Assert.NotEqual(first.Hash, second.Hash);
        Assert.Equal(2, log.GetAll().Count);
    }

    [Fact]
    public void GetFor_returns_only_the_entries_touching_that_business_object()
    {
        var log = new InMemoryAuditLog();
        var po1 = Po();
        var po2 = Po();

        log.Append(Entry(po1, "Created PO 1"));
        log.Append(Entry(po2, "Created PO 2"));
        log.Append(Entry(po1, "Submitted PO 1"));

        Assert.Equal(2, log.GetFor(po1).Count);
        Assert.Single(log.GetFor(po2));
    }

    [Fact]
    public void VerifyChain_passes_on_an_clean_log()
    {
        var log = new InMemoryAuditLog();
        var bo = Po();
        log.Append(Entry(bo, "Created"));
        log.Append(Entry(bo, "Submitted"));
        log.Append(Entry(bo, "Approved"));

        Assert.Null(log.VerifyChain());
    }

    [Fact]
    public void VerifyChain_passes_on_an_empty_log()
        => Assert.Null(new InMemoryAuditLog().VerifyChain());
}
