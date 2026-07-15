using Microsoft.EntityFrameworkCore;
using Modules.Finance.Contracts;

namespace Modules.Finance.Infrastructure;

/// <summary>Implements the published <see cref="IAPInvoiceLookup"/> contract — same "project straight off
/// the module's own DbContext" pattern as Modules.MasterData's Ef*Lookup classes.</summary>
public sealed class EfAPInvoiceLookup : IAPInvoiceLookup
{
    private readonly FinanceDbContext _dbContext;

    public EfAPInvoiceLookup(FinanceDbContext dbContext) => _dbContext = dbContext;

    public async Task<APInvoiceSummary?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _dbContext.APInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return invoice is null
            ? null
            : new APInvoiceSummary(invoice.Id, invoice.DocumentNumber, invoice.VendorId, invoice.Status.ToString(), invoice.NetAmount, invoice.GrossAmount);
    }
}
