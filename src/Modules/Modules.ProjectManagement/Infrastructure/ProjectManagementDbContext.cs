using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.ProjectManagement.Domain;
using Platform.Core;
using Platform.Workflow;

namespace Modules.ProjectManagement.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("projectmanagement") — physically enforcing the module-boundary rule
/// from docs/architecture/01-overview.md §3.2 at the database level, same as every other
/// module's own schema. Owns its own copies of the generic kernel-port implementations (WorkflowInstance
/// persistence, number range counters) rather than sharing another module's tables.
/// </summary>
public sealed class ProjectManagementDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();
    internal DbSet<WbsElement> WbsElements => Set<WbsElement>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<NumberRangeCounterEntity> NumberRangeCounters => Set<NumberRangeCounterEntity>();

    public ProjectManagementDbContext(DbContextOptions<ProjectManagementDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("projectmanagement");

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
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

            entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProjectNameArabic).HasColumnName("project_name_arabic").HasMaxLength(200);
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.WbsElements)
                .WithOne()
                .HasForeignKey("ProjectId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.WbsElements)
                .HasField("_wbsElements")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<WbsElement>(entity =>
        {
            entity.ToTable("wbs_elements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasColumnName("name_arabic").HasMaxLength(200);
            entity.Property(e => e.ParentWbsElementId).HasColumnName("parent_wbs_element_id");
            entity.Property(e => e.IsPlanningElement).HasColumnName("is_planning_element");
            entity.Property(e => e.IsAccountAssignmentElement).HasColumnName("is_account_assignment_element");
            entity.Property(e => e.IsBillingElement).HasColumnName("is_billing_element");
            entity.Property<Guid>("ProjectId").HasColumnName("project_id");
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

            // Same jsonb + explicit ValueComparer treatment as every other module's identical WorkflowInstance
            // mapping — see FinanceDbContext's block for the full explanation.
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
