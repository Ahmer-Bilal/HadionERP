using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface IIpcRepository
{
    Task<Ipc?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ipc>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether an IPC already exists against this Measurement Sheet — enforces "one IPC per
    /// measured period" so the same certified quantity can never be billed twice.</summary>
    Task<bool> ExistsForMeasurementSheetAsync(Guid measurementSheetId, CancellationToken cancellationToken = default);

    /// <summary>Every IPC ever raised against this commercial document, regardless of status —
    /// <c>RetentionReleaseService</c> filters to Approved and sums each one's own <c>RetentionAmount</c> to
    /// compute the running "total retention withheld to date" balance (construction-commercial-processes-
    /// spec.md §5).</summary>
    Task<IReadOnlyList<Ipc>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default);

    void Add(Ipc ipc);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
