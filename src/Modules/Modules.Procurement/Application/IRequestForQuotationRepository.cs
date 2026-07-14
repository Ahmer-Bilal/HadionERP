using Modules.Procurement.Domain;

namespace Modules.Procurement.Application;

public interface IRequestForQuotationRepository
{
    Task<RequestForQuotation?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestForQuotation>> ListAsync(int skip, int top, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(RequestForQuotation rfq);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
