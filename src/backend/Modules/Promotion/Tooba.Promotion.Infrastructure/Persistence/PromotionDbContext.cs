using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>promotion</c>. قیمت، سفارش و مالیات را نگه نمی‌دارد.
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
    /// تعاریف پروموشن.
    /// </summary>
    public DbSet<PromotionDefinition> Promotions => Set<PromotionDefinition>();

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
