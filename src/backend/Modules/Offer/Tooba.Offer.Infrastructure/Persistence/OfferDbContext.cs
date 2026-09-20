using Tooba.Offer.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence.Configurations;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure.Persistence;

/// <summary>
/// Owns the <c>offer</c> schema; price and inventory are separate.
/// </summary>
public sealed class OfferDbContext : DbContext
{
    /// <summary>
    /// Offer schema name.
    /// </summary>
    public const string Schema = "offer";

    /// <summary>
    /// Creates the context from Host-provided options.
    /// </summary>
    public OfferDbContext(DbContextOptions<OfferDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Seller listings.
    /// </summary>
    public DbSet<SellerOffer> Offers => Set<SellerOffer>();

    /// <summary>
    /// Offer outbox messages.
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
/// Design-time Offer migration context factory.
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
