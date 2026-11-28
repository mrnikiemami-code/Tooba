using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Offer.Domain.Aggregates;

namespace Tooba.Offer.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the Offer-owned shipping method link rows.
/// </summary>
/// <remarks>
/// Only own-schema persistence: no foreign key into Shipping and no copied code/label/rate/type. Uniqueness of
/// (offer, method) is the database invariant; a duplicate insert is a hard failure, never a silent merge.
/// </remarks>
public sealed class OfferShippingMethodConfiguration : IEntityTypeConfiguration<OfferShippingMethod>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OfferShippingMethod> entity)
    {
        entity.ToTable("offer_shipping_methods");
        entity.HasKey(x => new { x.OfferId, x.MethodId });
        entity.Property(x => x.OfferId).ValueGeneratedNever();
        entity.Property(x => x.MethodId).ValueGeneratedNever();
        entity.HasOne<SellerOffer>()
            .WithMany()
            .HasForeignKey(x => x.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(x => x.OfferId);
    }
}
