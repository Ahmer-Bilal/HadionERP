using Modules.Construction.Domain;

namespace Modules.Construction.Application;

public interface IMeasurementSheetRepository
{
    Task<MeasurementSheet?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeasurementSheet>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Every sheet against the same commercial document, regardless of status — used to compute
    /// cumulative certified-to-date quantity per line and enforce the over-measurement guard
    /// (construction-commercial-processes-spec.md §2), a cross-aggregate check no single sheet can answer
    /// on its own.</summary>
    Task<IReadOnlyList<MeasurementSheet>> ListByCommercialDocumentAsync(
        CommercialDocumentType commercialDocumentType, Guid commercialDocumentId, CancellationToken cancellationToken = default);

    void Add(MeasurementSheet measurementSheet);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
