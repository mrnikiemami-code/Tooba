using Tooba.Offer.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Offer.Domain;

namespace Tooba.Offer.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures SellerOffer persistence.
/// </summary>
public sealed class SellerOfferConfiguration : IEntityTypeConfiguration<SellerOffer>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SellerOffer> entity)
    {
        entity.ToTable("offers");
        entity.HasKey(x => x.OfferId);
        entity.Property(x => x.OfferId).ValueGeneratedNever();
        entity.Property(x => x.SellerSku).HasMaxLength(64);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.ReturnPolicyChoice).HasMaxLength(32).HasDefaultValue("Default");
        entity.Property(x => x.CustomReturnWindowDays);
        entity.Property(x => x.MinimumOrderQuantity).HasColumnType("numeric(18,6)");
        entity.Property(x => x.MaximumOrderQuantity).HasColumnType("numeric(18,6)");
        entity.Ignore(x => x.DomainEvents);
        entity.HasIndex(x => new { x.SellerPartyId, x.CatalogVariantId, x.Channel })
            .IsUnique()
            .HasFilter("status <> 'Archived'");
        entity.HasIndex(x => new { x.SellerPartyId, x.SellerSku })
            .IsUnique()
            .HasFilter("seller_sku IS NOT NULL");
    }
}
