using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.Procurement.Domain;
using Platform.Attachments;
using Platform.Core;
using Platform.Workflow;

namespace Modules.Procurement.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("procurement") — physically enforcing the module-boundary rule from
/// docs/architecture/01-architecture-foundation.md §3.2 at the database level, same as Modules.MasterData's
/// "masterdata" schema and Modules.Finance's "finance" schema. Owns its own copies of the generic
/// kernel-port implementations (WorkflowInstance persistence, number range counters, attachments) rather
/// than sharing another module's tables — see <see cref="NumberRangeCounterEntity"/>'s doc comment for why.
/// </summary>
public sealed class ProcurementDbContext : DbContext
{
    public DbSet<VendorPrequalification> VendorPrequalifications => Set<VendorPrequalification>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<NumberRangeCounterEntity> NumberRangeCounters => Set<NumberRangeCounterEntity>();
    public DbSet<AttachmentMetadata> Attachments => Set<AttachmentMetadata>();
    internal DbSet<AttachmentContentRow> AttachmentContents => Set<AttachmentContentRow>();

    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("procurement");

        modelBuilder.Entity<VendorPrequalification>(entity =>
        {
            entity.ToTable("vendor_prequalifications");
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

            entity.Property(e => e.BusinessPartnerId).HasColumnName("business_partner_id");
            entity.Property(e => e.RoleType).HasColumnName("role_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Trade).HasColumnName("trade").HasMaxLength(100);
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
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

            // Same jsonb + explicit ValueComparer treatment as Modules.Finance's/Modules.MasterData's own
            // WorkflowInstance mapping — see FinanceDbContext's identical block for the full explanation.
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

        modelBuilder.Entity<AttachmentMetadata>(entity =>
        {
            entity.ToTable("attachments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BusinessObjectType).HasColumnName("business_object_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.BusinessObjectId).HasColumnName("business_object_id");
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(150).IsRequired();
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
            entity.HasIndex(e => new { e.BusinessObjectType, e.BusinessObjectId });
        });

        modelBuilder.Entity<AttachmentContentRow>(entity =>
        {
            entity.ToTable("attachment_contents");
            entity.HasKey(e => e.AttachmentId);
            entity.Property(e => e.AttachmentId).HasColumnName("attachment_id").ValueGeneratedNever();
            entity.Property(e => e.Content).HasColumnName("content").HasColumnType("bytea").IsRequired();
            entity.HasOne<AttachmentMetadata>()
                .WithOne()
                .HasForeignKey<AttachmentContentRow>(e => e.AttachmentId)
                .OnDelete(DeleteBehavior.Cascade);
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
