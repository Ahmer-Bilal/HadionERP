namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of one admin-configurable Lookup value another module (Finance's
/// `PaymentService`, validating `PaymentMethod`) may depend on — same Contracts-package rule as
/// <see cref="IGLAccountLookup"/>. Built the moment a real cross-module consumer needed it, per
/// `Modules.MasterData/README.md`'s own Deferred note ("add `ILookupCatalog` the same way
/// `IBusinessPartnerLookup`/`IItemLookup` exist once a module outside MasterData actually needs to read a
/// lookup type") — not built speculatively ahead of a real need.
/// </summary>
public sealed record LookupValueSummary(string LookupTypeCode, string Code, string Name, string? NameArabic, bool IsActive);

/// <summary>Read-only lookup another module calls to validate a lookup-value reference (e.g. a Payment's
/// `PaymentMethod`) before accepting it. Implemented in Modules.MasterData.Infrastructure, registered in
/// Gateway.Api's DI container.</summary>
public interface ILookupCatalog
{
    Task<LookupValueSummary?> GetValueAsync(string lookupTypeCode, string valueCode, CancellationToken cancellationToken = default);
}
