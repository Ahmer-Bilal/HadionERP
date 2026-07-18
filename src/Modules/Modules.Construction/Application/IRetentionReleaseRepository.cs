using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface IRetentionReleaseRepository
{
    Task<RetentionRelease?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionRelease>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every release ever raised against this commercial document, regardless of status — the
    /// caller (<c>RetentionReleaseService</c>) filters to Approved when computing the running balance, the
    /// same "load all, filter by status in the service" pattern <c>IIpcRepository.ListByCommercialDocumentAsync</c>
    /// already establishes.</summary>
    Task<IReadOnlyList<RetentionRelease>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default);

    void Add(RetentionRelease retentionRelease);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
