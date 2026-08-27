using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Story.Domain;
using StoryEntity = Tooba.Story.Domain.Story;

namespace Tooba.Story.Infrastructure.Persistence;

/// <summary>DbContext مالک schema مستقل story.</summary>
public sealed class StoryDbContext : DbContext
{
    /// <summary>schema اختصاصی Story.</summary>
    public const string Schema = "story";

    /// <summary>DbContext را می‌سازد.</summary>
    public StoryDbContext(DbContextOptions<StoryDbContext> options) : base(options) { }

    /// <summary>استوری‌ها.</summary>
    public DbSet<StoryEntity> Stories => Set<StoryEntity>();

    /// <summary>آیتم‌های استوری.</summary>
    public DbSet<StoryItem> StoryItems => Set<StoryItem>();

    /// <summary>Outbox ماژول.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<StoryEntity>(entity =>
        {
            entity.ToTable("stories");
            entity.HasKey(x => x.StoryId);
            entity.Property(x => x.StoryId).ValueGeneratedNever();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(StoryRules.LocaleMaxLength);
            entity.Property(x => x.Market).HasMaxLength(StoryRules.MarketMaxLength);
            entity.Property(x => x.Title).HasMaxLength(StoryRules.TitleMaxLength).IsRequired();
            entity.Property(x => x.CoverMediaUrl).HasMaxLength(StoryRules.MediaUrlMaxLength);
            entity.Property(x => x.Origin).HasConversion<int>().IsRequired();
            entity.Property(x => x.ReviewStatus).HasConversion<int>().IsRequired();
            entity.Property(x => x.RejectionReason).HasMaxLength(StoryRules.RejectionReasonMaxLength);
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.Property(x => x.CtaType).HasMaxLength(StoryRules.CtaTypeMaxLength).IsRequired();
            entity.Property(x => x.CtaTarget).HasMaxLength(StoryRules.CtaTargetMaxLength);
            entity.Property(x => x.VersionToken).IsConcurrencyToken();
            entity.HasIndex(x => new { x.TenantId, x.DisplayOrder });
            entity.HasIndex(x => new { x.TenantId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.ReviewStatus });
            entity.HasIndex(x => new { x.TenantId, x.SellerPartyId });
            entity.Ignore(x => x.Items);
        });
        modelBuilder.Entity<StoryItem>(entity =>
        {
            entity.ToTable("story_items");
            entity.HasKey(x => x.StoryItemId);
            entity.Property(x => x.StoryItemId).ValueGeneratedNever();
            entity.Property(x => x.StoryId).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(StoryRules.MediaTypeMaxLength).IsRequired();
            entity.Property(x => x.MediaUrl).HasMaxLength(StoryRules.MediaUrlMaxLength);
            entity.Property(x => x.Caption).HasMaxLength(StoryRules.CaptionMaxLength);
            entity.Property(x => x.CtaType).HasMaxLength(StoryRules.CtaTypeMaxLength).IsRequired();
            entity.Property(x => x.CtaTarget).HasMaxLength(StoryRules.CtaTargetMaxLength);
            entity.HasIndex(x => new { x.StoryId, x.DisplayOrder });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>کارخانهٔ design-time مهاجرت Story.</summary>
public sealed class StoryDbContextFactory : IDesignTimeDbContextFactory<StoryDbContext>
{
    /// <inheritdoc />
    public StoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            StoryDbContext.Schema,
            typeof(StoryDbContext));
        return new StoryDbContext(options.Options);
    }
}
