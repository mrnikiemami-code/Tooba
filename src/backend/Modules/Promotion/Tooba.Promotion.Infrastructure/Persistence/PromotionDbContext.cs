using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;
using Tooba.Promotion.Domain.Merchandising;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;


namespace Tooba.Promotion.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>promotion</c>. قیمت، سفارش و مالیات را نگه نمی‌دارد.
/// شامل تعریف تخفیف تسویه و کمپین مرچندایزینگ (جدا از هم).
/// </summary>
public sealed class PromotionDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Promotion.
    /// </summary>
    public const string Schema = "promotion";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public PromotionDbContext(DbContextOptions<PromotionDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// تعاریف پروموشن تسویه.
    /// </summary>
    public DbSet<PromotionDefinition> Promotions => Set<PromotionDefinition>();

    /// <summary>
    /// گونه‌های مرجع مرچندایزینگ.
    /// </summary>
    public DbSet<MerchandisingPromotionType> MerchandisingPromotionTypes => Set<MerchandisingPromotionType>();

    /// <summary>
    /// ترجمه‌های گونهٔ مرچندایزینگ.
    /// </summary>
    public DbSet<MerchandisingPromotionTypeTranslation> MerchandisingPromotionTypeTranslations =>
        Set<MerchandisingPromotionTypeTranslation>();

    /// <summary>
    /// کمپین‌های مرچندایزینگ.
    /// </summary>
    public DbSet<MerchandisingCampaign> MerchandisingCampaigns => Set<MerchandisingCampaign>();

    /// <summary>
    /// ترجمه‌های کمپین.
    /// </summary>
    public DbSet<MerchandisingCampaignTranslation> MerchandisingCampaignTranslations =>
        Set<MerchandisingCampaignTranslation>();

    /// <summary>
    /// عضویت Offer در کمپین.
    /// </summary>
    public DbSet<MerchandisingCampaignOffer> MerchandisingCampaignOffers => Set<MerchandisingCampaignOffer>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<PromotionDefinition>(entity =>
        {
            entity.ToTable("promotions");
            entity.HasKey(x => x.PromotionId);
            entity.Property(x => x.PromotionId).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.StackingPolicy).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.DiscountKind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.PercentageRate).HasPrecision(19, 8);
            entity.Property(x => x.FixedAmount).HasPrecision(19, 4);
            entity.Property(x => x.FixedAmountCurrency).HasMaxLength(3);
            entity.Property(x => x.CouponCode).HasMaxLength(64);
            entity.Property(x => x.Market).HasMaxLength(16);
            entity.Property(x => x.SalesChannel).HasMaxLength(32);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.MinimumSubtotal).HasPrecision(19, 4);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => x.CouponCode);
            entity.HasIndex(x => new { x.Status, x.EffectiveFrom });
        });

        modelBuilder.Entity<MerchandisingPromotionType>(entity =>
        {
            entity.ToTable("merchandising_promotion_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        modelBuilder.Entity<MerchandisingPromotionTypeTranslation>(entity =>
        {
            entity.ToTable("merchandising_promotion_type_translations");
            entity.HasKey(x => new { x.TypeId, x.Locale });
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasOne<MerchandisingPromotionType>()
                .WithMany()
                .HasForeignKey(x => x.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MerchandisingCampaign>(entity =>
        {
            entity.ToTable("merchandising_campaigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(x => x.StoreId);
            entity.HasIndex(x => x.PromotionTypeId);
            entity.HasIndex(x => new { x.StoreId, x.PromotionTypeId, x.LifecycleStatus });
            entity.HasIndex(x => new { x.LifecycleStatus, x.StartAt, x.EndAt });
            entity.HasIndex(x => new { x.StoreId, x.Priority });
            entity.HasOne<MerchandisingPromotionType>()
                .WithMany()
                .HasForeignKey(x => x.PromotionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MerchandisingCampaignTranslation>(entity =>
        {
            entity.ToTable("merchandising_campaign_translations");
            entity.HasKey(x => new { x.CampaignId, x.Locale });
            entity.Property(x => x.Locale).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Subtitle).HasMaxLength(512);
            entity.Property(x => x.BadgeText).HasMaxLength(64);
            entity.HasOne<MerchandisingCampaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MerchandisingCampaignOffer>(entity =>
        {
            entity.ToTable("merchandising_campaign_offers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasIndex(x => new { x.CampaignId, x.SellerOfferId }).IsUnique();
            entity.HasIndex(x => x.SellerOfferId);
            entity.HasIndex(x => new { x.CampaignId, x.SortOrder });
            entity.HasOne<MerchandisingCampaign>()
                .WithMany()
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Promotion.
/// </summary>
public sealed class PromotionDbContextFactory : IDesignTimeDbContextFactory<PromotionDbContext>
{
    /// <inheritdoc />
    public PromotionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PromotionDbContext.Schema,
            typeof(PromotionDbContext));
        return new PromotionDbContext(options.Options);
    }
}
