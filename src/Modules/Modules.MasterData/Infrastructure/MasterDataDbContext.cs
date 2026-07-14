using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.MasterData.Domain;
using Platform.Attachments;
using Platform.Core;
using Platform.Notes;
using Platform.Workflow;

namespace Modules.MasterData.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("masterdata") — physically enforcing the module-boundary rule
/// from docs/architecture/01-architecture-foundation.md #3.2 at the database level, not just in
/// application code (every module owns its own schema).
/// </summary>
public sealed class MasterDataDbContext : DbContext
{
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();
    public DbSet<NumberRangeCounterEntity> NumberRangeCounters => Set<NumberRangeCounterEntity>();
    // WorkflowInstance is a Platform.Workflow kernel type, not this module's own — persisted here because
    // Platform.Workflow itself stays storage-agnostic (see IWorkflowInstanceRepository's doc comment) and
    // this is the only module with a real database so far. A running approval can span separate HTTP
    // requests (submit today, decision days later), so it must survive here, not stay in memory.
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    // AttachmentMetadata is a Platform.Attachments kernel type, same "storage-agnostic port, persisted by
    // the one module with a real database" reasoning as WorkflowInstance above. AttachmentContents is
    // internal on purpose — only EfAttachmentRepository ever queries it directly.
    public DbSet<AttachmentMetadata> Attachments => Set<AttachmentMetadata>();
    internal DbSet<AttachmentContentRow> AttachmentContents => Set<AttachmentContentRow>();
    // Note is a Platform.Notes kernel type, same reasoning as WorkflowInstance/AttachmentMetadata above.
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<GLAccount> GLAccounts => Set<GLAccount>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();

    public MasterDataDbContext(DbContextOptions<MasterDataDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("masterdata");

        modelBuilder.Entity<BusinessPartner>(entity =>
        {
            entity.ToTable("business_partners");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.DocumentNumber).HasColumnName("doc_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            // RowVersion (Platform.Core) only increments inside Transition() — a lifecycle-transition
            // counter, not "has this row changed at all" (a plain field edit never touches it). Postgres's
            // own xmin system column tracks every write regardless of which property changed, so THAT is
            // the real optimistic-concurrency token, not this column — found via an integration test that
            // proved two concurrent field edits weren't actually conflicting.
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameArabic).HasColumnName("name_arabic").HasMaxLength(200);
            entity.Property(e => e.PartnerType).HasColumnName("partner_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TaxRegistrationNumber).HasColumnName("tax_registration_number").HasMaxLength(50);

            // ExtensionFieldBag doesn't expose its internals — it exists specifically so extensions can
            // add fields without a schema migration (docs/architecture/04-data-and-api.md #1.3) — so it's
            // persisted as its own JSON serialization, not decomposed into columns.
            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            // Addresses/Contacts: child entities (docs/architecture/02-business-object-model.md #1's
            // "Lines/Items" pattern), mapped via their private backing fields since the public properties
            // are read-only (the aggregate root — BusinessPartner — controls adding/removing them, callers
            // never manipulate the collections directly).
            entity.HasMany(e => e.Addresses)
                .WithOne()
                .HasForeignKey("BusinessPartnerId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Addresses)
                .HasField("_addresses")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.Contacts)
                .WithOne()
                .HasForeignKey("BusinessPartnerId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Contacts)
                .HasField("_contacts")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Not persisted in this first slice: DomainEvents is transient by definition (drained and
            // published after save, never stored itself); Relations (typed links to other BOs) has no
            // real use yet since BusinessPartner doesn't reference other Business Objects — add a proper
            // mapping when a module actually needs it rather than guessing its shape now.
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<BusinessPartnerAddress>(entity =>
        {
            entity.ToTable("business_partner_addresses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AddressType).HasColumnName("address_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.AddressLine).HasColumnName("address_line").HasMaxLength(300);
            entity.Property<Guid>("BusinessPartnerId").HasColumnName("business_partner_id");
        });

        modelBuilder.Entity<BusinessPartnerContact>(entity =>
        {
            entity.ToTable("business_partner_contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.JobTitle).HasColumnName("job_title").HasMaxLength(150);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property<Guid>("BusinessPartnerId").HasColumnName("business_partner_id");
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

            // ApplicableSteps/History/the two approver dictionaries have no natural relational shape worth
            // a real child table for (they're read as a whole, on the one instance that's currently
            // active, never queried across instances) — stored as jsonb, the same choice already made for
            // BusinessPartner.ExtensionFields. Each gets an explicit ValueComparer (unlike ExtensionFields)
            // because WorkflowInstance mutates these IN PLACE across a load -> Decide() -> SaveChanges
            // unit of work — without one, EF's default reference-equality comparer for converted
            // reference types can't tell a mutated collection changed, and would silently skip writing a
            // real approval decision back to the database.
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

            // Transient/computed — never persisted: DomainEvents (drained after save, same as
            // BusinessObject's), and CurrentStep/RequiredApproversForCurrentStep/History, which are all
            // derived from the fields/properties already mapped above.
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.CurrentStep);
            entity.Ignore(e => e.RequiredApproversForCurrentStep);
            entity.Ignore(e => e.History);
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

        // No CLR navigation either direction (AttachmentContentRow is internal — nothing outside
        // EfAttachmentRepository should ever join to it), so the relationship is configured purely via
        // Fluent API: AttachmentContentRow.AttachmentId is both its own primary key and the foreign key
        // into attachments.Id, cascade-deleted so removing an attachment's metadata also removes its bytes.
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

        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.BusinessObjectType).HasColumnName("business_object_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.BusinessObjectId).HasColumnName("business_object_id");
            entity.Property(e => e.Text).HasColumnName("text").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => new { e.BusinessObjectType, e.BusinessObjectId });
        });

        modelBuilder.Entity<NumberRangeCounterEntity>(entity =>
        {
            entity.ToTable("number_range_counters");
            entity.HasKey(e => new { e.RangeKey, e.CompanyId, e.FiscalYear });

            // Explicit snake_case column names, matching business_partners — without these, EF's default
            // Npgsql convention keeps the PascalCase C# property names verbatim, which silently mismatched
            // the snake_case column names EfCoreNumberRangeService's raw SQL assumed (caught by an
            // integration test running against the real database, not by unit tests alone).
            entity.Property(e => e.RangeKey).HasColumnName("range_key");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.FiscalYear).HasColumnName("fiscal_year");
            entity.Property(e => e.LastSequence).HasColumnName("last_sequence");
        });

        modelBuilder.Entity<GLAccount>(entity =>
        {
            entity.ToTable("gl_accounts");
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
            entity.Property(e => e.AccountType).HasColumnName("account_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ParentAccountId).HasColumnName("parent_account_id");
            entity.Property(e => e.IsPostable).HasColumnName("is_postable");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            // Self-referencing hierarchy: an account's optional parent is another GLAccount. Null for
            // top-level accounts. No cascade — deleting a parent with children would orphan them, which
            // the platform's "no hard delete past Draft" rule already prevents at the app layer.
            entity.HasOne<GLAccount>()
                .WithMany()
                .HasForeignKey(e => e.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.NormalBalance);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("items");
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

            entity.Property(e => e.ItemCode).HasColumnName("item_code").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.ItemCode).IsUnique();
            entity.Property(e => e.ItemName).HasColumnName("item_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ItemNameArabic).HasColumnName("item_name_arabic").HasMaxLength(200);
            entity.Property(e => e.ItemType).HasColumnName("item_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<CostCenter>(entity =>
        {
            entity.ToTable("cost_centers");
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

            entity.Property(e => e.CostCenterCode).HasColumnName("cost_center_code").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.CostCenterCode).IsUnique();
            entity.Property(e => e.CostCenterName).HasColumnName("cost_center_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CostCenterNameArabic).HasColumnName("cost_center_name_arabic").HasMaxLength(200);
            entity.Property(e => e.ParentCostCenterId).HasColumnName("parent_cost_center_id");
            entity.Property(e => e.IsPostable).HasColumnName("is_postable");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            // Self-referencing hierarchy: same "no cascade, app layer prevents hard delete past Draft"
            // reasoning as GLAccount's ParentAccountId.
            entity.HasOne<CostCenter>()
                .WithMany()
                .HasForeignKey(e => e.ParentCostCenterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });
    }

    // WorkflowInstance's jsonb conversions (see OnModelCreating above) — a bare JsonSerializer call,
    // reused four times, not a speculative abstraction over something that might vary later.
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
