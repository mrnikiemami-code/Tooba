using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Tax.Domain;

namespace Tooba.Tax.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF برای انتساب Offer به طبقه.
/// </summary>
public sealed class TaxOfferClassificationConfiguration : IEntityTypeConfiguration<TaxOfferClassification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaxOfferClassification> entity)
    {
        entity.ToTable("offer_classifications");
        entity.HasKey(x => x.OfferId);
        entity.Property(x => x.OfferId).ValueGeneratedNever();
    }
}
