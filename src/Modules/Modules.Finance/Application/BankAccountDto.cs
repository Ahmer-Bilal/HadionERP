namespace Modules.Finance.Application;

public sealed record BankAccountDto(
    Guid Id,
    string? DocumentNumber,
    string Status,
    string AccountCode,
    string AccountName,
    string? AccountNameArabic,
    string BankName,
    string? Iban,
    Guid LinkedGLAccountId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record CreateBankAccountRequest(
    string AccountCode,
    string AccountName,
    string BankName,
    Guid LinkedGLAccountId,
    string? AccountNameArabic = null,
    string? Iban = null);

public sealed record UpdateBankAccountRequest(
    string AccountName,
    string BankName,
    string? AccountNameArabic = null,
    string? Iban = null,
    bool IsActive = true);
