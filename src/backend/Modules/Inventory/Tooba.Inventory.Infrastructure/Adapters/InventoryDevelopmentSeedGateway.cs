using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Errors;
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
            return Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid));
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

    /// <inheritdoc />
    public async Task<Result> ReserveDevelopmentHoldAsync(
        SeedDevelopmentStockHold request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Quantity <= 0)
        {
            return Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid));
        }

        var location = await EnsureDevelopmentLocationAsync(
            request.LocationCode,
            request.LocationCode,
            cancellationToken);
        if (location.IsFailure)
        {
            return Result.Failure(location.FirstError);
        }

        var stockItemId = await inventory.OpenPositionAsync(request.OfferId, location.Value, cancellationToken);
        await inventory.ReserveAsync(
            stockItemId,
            request.Quantity,
            request.ExternalReference,
            request.IdempotencyKey,
            null,
            cancellationToken);
        return Result.Success();
    }
}
