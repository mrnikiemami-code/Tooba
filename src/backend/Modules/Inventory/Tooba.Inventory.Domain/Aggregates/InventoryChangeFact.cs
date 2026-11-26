namespace Tooba.Inventory.Domain.Aggregates;

/// <summary>Durable Inventory-owned fact for physical or allocation change.</summary>
public sealed class InventoryChangeFact
{
    /// <summary>Stable fact identity.</summary>
    public Guid FactId { get; init; }

    /// <summary>Offer identity.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Physical position when applicable.</summary>
    public Guid? StockItemId { get; init; }

    /// <summary>Location when applicable.</summary>
    public Guid? LocationId { get; init; }

    /// <summary>Change kind.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Operation identity.</summary>
    public Guid OperationId { get; init; }

    /// <summary>Actor when authenticated.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>Commit instant.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Previous physical quantity.</summary>
    public decimal? OldOnHand { get; init; }

    /// <summary>New physical quantity.</summary>
    public decimal? NewOnHand { get; init; }

    /// <summary>Previous allocation quantity.</summary>
    public decimal? OldAllocation { get; init; }

    /// <summary>New allocation quantity.</summary>
    public decimal? NewAllocation { get; init; }

    /// <summary>Operational reason.</summary>
    public string? Reason { get; init; }
}
