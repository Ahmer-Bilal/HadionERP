using Modules.Finance.Domain;
using Platform.Core;

namespace Modules.Finance.Tests;

public class BudgetTests
{
    [Fact]
    public void A_new_budget_starts_in_draft_with_no_document_number()
    {
        var budget = new Budget("ahmer.bilal", Guid.NewGuid(), 2026, 50000m);

        Assert.Equal(BusinessObjectStatus.Draft, budget.Status);
        Assert.Null(budget.DocumentNumber);
        Assert.Equal(2026, budget.FiscalYear);
        Assert.Equal(50000m, budget.Amount);
    }

    [Fact]
    public void Zero_or_negative_amount_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Budget("ahmer.bilal", Guid.NewGuid(), 2026, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Budget("ahmer.bilal", Guid.NewGuid(), 2026, -1m));
    }

    [Fact]
    public void An_out_of_range_fiscal_year_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Budget("ahmer.bilal", Guid.NewGuid(), 1999, 1000m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Budget("ahmer.bilal", Guid.NewGuid(), 2101, 1000m));
    }

    [Fact]
    public void Full_lifecycle_draft_to_approved()
    {
        var budget = new Budget("ahmer.bilal", Guid.NewGuid(), 2026, 50000m);
        budget.AssignNumber("FIN-BUD-2026-000001");

        budget.Submit("ahmer.bilal");
        Assert.Equal(BusinessObjectStatus.Submitted, budget.Status);

        budget.Approve("finance.manager");
        Assert.Equal(BusinessObjectStatus.Approved, budget.Status);
    }

    [Fact]
    public void A_rejected_budget_can_be_rejected_but_not_re_approved_afterward()
    {
        var budget = new Budget("ahmer.bilal", Guid.NewGuid(), 2026, 50000m);
        budget.AssignNumber("FIN-BUD-2026-000001");
        budget.Submit("ahmer.bilal");

        budget.Reject("finance.manager");
        Assert.Equal(BusinessObjectStatus.Rejected, budget.Status);
        Assert.Throws<Platform.Core.Lifecycle.InvalidLifecycleTransitionException>(() => budget.Approve("finance.manager"));
    }
}
