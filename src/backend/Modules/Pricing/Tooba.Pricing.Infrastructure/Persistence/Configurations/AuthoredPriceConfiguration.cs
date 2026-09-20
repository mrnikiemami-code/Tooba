using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Pricing.Domain;

namespace Tooba.Pricing.Infrastructure.Persistence.Configurations;

/// <summary>
/// پیکربندی EF برای AuthoredPrice.
/// </summary>
public sealed class AuthoredPriceConfiguration : IEntityTypeConfiguration<AuthoredPrice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuthoredPrice> entity)
    {
        entity.ToTable("prices");
        entity.HasKey(x => x.PriceId);
        entity.Property(x => x.PriceId).ValueGeneratedNever();
        entity.Property(x => x.Market).HasMaxLength(16);
        entity.Property(x => x.Currency).HasMaxLength(3);
        entity.Property(x => x.Amount).HasPrecision(19, 4);
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.QualifierKind).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.QualifierKey).HasMaxLength(64);
        entity.Ignore(x => x.DomainEvents);
        entity.HasIndex(x => new { x.OfferId, x.Market, x.Channel, x.Currency, x.QualifierKind, x.ValidFrom });
    }
}
