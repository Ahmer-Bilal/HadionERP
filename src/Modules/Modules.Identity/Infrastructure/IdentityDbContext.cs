using Microsoft.EntityFrameworkCore;
using Modules.Identity.Domain;

namespace Modules.Identity.Infrastructure;

/// <summary>
/// This module's own Postgres schema ("identity") — same physical database as every other module, schema-
/// per-module is the boundary (docs/architecture/01-overview.md #3.2), same pattern as
/// every other module's own DbContext.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
            entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");

            entity.HasMany(e => e.Roles)
                .WithOne()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(e => e.Roles)
                .HasField("_roles")
                .UsePropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RoleKey).HasColumnName("role_key").HasMaxLength(200).IsRequired();
            entity.Property<Guid>("UserId").HasColumnName("user_id");
            entity.HasIndex("UserId", nameof(UserRole.RoleKey)).IsUnique();
        });
    }
}
