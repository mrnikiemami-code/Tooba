using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Inventory.Domain.Aggregates;

namespace Tooba.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for one shared allocation per Offer.</summary>
public sealed class OfferAllocationConfiguration : IEntityTypeConfiguration<OfferAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OfferAllocation> entity)
    {
        entity.ToTable("offer_allocations");
        entity.HasKey(x => x.OfferId);
        entity.Property(x => x.OfferId).ValueGeneratedNever();
        entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)");
        entity.Property(x => x.Held).HasColumnType("numeric(18,6)");
        entity.Property(x => x.Revision).IsConcurrencyToken();
    }
}
