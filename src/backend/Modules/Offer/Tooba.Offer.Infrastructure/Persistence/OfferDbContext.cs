using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence.Configurations;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure.Persistence;

/// <summary>
/// DbContext مالک schema <c>offer</c>. قیمت و موجودی اینجا نیستند.
/// </summary>
public sealed class OfferDbContext : DbContext
{
    /// <summary>
    /// schema اختصاصی Offer.
    /// </summary>
    public const string Schema = "offer";

    /// <summary>
    /// DbContext را با گزینه‌های Host می‌سازد.
    /// </summary>
    public OfferDbContext(DbContextOptions<OfferDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// listingهای فروشنده.
    /// </summary>
    public DbSet<SellerOffer> Offers => Set<SellerOffer>();

    /// <summary>
    /// Outbox همین ماژول.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new SellerOfferConfiguration());
        OutboxMessageMapping.Map(modelBuilder, Schema);
    }
}

/// <summary>
/// کارخانهٔ design-time مهاجرت Offer.
/// </summary>
public sealed class OfferDbContextFactory : IDesignTimeDbContextFactory<OfferDbContext>
{
    /// <inheritdoc />
    public OfferDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            ToobaNpgsql.DesignTimeConnectionString(),
            OfferDbContext.Schema,
            typeof(OfferDbContext));
        return new OfferDbContext(options.Options);
    }
}
