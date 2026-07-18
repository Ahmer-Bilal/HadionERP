using Modules.Finance.Domain;

namespace Modules.Finance.Tests;

public class FiscalYearTests
{
    [Fact]
    public void A_new_fiscal_year_auto_generates_12_monthly_periods()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);

        Assert.Equal(2026, year.Year);
        Assert.Equal(12, year.Periods.Count);
        Assert.Equal(Enumerable.Range(1, 12), year.Periods.OrderBy(p => p.PeriodNumber).Select(p => p.PeriodNumber));
    }

    [Fact]
    public void Each_period_spans_its_own_calendar_month()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);
        var february = year.Periods.Single(p => p.PeriodNumber == 2);

        Assert.Equal(new DateOnly(2026, 2, 1), february.StartDate);
        Assert.Equal(new DateOnly(2026, 2, 28), february.EndDate); // 2026 is not a leap year
    }

    [Fact]
    public void Every_period_starts_open_with_a_default_target_close_date()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);
        var may = year.Periods.Single(p => p.PeriodNumber == 5);

        Assert.True(may.IsOpen);
        Assert.Equal(may.EndDate.AddDays(5), may.TargetCloseDate);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void An_out_of_range_year_is_rejected(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FiscalYear("ahmer.bilal", year));
    }

    [Fact]
    public void FindPeriodFor_returns_the_covering_period()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);

        var found = year.FindPeriodFor(new DateOnly(2026, 5, 15));

        Assert.NotNull(found);
        Assert.Equal(5, found!.PeriodNumber);
    }

    [Fact]
    public void FindPeriodFor_returns_null_outside_the_year()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);

        Assert.Null(year.FindPeriodFor(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Close_and_reopen_toggle_IsOpen()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);
        var period = year.Periods.First();

        period.Close("finance.manager");
        Assert.False(period.IsOpen);

        period.Reopen("finance.manager");
        Assert.True(period.IsOpen);
    }

    [Fact]
    public void SetTargetCloseDate_updates_the_date_and_audit_fields()
    {
        var year = new FiscalYear("ahmer.bilal", 2026);
        var period = year.Periods.First();
        var newDate = new DateOnly(2026, 2, 10);

        period.SetTargetCloseDate(newDate, "finance.manager");

        Assert.Equal(newDate, period.TargetCloseDate);
        Assert.Equal("finance.manager", period.ModifiedBy);
        Assert.NotNull(period.ModifiedAt);
    }
}
