namespace Modules.MasterData.Contracts;

/// <summary>
/// The published, read-only view of a Business Partner another module (Finance) may depend on — same
/// Contracts-package rule as <see cref="IGLAccountLookup"/>. Finance needs to know "does this vendor exist,
/// is it approved and active," not Business Partner's own maintenance concerns (addresses, contacts,
/// attachments).
/// </summary>
public sealed record BusinessPartnerSummary(
    Guid Id,
    string Name,
    string? NameArabic,
    string PartnerType,
    string Status);

/// <summary>Read-only lookup Finance calls to validate a vendor reference (e.g. on an AP Invoice) before
/// posting. Implemented in Modules.MasterData.Infrastructure, registered in Gateway.Api's DI container.</summary>
public interface IBusinessPartnerLookup
{
    Task<BusinessPartnerSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
