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
/// docs/architecture/01-overview.md §3.2 at the database level, same as Modules.MasterData's
/// "masterdata" schema and Modules.Finance's "finance" schema. Owns its own copies of the generic
/// kernel-port implementations (WorkflowInstance persistence, number range counters, attachments) rather
/// than sharing another module's tables — see <see cref="NumberRangeCounterEntity"/>'s doc comment for why.
/// </summary>
public sealed class ProcurementDbContext : DbContext
{
    public DbSet<VendorPrequalification> VendorPrequalifications => Set<VendorPrequalification>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    internal DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<RequestForQuotation> RequestsForQuotation => Set<RequestForQuotation>();
    internal DbSet<RfqLine> RfqLines => Set<RfqLine>();
    internal DbSet<RfqInvitedVendor> RfqInvitedVendors => Set<RfqInvitedVendor>();
    internal DbSet<RfqVendorQuoteLine> RfqVendorQuoteLines => Set<RfqVendorQuoteLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    internal DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    internal DbSet<GrnLine> GrnLines => Set<GrnLine>();
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

        modelBuilder.Entity<PurchaseRequisition>(entity =>
        {
            entity.ToTable("purchase_requisitions");
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

            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequiredByDate).HasColumnName("required_by_date");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("PurchaseRequisitionId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.EstimatedTotal);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<PurchaseRequisitionLine>(entity =>
        {
            entity.ToTable("purchase_requisition_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
            entity.Property(e => e.EstimatedUnitPrice).HasColumnName("estimated_unit_price").HasColumnType("numeric(18,2)");
            entity.Property(e => e.LineDescription).HasColumnName("line_description").HasMaxLength(500);
            entity.Property<Guid>("PurchaseRequisitionId").HasColumnName("purchase_requisition_id");
            entity.Ignore(e => e.EstimatedLineTotal);
        });

        modelBuilder.Entity<RequestForQuotation>(entity =>
        {
            entity.ToTable("requests_for_quotation");
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

            entity.Property(e => e.PurchaseRequisitionId).HasColumnName("purchase_requisition_id");
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.ResponseDeadline).HasColumnName("response_deadline");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("RequestForQuotationId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.InvitedVendors)
                .WithOne()
                .HasForeignKey("RequestForQuotationId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.InvitedVendors)
                .HasField("_invitedVendors")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.VendorQuoteLines)
                .WithOne()
                .HasForeignKey("RequestForQuotationId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.VendorQuoteLines)
                .HasField("_vendorQuoteLines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<RfqLine>(entity =>
        {
            entity.ToTable("rfq_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PurchaseRequisitionLineId).HasColumnName("purchase_requisition_line_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
            entity.Property<Guid>("RequestForQuotationId").HasColumnName("request_for_quotation_id");
        });

        modelBuilder.Entity<RfqInvitedVendor>(entity =>
        {
            entity.ToTable("rfq_invited_vendors");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property<Guid>("RequestForQuotationId").HasColumnName("request_for_quotation_id");
        });

        modelBuilder.Entity<RfqVendorQuoteLine>(entity =>
        {
            entity.ToTable("rfq_vendor_quote_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.RfqLineId).HasColumnName("rfq_line_id");
            entity.Property(e => e.QuotedUnitPrice).HasColumnName("quoted_unit_price").HasColumnType("numeric(18,2)");
            entity.Property<Guid>("RequestForQuotationId").HasColumnName("request_for_quotation_id");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
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
            entity.Property(e => e.RequestForQuotationId).HasColumnName("request_for_quotation_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("PurchaseOrderId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.Total);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)");
            entity.Property(e => e.RfqLineId).HasColumnName("rfq_line_id");
            entity.Property<Guid>("PurchaseOrderId").HasColumnName("purchase_order_id");
            entity.Ignore(e => e.LineTotal);
        });

        modelBuilder.Entity<GoodsReceiptNote>(entity =>
        {
            entity.ToTable("goods_receipt_notes");
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

            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("GoodsReceiptNoteId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.ReceivedValue);
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<GrnLine>(entity =>
        {
            entity.ToTable("grn_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.PurchaseOrderLineId).HasColumnName("purchase_order_line_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.QuantityReceived).HasColumnName("quantity_received").HasColumnType("numeric(18,3)");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(18,2)");
            entity.Property<Guid>("GoodsReceiptNoteId").HasColumnName("goods_receipt_note_id");
            entity.Ignore(e => e.LineValue);
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
