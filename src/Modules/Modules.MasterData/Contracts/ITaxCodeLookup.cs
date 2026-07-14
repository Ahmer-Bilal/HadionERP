namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of a Tax Code another module (Finance) may depend on — same
/// Contracts-package rule as <see cref="IGLAccountLookup"/>. Finance needs the rate and active flag to
/// compute VAT on a document; it never maintains tax codes itself.
/// </summary>
public sealed record TaxCodeSummary(
    Guid Id,
    string TaxCodeCode,
    string TaxCodeName,
    decimal Rate,
    string TaxType,
    bool IsActive);

/// <summary>Read-only lookup Finance calls to resolve a tax code reference (e.g. on an AP Invoice) and
/// snapshot its rate at posting time. Implemented in Modules.MasterData.Infrastructure, registered in
/// Gateway.Api's DI container.</summary>
public interface ITaxCodeLookup
{
    Task<TaxCodeSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
