using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Persistence;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence.Configurations;

namespace Tooba.Pricing.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>pricing</c>. قیمت را روی Product یا Offer نگه نمی‌دارد.
/// </summary>
public sealed class PricingDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Pricing.
    /// </summary>
    public const string Schema = "pricing";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public PricingDbContext(DbContextOptions<PricingDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// قیمت‌های نوشته‌شده.
    /// </summary>
    public DbSet<AuthoredPrice> Prices => Set<AuthoredPrice>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AuthoredPriceConfiguration());
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Pricing.
/// </summary>
public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    /// <inheritdoc />
    public PricingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            PricingDbContext.Schema,
            typeof(PricingDbContext));
        return new PricingDbContext(options.Options);
    }
}
