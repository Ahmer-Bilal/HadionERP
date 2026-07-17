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

    void Add(Ipc ipc);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
