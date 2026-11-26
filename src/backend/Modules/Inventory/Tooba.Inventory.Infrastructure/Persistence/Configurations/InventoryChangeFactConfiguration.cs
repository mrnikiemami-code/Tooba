using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tooba.Inventory.Domain.Aggregates;

namespace Tooba.Inventory.Infrastructure.Persistence.Configurations;

/// <summary>EF mapping for Inventory-owned change facts.</summary>
public sealed class InventoryChangeFactConfiguration : IEntityTypeConfiguration<InventoryChangeFact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InventoryChangeFact> entity)
    {
        entity.ToTable("offer_inventory_changes");
        entity.HasKey(x => x.FactId);
        entity.Property(x => x.FactId).ValueGeneratedNever();
        entity.Property(x => x.Kind).HasMaxLength(48);
        entity.Property(x => x.Reason).HasMaxLength(256);
        entity.Property(x => x.OldOnHand).HasColumnType("numeric(18,6)");
        entity.Property(x => x.NewOnHand).HasColumnType("numeric(18,6)");
        entity.Property(x => x.OldAllocation).HasColumnType("numeric(18,6)");
        entity.Property(x => x.NewAllocation).HasColumnType("numeric(18,6)");
        entity.HasIndex(x => new { x.OfferId, x.OccurredAt });
        entity.HasIndex(x => x.OperationId).IsUnique();
    }
}
