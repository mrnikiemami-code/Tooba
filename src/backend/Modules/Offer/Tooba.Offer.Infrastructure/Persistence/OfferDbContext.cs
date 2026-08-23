using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Offer.Domain;
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
        modelBuilder.Entity<SellerOffer>(entity =>
        {
            entity.ToTable("offers");
            entity.HasKey(x => x.OfferId);
            entity.Property(x => x.OfferId).ValueGeneratedNever();
            entity.Property(x => x.SellerSku).HasMaxLength(64);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Ignore(x => x.DomainEvents);
            entity.HasIndex(x => new { x.SellerPartyId, x.CatalogVariantId, x.Channel })
                .IsUnique()
                .HasFilter("status <> 'Archived'");
            entity.HasIndex(x => new { x.SellerPartyId, x.SellerSku })
                .IsUnique()
                .HasFilter("seller_sku IS NOT NULL");
        });
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
