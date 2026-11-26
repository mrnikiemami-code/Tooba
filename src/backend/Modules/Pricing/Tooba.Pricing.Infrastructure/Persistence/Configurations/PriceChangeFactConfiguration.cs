using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Pricing.Domain.Aggregates;

namespace Tooba.Pricing.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for Pricing-owned price change facts.</summary>
public sealed class PriceChangeFactConfiguration : IEntityTypeConfiguration<PriceChangeFact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PriceChangeFact> entity)
    {
        entity.ToTable("offer_price_changes");
        entity.HasKey(x => x.FactId);
        entity.Property(x => x.FactId).ValueGeneratedNever();
        entity.Property(x => x.Currency).HasMaxLength(3);
        entity.Property(x => x.Revision).HasMaxLength(64);
        entity.Property(x => x.OldAmount).HasPrecision(19, 4);
        entity.Property(x => x.NewAmount).HasPrecision(19, 4);
        entity.HasIndex(x => new { x.OfferId, x.OccurredAt });
        entity.HasIndex(x => x.OperationId).IsUnique();
    }
}
