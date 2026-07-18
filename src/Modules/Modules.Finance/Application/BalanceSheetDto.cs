namespace Modules.Finance.Application;

/// <summary>The Balance Sheet (Statement of Financial Position) as of one date, optionally compared against
/// another date. <see cref="TotalAssets"/> always equals <see cref="TotalLiabilities"/> +
/// <see cref="TotalEquity"/> — not by construction of this DTO, but because <see cref="EquityLines"/>
/// includes a computed "Retained Earnings (Undistributed)" line (see
/// <see cref="BalanceSheetService"/>'s own doc comment for why), which is what makes the fundamental
/// accounting equation hold even though there is no Period Closing Center yet to formally post that amount
/// into a real Retained Earnings G/L account.</summary>
public sealed record BalanceSheetDto(
    DateOnly AsOfDate,
    DateOnly? CompareAsOfDate,
    IReadOnlyList<StatementLineDto> AssetLines,
    decimal TotalAssets,
    decimal? CompareTotalAssets,
    IReadOnlyList<StatementLineDto> LiabilityLines,
    decimal TotalLiabilities,
    decimal? CompareTotalLiabilities,
    IReadOnlyList<StatementLineDto> EquityLines,
    decimal TotalEquity,
    decimal? CompareTotalEquity,
    decimal TotalLiabilitiesAndEquity,
    decimal? CompareTotalLiabilitiesAndEquity);
