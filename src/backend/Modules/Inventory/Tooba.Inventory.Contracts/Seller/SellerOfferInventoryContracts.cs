using Tooba.BuildingBlocks.Results;

namespace Tooba.Inventory.Contracts.Seller;

/// <summary>Inventory-owned availability summary for offer presentation.</summary>
public sealed record OfferInventorySummary(
    Guid OfferId,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    IReadOnlyList<SellerInventoryPosition>? Locations = null);

/// <summary>A named stock position belonging to the offer.</summary>
public sealed record SellerInventoryPosition(
    Guid StockItemId,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    decimal OnHand,
    decimal Reserved,
    decimal Available);

/// <summary>Inventory-owned seller stock write request.</summary>
public sealed record SetSellerOfferInventory(
    Guid OfferId,
    Guid SellerPartyId,
    decimal OnHand,
    string? Reason,
    Guid? StockItemId = null);

/// <summary>Inventory-owned boundary for offer reads and seller stock writes.</summary>
public interface ISellerOfferInventoryGateway
{
    /// <summary>Reads availability for the requested offers.</summary>
    Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>Sets on-hand stock, creating an owner-selected default location when necessary.</summary>
    Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken);
}

/// <summary>An active storage location that is eligible to hold an offer's stock position.</summary>
public sealed record InventoryEligibleLocation(Guid LocationId, string Code, string Name);

/// <summary>An offer stock position provisioned at a single location (physical quantities are not set here).</summary>
public sealed record ProvisionedStockPosition(
    Guid StockItemId,
    Guid LocationId,
    Guid OfferId,
    decimal OnHand,
    decimal Reserved);

/// <summary>
/// Inventory-owned boundary for eligible-location reads and idempotent stock-position provisioning.
/// </summary>
public interface IInventoryProvisioningGateway
{
    /// <summary>Lists active storage locations ordered by code; never creates a location.</summary>
    Task<IReadOnlyList<InventoryEligibleLocation>> ListEligibleLocationsAsync(Guid? sellerPartyId, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures exactly one stock position exists for the offer at the given active location and returns it.
    /// Repeated calls with the same offer/location return the same position; physical quantities stay untouched.
    /// </summary>
    Task<ProvisionedStockPosition> EnsurePositionAsync(
        Guid offerId,
        Guid catalogVariantId,
        Guid locationId,
        Guid? sellerPartyId,
        CancellationToken cancellationToken);
}
