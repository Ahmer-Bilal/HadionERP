using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.Finance.Domain;
using Platform.Core;
using Platform.Workflow;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("finance") — physically enforcing the module-boundary rule from
/// docs/architecture/01-overview.md §3.2 at the database level, same as
/// Modules.MasterData's own "masterdata" schema. Owns its own copies of the generic kernel-port
/// implementations (WorkflowInstance persistence, number range counters) rather than sharing MasterData's
/// tables — see <see cref="NumberRangeCounterEntity"/>'s doc comment for why.
/// </summary>
public sealed class FinanceDbContext : DbContext
{
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    internal DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<APInvoice> APInvoices => Set<APInvoice>();
    public DbSet<ARInvoice> ARInvoices => Set<ARInvoice>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Payment> Payments => Set<Payment>();
    internal DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<CustomerReceipt> CustomerReceipts => Set<CustomerReceipt>();
    internal DbSet<CustomerReceiptAllocation> CustomerReceiptAllocations => Set<CustomerReceiptAllocation>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    internal DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<ClosingActivity> ClosingActivities => Set<ClosingActivity>();
    internal DbSet<ClosingActivityStep> ClosingActivitySteps => Set<ClosingActivityStep>();
    // WorkflowInstance is a Platform.Workflow kernel type, persisted here for the same "storage-agnostic
    // port, persisted by whichever module actually has a database" reasoning as
    // Modules.MasterData.Infrastructure.MasterDataDbContext — but in Finance's own schema/table, not
    // MasterData's, since a running Journal Entry approval must survive independently of MasterData.
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<NumberRangeCounterEntity> NumberRangeCounters => Set<NumberRangeCounterEntity>();

    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("finance");

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.PostingDate).HasColumnName("posting_date");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReversalOfEntryId).HasColumnName("reversal_of_entry_id");
            entity.Property(e => e.SourceDocumentType).HasColumnName("source_document_type").HasMaxLength(50);
            entity.Property(e => e.SourceDocumentId).HasColumnName("source_document_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            // 0..n child collection, same pattern as Modules.MasterData's BusinessPartner.Addresses —
            // mapped via the private backing field since the public Lines property is read-only (the
            // aggregate root controls adding lines, callers never manipulate the collection directly).
            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.TotalDebits);
            entity.Ignore(e => e.TotalCredits);
            entity.Ignore(e => e.IsBalanced);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<JournalLine>(entity =>
        {
            entity.ToTable("journal_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.GLAccountId).HasColumnName("gl_account_id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.DebitAmount).HasColumnName("debit_amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.CreditAmount).HasColumnName("credit_amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.LineDescription).HasColumnName("line_description").HasMaxLength(500);
            entity.Property<Guid>("JournalEntryId").HasColumnName("journal_entry_id");
        });

        modelBuilder.Entity<APInvoice>(entity =>
        {
            entity.ToTable("ap_invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.VendorInvoiceNumber).HasColumnName("vendor_invoice_number").HasMaxLength(100).IsRequired();
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ExpenseAccountId).HasColumnName("expense_account_id");
            entity.Property(e => e.PayableAccountId).HasColumnName("payable_account_id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.TaxCodeId).HasColumnName("tax_code_id");
            entity.Property(e => e.TaxRate).HasColumnName("tax_rate").HasColumnType("numeric(5,2)");
            entity.Property(e => e.VatAccountId).HasColumnName("vat_account_id");
            entity.Property(e => e.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.LinkedJournalEntryId).HasColumnName("linked_journal_entry_id");
            entity.Property(e => e.SourceDocumentType).HasColumnName("source_document_type").HasMaxLength(50);
            entity.Property(e => e.SourceDocumentId).HasColumnName("source_document_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.TaxAmount);
            entity.Ignore(e => e.GrossAmount);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<ARInvoice>(entity =>
        {
            entity.ToTable("ar_invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerReference).HasColumnName("customer_reference").HasMaxLength(100);
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.RevenueAccountId).HasColumnName("revenue_account_id");
            entity.Property(e => e.ReceivableAccountId).HasColumnName("receivable_account_id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.TaxCodeId).HasColumnName("tax_code_id");
            entity.Property(e => e.TaxRate).HasColumnName("tax_rate").HasColumnType("numeric(5,2)");
            entity.Property(e => e.VatAccountId).HasColumnName("vat_account_id");
            entity.Property(e => e.NetAmount).HasColumnName("net_amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.LinkedJournalEntryId).HasColumnName("linked_journal_entry_id");
            entity.Property(e => e.SourceDocumentType).HasColumnName("source_document_type").HasMaxLength(50);
            entity.Property(e => e.SourceDocumentId).HasColumnName("source_document_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.TaxAmount);
            entity.Ignore(e => e.GrossAmount);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("bank_accounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.AccountCode).HasColumnName("account_code").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.AccountCode).IsUnique();
            entity.Property(e => e.AccountName).HasColumnName("account_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.AccountNameArabic).HasColumnName("account_name_arabic").HasMaxLength(200);
            entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Iban).HasColumnName("iban").HasMaxLength(50);
            entity.Property(e => e.LinkedGLAccountId).HasColumnName("linked_gl_account_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.BankAccountId).HasColumnName("bank_account_id");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reference).HasColumnName("reference").HasMaxLength(200);
            entity.Property(e => e.LinkedJournalEntryId).HasColumnName("linked_journal_entry_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Allocations)
                .WithOne()
                .HasForeignKey("PaymentId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Allocations)
                .HasField("_allocations")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.Amount);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.ToTable("payment_allocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.APInvoiceId).HasColumnName("ap_invoice_id");
            entity.Property(e => e.AllocatedAmount).HasColumnName("allocated_amount").HasColumnType("numeric(18,2)");
            entity.Property<Guid>("PaymentId").HasColumnName("payment_id");
        });

        modelBuilder.Entity<CustomerReceipt>(entity =>
        {
            entity.ToTable("customer_receipts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.BankAccountId).HasColumnName("bank_account_id");
            entity.Property(e => e.ReceiptDate).HasColumnName("receipt_date");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reference).HasColumnName("reference").HasMaxLength(200);
            entity.Property(e => e.LinkedJournalEntryId).HasColumnName("linked_journal_entry_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Allocations)
                .WithOne()
                .HasForeignKey("CustomerReceiptId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Allocations)
                .HasField("_allocations")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.Amount);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<CustomerReceiptAllocation>(entity =>
        {
            entity.ToTable("customer_receipt_allocations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ARInvoiceId).HasColumnName("ar_invoice_id");
            entity.Property(e => e.AllocatedAmount).HasColumnName("allocated_amount").HasColumnType("numeric(18,2)");
            entity.Property<Guid>("CustomerReceiptId").HasColumnName("customer_receipt_id");
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.ToTable("budgets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<FiscalYear>(entity =>
        {
            entity.ToTable("fiscal_years");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Year).HasColumnName("year");
            entity.HasIndex(e => e.Year).IsUnique();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // 0..n child collection, always exactly 12 (auto-generated at construction) — same "owned
            // through the aggregate root, mapped via the private backing field" pattern as
            // JournalEntry.Lines above.
            entity.HasMany(e => e.Periods)
                .WithOne()
                .HasForeignKey("FiscalYearId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Periods)
                .HasField("_periods")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FiscalPeriod>(entity =>
        {
            entity.ToTable("fiscal_periods");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PeriodNumber).HasColumnName("period_number");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsOpen).HasColumnName("is_open");
            entity.Property(e => e.TargetCloseDate).HasColumnName("target_close_date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
            entity.Property<Guid>("FiscalYearId").HasColumnName("fiscal_year_id");

            // Every posting-time period lookup (JournalEntryService) queries this table directly by date
            // range, never through a loaded FiscalYear aggregate — an index on the range keeps that check
            // cheap even once several years' worth of periods exist.
            entity.HasIndex(e => new { e.StartDate, e.EndDate });
        });

        modelBuilder.Entity<ClosingActivity>(entity =>
        {
            entity.ToTable("closing_activities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FiscalPeriodId).HasColumnName("fiscal_period_id");
            entity.Property(e => e.ActivityKey).HasColumnName("activity_key").HasMaxLength(50).IsRequired();
            entity.Property(e => e.SequenceNumber).HasColumnName("sequence_number");
            entity.Property(e => e.AssignedToUserId).HasColumnName("assigned_to_user_id");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.LastActionBy).HasColumnName("last_action_by").HasMaxLength(100);
            entity.Property(e => e.LastActionAt).HasColumnName("last_action_at");
            entity.HasIndex(e => e.FiscalPeriodId);

            // Real FK to FiscalPeriod (not just a loose Guid) so deleting a period — and transitively a
            // whole FiscalYear, via that entity's own cascade above — cascades to this activity and, in
            // turn, its own Steps below, rather than leaving orphaned rows behind.
            entity.HasOne<FiscalPeriod>()
                .WithMany()
                .HasForeignKey(e => e.FiscalPeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Steps)
                .WithOne()
                .HasForeignKey("ClosingActivityId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Steps)
                .HasField("_steps")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ClosingActivityStep>(entity =>
        {
            entity.ToTable("closing_activity_steps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.LinkedDocumentType).HasColumnName("linked_document_type").HasMaxLength(50);
            entity.Property(e => e.LinkedDocumentId).HasColumnName("linked_document_id");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by").HasMaxLength(100);
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property<Guid>("ClosingActivityId").HasColumnName("closing_activity_id");

            entity.Ignore(e => e.IsAutoTracked);
        });

        modelBuilder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("workflow_instances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DefinitionKey).HasColumnName("definition_key").HasMaxLength(200).IsRequired();
            entity.Property(e => e.BusinessObjectId).HasColumnName("business_object_id");
            entity.Property(e => e.BusinessObjectType).HasColumnName("business_object_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CurrentStepIndex).HasColumnName("current_step_index");
            entity.Property(e => e.CurrentStepStartedAt).HasColumnName("current_step_started_at");

            // Same jsonb + explicit ValueComparer treatment as Modules.MasterData's own WorkflowInstance
            // mapping, and for the same reason — see MasterDataDbContext's identical block for the full
            // explanation (WorkflowInstance mutates these in place across a load -> Decide() -> SaveChanges
            // unit of work).
            entity.Property(e => e.ApplicableSteps)
                .HasColumnName("applicable_steps")
                .HasColumnType("jsonb")
                .HasConversion(
                    steps => ToJson(steps),
                    json => (IReadOnlyList<WorkflowStepDefinition>)FromJson<List<WorkflowStepDefinition>>(json))
                .Metadata.SetValueComparer(ApplicableStepsComparer);

            entity.Property<List<WorkflowStepDecisionRecord>>("_history")
                .HasColumnName("history")
                .HasColumnType("jsonb")
                .HasConversion(history => ToJson(history), json => FromJson<List<WorkflowStepDecisionRecord>>(json))
                .Metadata.SetValueComparer(HistoryComparer);

            entity.Property<Dictionary<string, HashSet<string>>>("_approvedByStep")
                .HasColumnName("approved_by_step")
                .HasColumnType("jsonb")
                .HasConversion(dict => ToJson(dict), json => FromJson<Dictionary<string, HashSet<string>>>(json))
                .Metadata.SetValueComparer(StringSetDictionaryComparer);

            entity.Property<Dictionary<string, HashSet<string>>>("_requiredApproversByStep")
                .HasColumnName("required_approvers_by_step")
                .HasColumnType("jsonb")
                .HasConversion(dict => ToJson(dict), json => FromJson<Dictionary<string, HashSet<string>>>(json))
                .Metadata.SetValueComparer(StringSetDictionaryComparer);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.CurrentStep);
            entity.Ignore(e => e.RequiredApproversForCurrentStep);
            entity.Ignore(e => e.History);
        });

        modelBuilder.Entity<NumberRangeCounterEntity>(entity =>
        {
            entity.ToTable("number_range_counters");
            entity.HasKey(e => new { e.RangeKey, e.CompanyId, e.FiscalYear });
            entity.Property(e => e.RangeKey).HasColumnName("range_key");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(e => e.LastSequence).HasColumnName("last_sequence");
        });
    }

    private static string ToJson<T>(T value) => JsonSerializer.Serialize(value);

    private static T FromJson<T>(string json) where T : new() => JsonSerializer.Deserialize<T>(json) ?? new T();

    private static readonly ValueComparer<IReadOnlyList<WorkflowStepDefinition>> ApplicableStepsComparer = new(
        (a, b) => ToJson(a) == ToJson(b),
        v => ToJson(v).GetHashCode(),
        v => FromJson<List<WorkflowStepDefinition>>(ToJson(v)));

    private static readonly ValueComparer<List<WorkflowStepDecisionRecord>> HistoryComparer = new(
        (a, b) => ToJson(a) == ToJson(b),
        v => ToJson(v).GetHashCode(),
        v => FromJson<List<WorkflowStepDecisionRecord>>(ToJson(v)));

    private static readonly ValueComparer<Dictionary<string, HashSet<string>>> StringSetDictionaryComparer = new(
        (a, b) => ToJson(a) == ToJson(b),
        v => ToJson(v).GetHashCode(),
        v => FromJson<Dictionary<string, HashSet<string>>>(ToJson(v)));
}
