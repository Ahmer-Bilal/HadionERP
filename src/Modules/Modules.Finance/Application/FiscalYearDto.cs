namespace Modules.Finance.Application;

public sealed record FiscalPeriodDto(
    Guid Id,
    int PeriodNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsOpen,
    DateOnly TargetCloseDate);

public sealed record FiscalYearDto(
    Guid Id,
    int Year,
    IReadOnlyList<FiscalPeriodDto> Periods,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateFiscalYearRequest(int Year);
