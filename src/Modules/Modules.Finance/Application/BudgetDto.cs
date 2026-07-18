namespace Modules.Finance.Application;

public sealed record BudgetDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    Guid CostCenterId,
    int FiscalYear,
    decimal Amount,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateBudgetRequest(
    Guid CostCenterId,
    int FiscalYear,
    decimal Amount);
