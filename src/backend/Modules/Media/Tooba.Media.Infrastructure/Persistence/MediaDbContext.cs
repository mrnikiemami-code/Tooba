using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Media.Domain.Aggregates;
using Tooba.Media.Domain.Enums;
using Tooba.Persistence;

namespace Tooba.Media.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل media.</summary>
public sealed class MediaDbContext : DbContext
{
    /// <summary>schema اختصاصی Media.</summary>
    public const string Schema = "media";

    /// <summary>DbContext را می‌سازد.</summary>
    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    /// <summary>دارایی‌های رسانه.</summary>
    public DbSet<MediaAsset> Assets => Set<MediaAsset>();

    /// <summary>مالکیت فروشگاهی دارایی‌های جدید.</summary>
    public DbSet<MediaAssetOwnerBinding> AssetOwnerBindings => Set<MediaAssetOwnerBinding>();

    /// <summary>رخداد آپلود فروشنده برای replay.</summary>
    public DbSet<Tooba.Media.Infrastructure.Assets.MediaAssetOwnerOccurrence> AssetOwnerOccurrences =>
        Set<Tooba.Media.Infrastructure.Assets.MediaAssetOwnerOccurrence>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("assets");
            entity.HasKey(x => x.MediaAssetId);
            entity.Property(x => x.MediaAssetId).ValueGeneratedNever();
            entity.Property(x => x.StorageKey).HasMaxLength(MediaAsset.StorageKeyMaxLength).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(MediaAsset.OriginalFileNameMaxLength).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(MediaAsset.ContentTypeMaxLength).IsRequired();
            entity.Property(x => x.ChecksumSha256).HasMaxLength(MediaAsset.ChecksumSha256Length);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.OriginalFileName);
        });
        modelBuilder.Entity<MediaAssetOwnerBinding>(entity =>
        {
            entity.ToTable("asset_owner_bindings");
            entity.HasKey(x => x.MediaAssetId);
            entity.Property(x => x.MediaAssetId).ValueGeneratedNever();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.SellerPartyId).IsRequired();
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.SellerPartyId, x.CreatedAt });
        });
        modelBuilder.Entity<Tooba.Media.Infrastructure.Assets.MediaAssetOwnerOccurrence>(entity =>
        {
            entity.ToTable("asset_owner_occurrences");
            entity.HasKey(x => x.OccurrenceKey);
            entity.Property(x => x.OccurrenceKey).HasMaxLength(128);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.SellerPartyId).IsRequired();
            entity.Property(x => x.MediaAssetId).IsRequired();
            entity.Property(x => x.ActorUserId).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.SellerPartyId, x.CreatedAt });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Media.</summary>
public sealed class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    /// <inheritdoc />
    public MediaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            MediaDbContext.Schema,
            typeof(MediaDbContext));
        return new MediaDbContext(options.Options);
    }
}
