using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>tax</c>. قیمت، سفارش و فاکتور را نگه نمی‌دارد.
/// </summary>
public sealed class TaxDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Tax.
    /// </summary>
    public const string Schema = "tax";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public TaxDbContext(DbContextOptions<TaxDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// طبقه‌های مالیاتی مات.
    /// </summary>
    public DbSet<TaxCategory> Categories => Set<TaxCategory>();

    /// <summary>
    /// انتساب Offer به طبقه.
    /// </summary>
    public DbSet<TaxOfferClassification> OfferClassifications => Set<TaxOfferClassification>();

    /// <summary>
    /// قواعد مؤثر به تاریخ.
    /// </summary>
    public DbSet<TaxRule> Rules => Set<TaxRule>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<TaxCategory>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).ValueGeneratedNever();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<TaxOfferClassification>(entity =>
        {
            entity.ToTable("offer_classifications");
            entity.HasKey(x => x.OfferId);
            entity.Property(x => x.OfferId).ValueGeneratedNever();
        });
        modelBuilder.Entity<TaxRule>(entity =>
        {
            entity.ToTable("rules");
            entity.HasKey(x => x.RuleId);
            entity.Property(x => x.RuleId).ValueGeneratedNever();
            entity.Property(x => x.Jurisdiction).HasMaxLength(64);
            entity.Property(x => x.Market).HasMaxLength(16);
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.OverridePolicy).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Rate).HasPrecision(19, 8);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.Jurisdiction, x.Market, x.CategoryId, x.EffectiveFrom });
        });
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Tax.
/// </summary>
public sealed class TaxDbContextFactory : IDesignTimeDbContextFactory<TaxDbContext>
{
    /// <inheritdoc />
    public TaxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaxDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            TaxDbContext.Schema,
            typeof(TaxDbContext));
        return new TaxDbContext(options.Options);
    }
}
