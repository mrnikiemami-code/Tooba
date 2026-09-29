using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>Development-only Inventory seed capability owned by Inventory.Infrastructure.</summary>
public sealed class InventoryDevelopmentSeedGateway(
    IInventoryDirectory inventory,
    IInventoryQueryGateway queries) : IInventoryDevelopmentSeedGateway
{
    /// <inheritdoc />
    public async Task<Result<Guid>> EnsureDevelopmentLocationAsync(
        string code,
        string name,
        CancellationToken cancellationToken)
    {
        var existing = await queries.FindLocationByCodeAsync(code, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(existing.LocationId);
        }

        var locationId = await inventory.CreateLocationAsync(code, name, cancellationToken);
        return Result.Success(locationId);
    }

    /// <inheritdoc />
    public async Task<Result> IncreaseDevelopmentStockAsync(
        SeedDevelopmentStock request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Quantity < 0)
        {
            return Result.Failure(new SemanticError(Contracts.Errors.InventoryErrorCodes.QuantityInvalid));
        }

        var stockItemId = await inventory.OpenPositionAsync(request.OfferId, request.LocationId, cancellationToken);
        await inventory.AdjustAsync(
            stockItemId,
            StockAdjustmentKind.Increase,
            request.Quantity,
            request.Reason,
            null,
            cancellationToken);
        return Result.Success();
    }
}
