using Tooba.BuildingBlocks.Results;

namespace Tooba.Inventory.Contracts.Availability;

/// <summary>Inventory-owned Development-support request for demo stock.</summary>
public sealed record SeedDevelopmentStock(
    Guid OfferId,
    Guid LocationId,
    decimal Quantity,
    string Reason);

/// <summary>Inventory-owned Development-support request for a demo stock hold.</summary>
public sealed record SeedDevelopmentStockHold(
    Guid OfferId,
    string LocationCode,
    decimal Quantity,
    string ExternalReference,
    string IdempotencyKey);

/// <summary>Inventory-owned Development-support request to drain an offer position to zero.</summary>
public sealed record SeedDevelopmentStockDrain(
    Guid OfferId,
    Guid LocationId,
    string Reason);

/// <summary>
/// Inventory-owned Development-support capability used by module-owned Development seeds
/// so they can ensure demo location/position/stock without touching Inventory persistence.
/// </summary>
public interface IInventoryDevelopmentSeedGateway
{
    /// <summary>Creates and activates the demo location when absent, otherwise returns the existing one.</summary>
    Task<Result<Guid>> EnsureDevelopmentLocationAsync(
        string code,
        string name,
        CancellationToken cancellationToken);

    /// <summary>Opens the offer position when absent and increases on-hand by the given quantity.</summary>
    Task<Result> IncreaseDevelopmentStockAsync(
        SeedDevelopmentStock request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens the offer position at the location when absent and reduces on-hand to zero so the offer
    /// demonstrates an out-of-stock scenario. The "drain to zero" semantics are Inventory-owned.
    /// </summary>
    Task<Result> DrainDevelopmentStockAsync(
        SeedDevelopmentStockDrain request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Holds the given quantity on the offer's position at the location (opening the position
    /// when absent). Reuses an existing hold with the same idempotency key.
    /// </summary>
    Task<Result> ReserveDevelopmentHoldAsync(
        SeedDevelopmentStockHold request,
        CancellationToken cancellationToken);
}
