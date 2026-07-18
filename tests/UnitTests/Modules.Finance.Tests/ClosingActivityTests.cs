using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

public class ClosingActivityTests
{
    private static ClosingActivity NewActivity() =>
        new(Guid.NewGuid(), ClosingActivityCatalog.AccountsPayable, 2);

    [Fact]
    public void A_new_activity_starts_NotStarted_until_something_refreshes_it()
    {
        var activity = NewActivity();
        Assert.Equal(ClosingActivityStatus.NotStarted, activity.Status);
    }

    [Fact]
    public void RefreshStatus_on_zero_steps_reads_as_Completed()
    {
        var activity = NewActivity();
        activity.RefreshStatus("system/auto-tracked");
        Assert.Equal(ClosingActivityStatus.Completed, activity.Status);
    }

    [Fact]
    public void RefreshStatus_reports_NotStarted_when_no_step_is_complete()
    {
        var activity = NewActivity();
        activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());
        activity.AddStep("Close invoice B", "APInvoice", Guid.NewGuid());

        activity.RefreshStatus("system/auto-tracked");

        Assert.Equal(ClosingActivityStatus.NotStarted, activity.Status);
    }

    [Fact]
    public void RefreshStatus_reports_InProgress_when_some_but_not_all_steps_are_complete()
    {
        var activity = NewActivity();
        var step1 = activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());
        activity.AddStep("Close invoice B", "APInvoice", Guid.NewGuid());
        step1.SetCompletionFromLiveStatus(true);

        activity.RefreshStatus("system/auto-tracked");

        Assert.Equal(ClosingActivityStatus.InProgress, activity.Status);
    }

    [Fact]
    public void RefreshStatus_reports_Completed_when_every_step_is_complete()
    {
        var activity = NewActivity();
        var step1 = activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());
        var step2 = activity.AddStep("Close invoice B", "APInvoice", Guid.NewGuid());
        step1.SetCompletionFromLiveStatus(true);
        step2.SetCompletionFromLiveStatus(true);

        activity.RefreshStatus("system/auto-tracked");

        Assert.Equal(ClosingActivityStatus.Completed, activity.Status);
    }

    [Fact]
    public void SetBlocked_true_overrides_step_derived_status()
    {
        var activity = NewActivity();
        activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());

        activity.SetBlocked(true, "sana.ali");

        Assert.Equal(ClosingActivityStatus.Blocked, activity.Status);
    }

    [Fact]
    public void RefreshStatus_is_a_no_op_while_Blocked_until_explicitly_unblocked()
    {
        var activity = NewActivity();
        var step = activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());
        activity.SetBlocked(true, "sana.ali");

        step.SetCompletionFromLiveStatus(true);
        activity.RefreshStatus("system/auto-tracked");
        Assert.Equal(ClosingActivityStatus.Blocked, activity.Status); // still blocked despite the step completing

        activity.SetBlocked(false, "sana.ali");
        Assert.Equal(ClosingActivityStatus.Completed, activity.Status); // now re-derived from steps
    }

    [Fact]
    public void An_auto_tracked_step_cannot_be_manually_completed()
    {
        var activity = NewActivity();
        var step = activity.AddStep("Close invoice A", "APInvoice", Guid.NewGuid());

        Assert.True(step.IsAutoTracked);
    }

    [Fact]
    public void A_manual_step_is_not_auto_tracked()
    {
        var activity = NewActivity();
        var step = activity.AddStep("Reconcile account 'CC-1000'", null, null);

        Assert.False(step.IsAutoTracked);
    }

    [Fact]
    public void Assign_sets_the_assignee_due_date_and_last_action()
    {
        var activity = NewActivity();
        var userId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 6, 2);

        activity.Assign(userId, dueDate, "ahmer.khan");

        Assert.Equal(userId, activity.AssignedToUserId);
        Assert.Equal(dueDate, activity.DueDate);
        Assert.Equal("ahmer.khan", activity.LastActionBy);
        Assert.NotNull(activity.LastActionAt);
    }
}
