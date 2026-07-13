using Platform.Security.Sod;

namespace Platform.Security.Tests;

public class SodEngineTests
{
    private static readonly SodConflictRule VendorCreateVsPay =
        new("CreateVendor", "ApproveVendorPayment", "The same user must not both onboard a vendor and approve payment to it.");

    [Fact]
    public void Detects_a_conflict_when_both_duties_are_held()
    {
        var engine = new SodEngine(new[] { VendorCreateVsPay }, new InMemorySodExceptionLog());

        var conflicts = engine.FindConflicts(new[] { "CreateVendor", "ApproveVendorPayment", "SomeUnrelatedDuty" });

        var conflict = Assert.Single(conflicts);
        Assert.Equal(VendorCreateVsPay, conflict);
    }

    [Fact]
    public void No_conflict_when_only_one_side_of_the_pair_is_held()
    {
        var engine = new SodEngine(new[] { VendorCreateVsPay }, new InMemorySodExceptionLog());

        var conflicts = engine.FindConflicts(new[] { "CreateVendor", "SomeUnrelatedDuty" });

        Assert.Empty(conflicts);
    }

    [Fact]
    public void A_logged_exception_resolves_the_conflict_for_that_user_only()
    {
        var log = new InMemorySodExceptionLog();
        var engine = new SodEngine(new[] { VendorCreateVsPay }, log);
        var dutyKeys = new[] { "CreateVendor", "ApproveVendorPayment" };

        Assert.Single(engine.FindUnresolvedConflicts("u.smalloffice", dutyKeys));

        log.Grant("u.smalloffice", VendorCreateVsPay, approvedBy: "cfo", reason: "Two-person team, CFO reviews all payments monthly.");

        Assert.Empty(engine.FindUnresolvedConflicts("u.smalloffice", dutyKeys));
        // Someone else holding the same conflicting duties still gets blocked — the exception is per-user.
        Assert.Single(engine.FindUnresolvedConflicts("u.someoneelse", dutyKeys));

        var loggedEntry = Assert.Single(log.History);
        Assert.Equal("cfo", loggedEntry.ApprovedBy);
    }
}
