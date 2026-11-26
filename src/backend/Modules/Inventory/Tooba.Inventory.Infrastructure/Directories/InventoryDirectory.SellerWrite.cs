using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Seller stock-write use case for <see cref="InventoryDirectory"/>. A seller may adjust physical
/// stock only at a location explicitly owned by that same seller; platform fulfillment physical
/// stock is denied to sellers and no default warehouse is invented.
/// </summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OnHand < 0)
            return Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid));
        var offer = await FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
        if (offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform
            && !request.AdminPhysicalAdjustment)
            return Result.Failure(new SemanticError(InventoryErrorCodes.LocationNotEligible));

        if (request.StockItemId is not Guid stockItemId)
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdRequired));
        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.ExpectedRevision))
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(new SemanticError(InventoryErrorCodes.AdjustmentReasonRequired));

        var selected = await _db.Positions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StockItemId == stockItemId && x.OfferId == request.OfferId, cancellationToken);
        if (selected is null)
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdUnknown));

        var location = await _db.Locations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LocationId == selected.LocationId, cancellationToken);
        if (location is null || location.Status != InventoryLocationStatus.Active
            || (location.OwnerKind == InventoryLocationOwnerKind.Seller
                ? location.SellerPartyId != request.SellerPartyId
                    || offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform
                : !request.AdminPhysicalAdjustment
                    || location.SellerPartyId is not null
                    || offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Seller))
            return Result.Failure(new SemanticError(InventoryErrorCodes.LocationNotEligible));

        var operationId = request.OperationId;
        var reason = request.Reason.Trim();
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await LockOfferAllocationAsync(request.OfferId, cancellationToken);
        // Serialize workspace intents on the target position, before checking the durable replay.
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM inventory.stock_positions WHERE stock_item_id = {stockItemId} FOR UPDATE",
            cancellationToken);
        var replay = await _db.OfferInventoryChanges.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (replay is not null)
        {
            return replay.OfferId == request.OfferId && replay.StockItemId == stockItemId
                && replay.Kind == "PhysicalChanged" && replay.NewOnHand == request.OnHand
                && replay.ActorUserId == request.ActorUserId && replay.Reason == reason
                ? Result.Success()
                : Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));
        }

        var current = await _db.Positions.AsNoTracking()
            .SingleAsync(x => x.StockItemId == stockItemId, cancellationToken);
        if (!RevisionMatches(current.UpdatedAt, request.ExpectedRevision))
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));
        if (request.OnHand < current.Reserved)
            return Result.Failure(new SemanticError(InventoryErrorCodes.AdjustmentQuantityExceeds));

        var variant = await FindVariantAsync(offer.CatalogVariantId, cancellationToken);
        if (variant is null)
            return Result.Failure(new SemanticError(InventoryErrorCodes.CatalogVariantMissing));
        if (decimal.Round(request.OnHand, Math.Clamp(variant.QuantityDecimalPlaces, 0, 28)) != request.OnHand
            || (variant.QuantityStep is decimal step && step > 0 && request.OnHand % step != 0))
            return Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid));

        var now = _clock.UtcNow;
        if (now.UtcTicks / 10 <= current.UpdatedAt.UtcTicks / 10)
            now = current.UpdatedAt.AddTicks(10);
        // Absolute SET and the owner revision predicate are one SQL statement.
        var affected = await _db.Positions
            .Where(x => x.StockItemId == stockItemId && x.UpdatedAt == current.UpdatedAt
                && x.Reserved <= request.OnHand)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.OnHand, request.OnHand)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        if (affected != 1)
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));

        var position = await _db.Positions.SingleAsync(x => x.StockItemId == stockItemId, cancellationToken);
        await _db.Entry(position).ReloadAsync(cancellationToken);
        position.RecordAdjustment(StockAdjustmentKind.Set, request.OnHand - current.OnHand, reason);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        _db.OfferInventoryChanges.Add(new InventoryChangeFact
        {
            FactId = _ids.NewId(),
            OfferId = request.OfferId,
            StockItemId = stockItemId,
            LocationId = current.LocationId,
            Kind = "PhysicalChanged",
            OperationId = operationId,
            ActorUserId = request.ActorUserId,
            OccurredAt = now,
            OldOnHand = current.OnHand,
            NewOnHand = request.OnHand,
            Reason = reason,
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private static bool RevisionMatches(DateTimeOffset actual, string expected)
    {
        return DateTimeOffset.TryParse(expected, CultureInfo.InvariantCulture,
                   DateTimeStyles.RoundtripKind, out var parsed)
               && parsed.UtcTicks / 10 == actual.UtcTicks / 10;
    }

}
