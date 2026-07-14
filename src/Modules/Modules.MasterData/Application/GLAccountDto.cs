namespace Modules.MasterData.Application;

public sealed record GLAccountDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string AccountCode,
    string AccountName,
    string? AccountNameArabic,
    string AccountType,
    string NormalBalance,
    Guid? ParentAccountId,
    bool IsPostable,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateGLAccountRequest(
    string AccountCode,
    string AccountName,
    string AccountType,
    string? AccountNameArabic = null,
    Guid? ParentAccountId = null,
    bool IsPostable = true);

public sealed record UpdateGLAccountRequest(
    string AccountName,
    string? AccountNameArabic = null,
    Guid? ParentAccountId = null,
    bool IsPostable = true,
    bool IsActive = true);
