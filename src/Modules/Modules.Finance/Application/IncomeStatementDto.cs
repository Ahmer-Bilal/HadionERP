namespace Modules.Finance.Application;

/// <summary>The Income Statement (Profit &amp; Loss) for one period, optionally compared against another.
/// See <see cref="IncomeStatementService"/>'s own doc comment for how this is derived.</summary>
public sealed record IncomeStatementDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly? ComparePeriodStart,
    DateOnly? ComparePeriodEnd,
    IReadOnlyList<StatementLineDto> RevenueLines,
    decimal TotalRevenue,
    decimal? CompareTotalRevenue,
    IReadOnlyList<StatementLineDto> ExpenseLines,
    decimal TotalExpenses,
    decimal? CompareTotalExpenses,
    decimal NetProfit,
    decimal? CompareNetProfit);
