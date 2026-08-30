using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Media.Domain;
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
