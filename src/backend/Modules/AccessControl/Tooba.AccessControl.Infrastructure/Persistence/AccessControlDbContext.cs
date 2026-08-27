using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.AccessControl.Domain;
using Tooba.Persistence;

namespace Tooba.AccessControl.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>access_control</c>. جداول سایر ماژول‌ها را JOIN نمی‌کند.
/// </summary>
public sealed class AccessControlDbContext : DbContext
{
    /// <summary>schema اختصاصی AccessControl.</summary>
    public const string Schema = "access_control";

    /// <summary>DbContext را با گزینه‌های Host می‌سازد.</summary>
    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
        : base(options)
    {
    }

    /// <summary>نقش‌ها.</summary>
    public DbSet<AccessRole> Roles => Set<AccessRole>();

    /// <summary>اعطای مجوز نقش.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>تخصیص کاربر-نقش.</summary>
    public DbSet<UserRoleAssignment> Assignments => Set<UserRoleAssignment>();

    /// <summary>سقف تفویض فروشنده.</summary>
    public DbSet<PlatformSellerCeiling> SellerCeilings => Set<PlatformSellerCeiling>();

    /// <summary>رخدادهای audit.</summary>
    public DbSet<AccessAuditEvent> AuditEvents => Set<AccessAuditEvent>();

    /// <summary>Outbox همین ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AccessRole>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.TenantId).HasMaxLength(128);
            entity.Property(x => x.OwnerScopeKind).HasConversion<int>();
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => new { x.OwnerScopeKind, x.OwnerScopeId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.OwnerScopeKind, x.OwnerScopeId, x.IsArchived });
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.PermissionId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ScopeKind).HasConversion<int>();
            entity.HasIndex(x => new { x.RoleId, x.PermissionId, x.ScopeKind, x.ScopeResourceId }).IsUnique();
            entity.HasIndex(x => x.RoleId);
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("user_role_assignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OwnerScopeKind).HasConversion<int>();
            entity.HasIndex(x => new { x.UserId, x.RoleId, x.OwnerScopeKind, x.OwnerScopeId }).IsUnique();
            entity.HasIndex(x => new { x.OwnerScopeKind, x.OwnerScopeId, x.UserId });
            entity.HasIndex(x => x.RoleId);
        });

        modelBuilder.Entity<PlatformSellerCeiling>(entity =>
        {
            entity.ToTable("platform_seller_ceilings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.PermissionId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ScopeKind).HasConversion<int>();
            entity.HasIndex(x => new { x.SellerPartyId, x.PermissionId, x.ScopeKind, x.ScopeResourceId }).IsUnique();
            entity.HasIndex(x => x.SellerPartyId);
        });

        modelBuilder.Entity<AccessAuditEvent>(entity =>
        {
            entity.ToTable("access_audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TargetType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TargetId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.BeforeSummary).HasMaxLength(1024);
            entity.Property(x => x.AfterSummary).HasMaxLength(1024);
            entity.Property(x => x.TraceId).HasMaxLength(128);
            entity.HasIndex(x => x.At);
            entity.HasIndex(x => new { x.SellerScopeId, x.At });
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ زمان طراحی مهاجرت.</summary>
public sealed class AccessControlDbContextFactory : IDesignTimeDbContextFactory<AccessControlDbContext>
{
    /// <inheritdoc />
    public AccessControlDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccessControlDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            AccessControlDbContext.Schema,
            typeof(AccessControlDbContext));
        return new AccessControlDbContext(options.Options);
    }
}
