using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence.Configurations;

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
        modelBuilder.ApplyConfiguration(new TaxCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new TaxOfferClassificationConfiguration());
        modelBuilder.ApplyConfiguration(new TaxRuleConfiguration());
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
