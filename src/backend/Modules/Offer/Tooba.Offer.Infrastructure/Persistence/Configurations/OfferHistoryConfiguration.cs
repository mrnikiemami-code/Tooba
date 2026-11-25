using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Offer.Domain.Aggregates;

namespace Tooba.Offer.Infrastructure.Persistence.Configurations;

/// <summary>Configures the append-only Offer history projection.</summary>
public sealed class OfferHistoryConfiguration : IEntityTypeConfiguration<OfferHistoryEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OfferHistoryEntry> entity)
    {
        entity.ToTable("offer_history");
        entity.HasKey(x => x.OfferHistoryEntryId);
        entity.Property(x => x.OfferHistoryEntryId).ValueGeneratedNever();
        entity.Property(x => x.OfferId);
        entity.Property(x => x.OccurredAt);
        entity.Property(x => x.ChangeKind).HasMaxLength(64);
        entity.Property(x => x.ActorUserId);
        entity.Property(x => x.Summary).HasMaxLength(512);
        entity.Property(x => x.OldValue).HasMaxLength(1024);
        entity.Property(x => x.NewValue).HasMaxLength(1024);
        entity.HasIndex(x => new { x.OfferId, x.OccurredAt })
            .IsDescending(false, true);
    }
}
