using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Modules.Construction.Domain;
using Platform.Core;
using Platform.Workflow;

namespace Modules.Construction.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("construction") — physically enforcing the module-boundary rule from
/// docs/architecture/01-overview.md §3.2 at the database level, same as every other module's
/// own schema. Owns its own copies of the generic kernel-port implementations (WorkflowInstance persistence,
/// number range counters) rather than sharing another module's tables.
/// </summary>
public sealed class ConstructionDbContext : DbContext
{
    public DbSet<Contract> Contracts => Set<Contract>();
    internal DbSet<BoqLine> BoqLines => Set<BoqLine>();
    public DbSet<Subcontract> Subcontracts => Set<Subcontract>();
    internal DbSet<SubcontractLine> SubcontractLines => Set<SubcontractLine>();
    internal DbSet<BackCharge> BackCharges => Set<BackCharge>();
    public DbSet<MeasurementSheet> MeasurementSheets => Set<MeasurementSheet>();
    internal DbSet<MeasurementLine> MeasurementLines => Set<MeasurementLine>();
    public DbSet<Ipc> Ipcs => Set<Ipc>();
    internal DbSet<IpcLine> IpcLines => Set<IpcLine>();
    public DbSet<VariationOrder> VariationOrders => Set<VariationOrder>();
    internal DbSet<VariationOrderLine> VariationOrderLines => Set<VariationOrderLine>();
    public DbSet<RetentionRelease> RetentionReleases => Set<RetentionRelease>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<NumberRangeCounterEntity> NumberRangeCounters => Set<NumberRangeCounterEntity>();

    public ConstructionDbContext(DbContextOptions<ConstructionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("construction");

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ContractType).HasColumnName("contract_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PaymentTerms).HasColumnName("payment_terms").HasMaxLength(500);
            entity.Property(e => e.AdvancePaymentPercentage).HasColumnName("advance_payment_percentage").HasColumnType("numeric(5,2)");
            entity.Property(e => e.RetentionPercentage).HasColumnName("retention_percentage").HasColumnType("numeric(5,2)");
            entity.Property(e => e.DefectsLiabilityPeriodMonths).HasColumnName("defects_liability_period_months");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.BoqLines)
                .WithOne()
                .HasForeignKey("ContractId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.BoqLines)
                .HasField("_boqLines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
            entity.Ignore(e => e.ContractValue);
        });

        modelBuilder.Entity<BoqLine>(entity =>
        {
            entity.ToTable("boq_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.DescriptionArabic).HasColumnName("description_arabic").HasMaxLength(500);
            entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
            entity.Property(e => e.Rate).HasColumnName("rate").HasColumnType("numeric(18,2)");
            entity.Property(e => e.WbsElementId).HasColumnName("wbs_element_id");
            entity.Property<Guid>("ContractId").HasColumnName("contract_id");

            entity.Ignore(e => e.Amount);
        });

        modelBuilder.Entity<Subcontract>(entity =>
        {
            entity.ToTable("subcontracts");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.SubcontractorId).HasColumnName("subcontractor_id");
            entity.Property(e => e.RetentionPercentage).HasColumnName("retention_percentage").HasColumnType("numeric(5,2)");
            entity.Property(e => e.MobilizationAdvancePercentage).HasColumnName("mobilization_advance_percentage").HasColumnType("numeric(5,2)");
            entity.Property(e => e.DefectsLiabilityPeriodMonths).HasColumnName("defects_liability_period_months");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("SubcontractId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.BackCharges)
                .WithOne()
                .HasForeignKey("SubcontractId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.BackCharges)
                .HasField("_backCharges")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
            entity.Ignore(e => e.SubcontractValue);
            entity.Ignore(e => e.TotalBackCharges);
            entity.Ignore(e => e.NetPayableValue);
        });

        modelBuilder.Entity<SubcontractLine>(entity =>
        {
            entity.ToTable("subcontract_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.DescriptionArabic).HasColumnName("description_arabic").HasMaxLength(500);
            entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("numeric(18,3)");
            entity.Property(e => e.Rate).HasColumnName("rate").HasColumnType("numeric(18,2)");
            entity.Property(e => e.WbsElementId).HasColumnName("wbs_element_id");
            entity.Property<Guid>("SubcontractId").HasColumnName("subcontract_id");

            entity.Ignore(e => e.Amount);
        });

        modelBuilder.Entity<BackCharge>(entity =>
        {
            entity.ToTable("back_charges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.DateIncurred).HasColumnName("date_incurred");
            entity.Property<Guid>("SubcontractId").HasColumnName("subcontract_id");
        });

        modelBuilder.Entity<MeasurementSheet>(entity =>
        {
            entity.ToTable("measurement_sheets");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CommercialDocumentType).HasColumnName("commercial_document_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CommercialDocumentId).HasColumnName("commercial_document_id");
            entity.Property(e => e.PeriodStart).HasColumnName("period_start");
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("MeasurementSheetId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
        });

        modelBuilder.Entity<MeasurementLine>(entity =>
        {
            entity.ToTable("measurement_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CommercialDocumentLineId).HasColumnName("commercial_document_line_id");
            entity.Property(e => e.QuantitySubmitted).HasColumnName("quantity_submitted").HasColumnType("numeric(18,3)");
            entity.Property(e => e.QuantityCertified).HasColumnName("quantity_certified").HasColumnType("numeric(18,3)");
            entity.Property(e => e.Remarks).HasColumnName("remarks").HasMaxLength(1000);
            entity.Property<Guid>("MeasurementSheetId").HasColumnName("measurement_sheet_id");
        });

        modelBuilder.Entity<Ipc>(entity =>
        {
            entity.ToTable("ipcs");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CommercialDocumentType).HasColumnName("commercial_document_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CommercialDocumentId).HasColumnName("commercial_document_id");
            entity.Property(e => e.MeasurementSheetId).HasColumnName("measurement_sheet_id");
            entity.Property(e => e.PeriodStart).HasColumnName("period_start");
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
            entity.Property(e => e.RetentionPercentageApplied).HasColumnName("retention_percentage_applied").HasColumnType("numeric(5,2)");
            entity.Property(e => e.AdvancePaymentPercentageApplied).HasColumnName("advance_payment_percentage_applied").HasColumnType("numeric(5,2)");
            entity.Property(e => e.OtherDeductions).HasColumnName("other_deductions").HasColumnType("numeric(18,2)");
            entity.Property(e => e.RevenueAccountId).HasColumnName("revenue_account_id");
            entity.Property(e => e.ReceivableAccountId).HasColumnName("receivable_account_id");
            entity.Property(e => e.TaxCodeId).HasColumnName("tax_code_id");
            entity.Property(e => e.VatAccountId).HasColumnName("vat_account_id");
            entity.Property(e => e.LinkedArInvoiceId).HasColumnName("linked_ar_invoice_id");
            entity.Property(e => e.ExpenseAccountId).HasColumnName("expense_account_id");
            entity.Property(e => e.PayableAccountId).HasColumnName("payable_account_id");
            entity.Property(e => e.LinkedApInvoiceId).HasColumnName("linked_ap_invoice_id");

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("IpcId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
            entity.Ignore(e => e.GrossValueToDate);
            entity.Ignore(e => e.GrossValueThisPeriod);
            entity.Ignore(e => e.GrossValuePreviousIpc);
            entity.Ignore(e => e.RetentionAmount);
            entity.Ignore(e => e.AdvanceRecoveryAmount);
            entity.Ignore(e => e.NetPayable);
        });

        modelBuilder.Entity<IpcLine>(entity =>
        {
            entity.ToTable("ipc_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CommercialDocumentLineId).HasColumnName("commercial_document_line_id");
            entity.Property(e => e.Rate).HasColumnName("rate").HasColumnType("numeric(18,2)");
            entity.Property(e => e.QuantityThisPeriod).HasColumnName("quantity_this_period").HasColumnType("numeric(18,3)");
            entity.Property(e => e.QuantityToDate).HasColumnName("quantity_to_date").HasColumnType("numeric(18,3)");
            entity.Property<Guid>("IpcId").HasColumnName("ipc_id");

            entity.Ignore(e => e.ValueThisPeriod);
            entity.Ignore(e => e.ValueToDate);
        });

        modelBuilder.Entity<VariationOrder>(entity =>
        {
            entity.ToTable("variation_orders");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CommercialDocumentType).HasColumnName("commercial_document_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CommercialDocumentId).HasColumnName("commercial_document_id");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();

            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey("VariationOrderId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Lines)
                .HasField("_lines")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
            entity.Ignore(e => e.TotalValue);
        });

        modelBuilder.Entity<VariationOrderLine>(entity =>
        {
            entity.ToTable("variation_order_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CommercialDocumentLineId).HasColumnName("commercial_document_line_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
            entity.Property(e => e.DescriptionArabic).HasColumnName("description_arabic").HasMaxLength(500);
            entity.Property(e => e.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20);
            entity.Property(e => e.WbsElementId).HasColumnName("wbs_element_id");
            entity.Property(e => e.QuantityDelta).HasColumnName("quantity_delta").HasColumnType("numeric(18,3)");
            entity.Property(e => e.Rate).HasColumnName("rate").HasColumnType("numeric(18,2)");
            entity.Property<Guid>("VariationOrderId").HasColumnName("variation_order_id");

            entity.Ignore(e => e.Amount);
        });

        modelBuilder.Entity<RetentionRelease>(entity =>
        {
            entity.ToTable("retention_releases");
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

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CommercialDocumentType).HasColumnName("commercial_document_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CommercialDocumentId).HasColumnName("commercial_document_id");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.AmountReleased).HasColumnName("amount_released").HasColumnType("numeric(18,2)");
            entity.Property(e => e.TriggerEvent).HasColumnName("trigger_event").HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.RevenueAccountId).HasColumnName("revenue_account_id");
            entity.Property(e => e.ReceivableAccountId).HasColumnName("receivable_account_id");
            entity.Property(e => e.TaxCodeId).HasColumnName("tax_code_id");
            entity.Property(e => e.VatAccountId).HasColumnName("vat_account_id");
            entity.Property(e => e.LinkedArInvoiceId).HasColumnName("linked_ar_invoice_id");
            entity.Property(e => e.ExpenseAccountId).HasColumnName("expense_account_id");
            entity.Property(e => e.PayableAccountId).HasColumnName("payable_account_id");
            entity.Property(e => e.LinkedApInvoiceId).HasColumnName("linked_ap_invoice_id");

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
