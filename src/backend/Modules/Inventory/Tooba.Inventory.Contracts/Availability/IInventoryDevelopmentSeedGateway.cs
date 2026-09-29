using Tooba.BuildingBlocks.Results;

namespace Tooba.Inventory.Contracts.Availability;

/// <summary>Inventory-owned Development-support request for demo stock.</summary>
public sealed record SeedDevelopmentStock(
    Guid OfferId,
    Guid LocationId,
    decimal Quantity,
    string Reason);

/// <summary>
/// Inventory-owned Development-support capability used by the Catalog attribute-schema seed
/// so Catalog can ensure demo location/position/stock without touching Inventory persistence.
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
}
