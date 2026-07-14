using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.Finance.Domain;
using Platform.Core;
using Platform.Workflow;

namespace Modules.Finance.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("finance") — physically enforcing the module-boundary rule from
/// docs/architecture/01-architecture-foundation.md §3.2 at the database level, same as
/// Modules.MasterData's own "masterdata" schema. Owns its own copies of the generic kernel-port
/// implementations (WorkflowInstance persistence, number range counters) rather than sharing MasterData's
/// tables — see <see cref="NumberRangeCounterEntity"/>'s doc comment for why.
/// </summary>
public sealed class FinanceDbContext : DbContext
{
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    internal DbSet<JournalLine> JournalLines => Set<JournalLine>();
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
