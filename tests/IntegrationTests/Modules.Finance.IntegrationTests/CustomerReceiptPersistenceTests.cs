using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class CustomerReceiptPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly ReceiptDate = new(2026, 7, 15);

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_customer_receipt_with_allocations_reads_back_with_its_child_lines_and_computed_amount()
    {
        Guid id;
        var customerId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var invoiceOneId = Guid.NewGuid();
        var invoiceTwoId = Guid.NewGuid();
        var journalEntryId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var receipt = new CustomerReceipt("ahmer.bilal", customerId, bankAccountId, ReceiptDate, "BankTransfer", "First installment");
            receipt.AddAllocation(invoiceOneId, 600m);
            receipt.AddAllocation(invoiceTwoId, 400m);
            receipt.LinkJournalEntry(journalEntryId);
            receipt.AssignNumber("FIN-CR-2026-000001");
            writeContext.CustomerReceipts.Add(receipt);
            await writeContext.SaveChangesAsync();
            id = receipt.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.CustomerReceipts
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("FIN-CR-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(customerId, reloaded.CustomerId);
        Assert.Equal(bankAccountId, reloaded.BankAccountId);
        Assert.Equal("BankTransfer", reloaded.PaymentMethod);
        Assert.Equal(1000m, reloaded.Amount);
        Assert.Equal(2, reloaded.Allocations.Count);
        Assert.Equal(journalEntryId, reloaded.LinkedJournalEntryId);
    }

    [Fact]
    public async Task Submit_approve_post_reverse_persist_each_status_transition()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var receipt = new CustomerReceipt("ahmer.bilal", Guid.NewGuid(), Guid.NewGuid(), ReceiptDate, "Check", null);
            receipt.AddAllocation(Guid.NewGuid(), 500m);
            receipt.AssignNumber("FIN-CR-2026-000002");
            writeContext.CustomerReceipts.Add(receipt);
            await writeContext.SaveChangesAsync();
            receipt.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            receipt.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            receipt.LinkJournalEntry(Guid.NewGuid());
            receipt.Post("finance.manager");
            await writeContext.SaveChangesAsync();
            receipt.Reverse("finance.manager");
            await writeContext.SaveChangesAsync();
            id = receipt.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.CustomerReceipts.FirstAsync(r => r.Id == id);
        Assert.Equal(BusinessObjectStatus.Reversed, reloaded.Status);
        Assert.Equal(4, reloaded.RowVersion); // Submit + Approve + Post + Reverse
    }

    [Fact]
    public async Task Deleting_a_customer_receipt_cascades_to_its_allocations()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var receipt = new CustomerReceipt("ahmer.bilal", Guid.NewGuid(), Guid.NewGuid(), ReceiptDate, "BankTransfer", null);
            receipt.AddAllocation(Guid.NewGuid(), 250m);
            receipt.AssignNumber("FIN-CR-2026-000003");
            writeContext.CustomerReceipts.Add(receipt);
            await writeContext.SaveChangesAsync();
            id = receipt.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var receipt = await deleteContext.CustomerReceipts.Include(r => r.Allocations).FirstAsync(r => r.Id == id);
            deleteContext.CustomerReceipts.Remove(receipt);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingAllocations = await readContext.Set<CustomerReceiptAllocation>().CountAsync();
        Assert.Equal(0, remainingAllocations);
    }
}
