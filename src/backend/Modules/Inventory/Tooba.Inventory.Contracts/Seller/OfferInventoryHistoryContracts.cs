namespace Tooba.Inventory.Contracts.Seller;

/// <summary>Durable Inventory-owned change fact for one Offer.</summary>
public sealed record OfferInventoryChangeFact(
    Guid FactId,
    Guid OfferId,
    Guid? StockItemId,
    Guid? LocationId,
    string Kind,
    Guid OperationId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    decimal? OldOnHand,
    decimal? NewOnHand,
    decimal? OldAllocation,
    decimal? NewAllocation,
    string? Reason);

/// <summary>Read-only Inventory history boundary used by Offer history.</summary>
public interface IOfferInventoryHistoryReader
{
    /// <summary>Lists durable Inventory change facts for one Offer.</summary>
    Task<IReadOnlyList<OfferInventoryChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken);
}
