using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Domain;
using Platform.Core;

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
            // counter, not "has this row changed at all" (a plain field edit like UpdateContactDetails
            // never touches it). Postgres's own xmin system column tracks every write regardless of which
            // property changed, so THAT is the real optimistic-concurrency token, not this column — found
            // via an integration test that proved two concurrent field edits weren't actually conflicting.
            entity.Property(e => e.RowVersion).HasColumnName("row_version");
            entity.UseXminAsConcurrencyToken();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.PartnerType).HasColumnName("partner_type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TaxRegistrationNumber).HasColumnName("tax_registration_number").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.AddressLine).HasColumnName("address_line").HasMaxLength(300);

            // ExtensionFieldBag doesn't expose its internals — it exists specifically so extensions can
            // add fields without a schema migration (docs/architecture/04-data-and-api.md #1.3) — so it's
            // persisted as its own JSON serialization, not decomposed into columns.
            entity.Property(e => e.ExtensionFields)
                .HasColumnName("extension_data")
                .HasColumnType("jsonb")
                .HasConversion(bag => bag.ToJson(), json => ExtensionFieldBag.FromJson(json));

            // Not persisted in this first slice: DomainEvents is transient by definition (drained and
            // published after save, never stored itself); Relations (typed links to other BOs) has no
            // real use yet since BusinessPartner doesn't reference other Business Objects — add a proper
            // mapping when a module actually needs it rather than guessing its shape now.
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.Relations);
            entity.Ignore(e => e.CanHardDelete);
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
    }
}
