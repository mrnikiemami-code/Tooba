namespace Tooba.Inventory.Contracts;

/// <summary>Inventory-owned availability summary for offer presentation.</summary>
public sealed record OfferInventorySummary(Guid OfferId, decimal OnHand, decimal Reserved, decimal Available);

/// <summary>Inventory-owned seller stock write request.</summary>
public sealed record SetSellerOfferInventory(Guid OfferId, Guid SellerPartyId, decimal OnHand, string? Reason);

/// <summary>Inventory-owned boundary for offer reads and seller stock writes.</summary>
public interface ISellerOfferInventoryGateway
{
    /// <summary>Reads availability for the requested offers.</summary>
    Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>Sets on-hand stock, creating an owner-selected default location when necessary.</summary>
    Task SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken);
}
