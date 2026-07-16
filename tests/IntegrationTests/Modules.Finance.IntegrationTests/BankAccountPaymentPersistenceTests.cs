using Microsoft.EntityFrameworkCore;
using Modules.Finance.Domain;
using Platform.Core;
using Xunit;

namespace Modules.Finance.IntegrationTests;

public class BankAccountPaymentPersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 14);
    private static readonly DateOnly PaymentDate = new(2026, 7, 15);

    public Task InitializeAsync() => TestDatabase.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_saved_bank_account_reads_back_identically_through_a_fresh_DbContext()
    {
        Guid id;
        var linkedGLAccountId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var bankAccount = new BankAccount("ahmer.bilal", "BANK-001", "Al Rajhi Current Account", "Al Rajhi Bank", linkedGLAccountId);
            bankAccount.UpdateAccountNameArabic("حساب الراجحي الجاري");
            bankAccount.UpdateIban("SA0000000000000000000000");
            bankAccount.AssignNumber("FIN-BANK-2026-000001");
            writeContext.BankAccounts.Add(bankAccount);
            await writeContext.SaveChangesAsync();
            id = bankAccount.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.BankAccounts.FirstOrDefaultAsync(b => b.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("BANK-001", reloaded!.AccountCode);
        Assert.Equal("حساب الراجحي الجاري", reloaded.AccountNameArabic);
        Assert.Equal("SA0000000000000000000000", reloaded.Iban);
        Assert.Equal(linkedGLAccountId, reloaded.LinkedGLAccountId);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task Submit_approve_persist_the_new_status_and_row_version()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var bankAccount = new BankAccount("ahmer.bilal", "BANK-002", "Test Account", "SNB", Guid.NewGuid());
            bankAccount.AssignNumber("FIN-BANK-2026-000002");
            writeContext.BankAccounts.Add(bankAccount);
            await writeContext.SaveChangesAsync();
            bankAccount.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            bankAccount.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            id = bankAccount.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.BankAccounts.FirstAsync(b => b.Id == id);
        Assert.Equal(BusinessObjectStatus.Approved, reloaded.Status);
        Assert.Equal(2, reloaded.RowVersion); // Submit + Approve
    }

    [Fact]
    public async Task A_saved_payment_with_allocations_reads_back_with_its_child_lines_and_computed_amount()
    {
        Guid id;
        var vendorId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var invoiceOneId = Guid.NewGuid();
        var invoiceTwoId = Guid.NewGuid();
        var journalEntryId = Guid.NewGuid();

        await using (var writeContext = TestDatabase.CreateContext())
        {
            var payment = new Payment("ahmer.bilal", vendorId, bankAccountId, PaymentDate, "BankTransfer", "First installment");
            payment.AddAllocation(invoiceOneId, 600m);
            payment.AddAllocation(invoiceTwoId, 400m);
            payment.LinkJournalEntry(journalEntryId);
            payment.AssignNumber("FIN-PAY-2026-000001");
            writeContext.Payments.Add(payment);
            await writeContext.SaveChangesAsync();
            id = payment.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id);

        Assert.NotNull(reloaded);
        Assert.Equal("FIN-PAY-2026-000001", reloaded!.DocumentNumber);
        Assert.Equal(vendorId, reloaded.VendorId);
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
            var payment = new Payment("ahmer.bilal", Guid.NewGuid(), Guid.NewGuid(), PaymentDate, "Check", null);
            payment.AddAllocation(Guid.NewGuid(), 500m);
            payment.AssignNumber("FIN-PAY-2026-000002");
            writeContext.Payments.Add(payment);
            await writeContext.SaveChangesAsync();
            payment.Submit("ahmer.bilal");
            await writeContext.SaveChangesAsync();
            payment.Approve("finance.manager");
            await writeContext.SaveChangesAsync();
            payment.LinkJournalEntry(Guid.NewGuid());
            payment.Post("finance.manager");
            await writeContext.SaveChangesAsync();
            payment.Reverse("finance.manager");
            await writeContext.SaveChangesAsync();
            id = payment.Id;
        }

        await using var readContext = TestDatabase.CreateContext();
        var reloaded = await readContext.Payments.FirstAsync(p => p.Id == id);
        Assert.Equal(BusinessObjectStatus.Reversed, reloaded.Status);
        Assert.Equal(4, reloaded.RowVersion); // Submit + Approve + Post + Reverse
    }

    [Fact]
    public async Task Deleting_a_payment_cascades_to_its_allocations()
    {
        Guid id;
        await using (var writeContext = TestDatabase.CreateContext())
        {
            var payment = new Payment("ahmer.bilal", Guid.NewGuid(), Guid.NewGuid(), PaymentDate, "BankTransfer", null);
            payment.AddAllocation(Guid.NewGuid(), 250m);
            payment.AssignNumber("FIN-PAY-2026-000003");
            writeContext.Payments.Add(payment);
            await writeContext.SaveChangesAsync();
            id = payment.Id;
        }

        await using (var deleteContext = TestDatabase.CreateContext())
        {
            var payment = await deleteContext.Payments.Include(p => p.Allocations).FirstAsync(p => p.Id == id);
            deleteContext.Payments.Remove(payment);
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = TestDatabase.CreateContext();
        var remainingAllocations = await readContext.Set<PaymentAllocation>().CountAsync();
        Assert.Equal(0, remainingAllocations);
    }
}
