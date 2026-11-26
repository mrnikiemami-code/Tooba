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

        var positions = await _db.Positions.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId)
            .OrderBy(x => x.StockItemId)
            .ToListAsync(cancellationToken);
        Guid stockItemId;
        StockPosition? selected;
        if (request.StockItemId is Guid target)
        {
            selected = positions.SingleOrDefault(x => x.StockItemId == target);
            if (selected is null)
                return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdUnknown));
            if (!await IsSellerOwnedLocationAsync(selected.LocationId, request.SellerPartyId, cancellationToken)
                && !request.AdminPhysicalAdjustment)
                return Result.Failure(new SemanticError(InventoryErrorCodes.LocationNotEligible));
            stockItemId = selected.StockItemId;
        }
        else if (positions.Count > 1)
        {
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdRequired));
        }
        else if (positions.Count == 1)
        {
            selected = positions[0];
            if (!await IsSellerOwnedLocationAsync(selected.LocationId, request.SellerPartyId, cancellationToken)
                && !request.AdminPhysicalAdjustment)
                return Result.Failure(new SemanticError(InventoryErrorCodes.LocationNotEligible));
            stockItemId = selected.StockItemId;
        }
        else
        {
            selected = null;
            var locationId = await _db.Locations.AsNoTracking()
                .Where(x => x.Status == InventoryLocationStatus.Active
                    && (request.AdminPhysicalAdjustment
                        ? x.OwnerKind != InventoryLocationOwnerKind.Seller
                        : x.OwnerKind == InventoryLocationOwnerKind.Seller && x.SellerPartyId == request.SellerPartyId))
                .OrderBy(x => x.Code)
                .Select(x => (Guid?)x.LocationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (locationId is null)
                return Result.Failure(new SemanticError(InventoryErrorCodes.LocationNotFound));
            stockItemId = await OpenPositionAsync(request.OfferId, locationId.Value, cancellationToken);
        }

        if (selected is not null && request.OnHand < selected.Reserved)
            return Result.Failure(new SemanticError(InventoryErrorCodes.AdjustmentQuantityExceeds));
        if (string.IsNullOrWhiteSpace(request.ExpectedRevision)
            || (selected is null
                ? request.ExpectedRevision != "absent"
                : !RevisionMatches(selected.UpdatedAt, request.ExpectedRevision)))
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));

        var operationId = request.OperationId == Guid.Empty ? _ids.NewId() : request.OperationId;
        var replay = await _db.OfferInventoryChanges.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (replay is not null)
        {
            return replay.OfferId == request.OfferId && replay.NewOnHand == request.OnHand
                ? Result.Success()
                : Result.Failure(new SemanticError(InventoryErrorCodes.PositionRevisionConflict));
        }

        var oldOnHand = selected?.OnHand;
        var now = _clock.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await AdjustAsync(
            stockItemId, StockAdjustmentKind.Set, request.OnHand,
            string.IsNullOrWhiteSpace(request.Reason) ? "seller-panel-adjust" : request.Reason.Trim(),
            null, cancellationToken);

        _db.OfferInventoryChanges.Add(new InventoryChangeFact
        {
            FactId = _ids.NewId(),
            OfferId = request.OfferId,
            StockItemId = stockItemId,
            LocationId = selected?.LocationId,
            Kind = "PhysicalChanged",
            OperationId = operationId,
            ActorUserId = request.ActorUserId,
            OccurredAt = now,
            OldOnHand = oldOnHand,
            NewOnHand = request.OnHand,
            Reason = request.Reason?.Trim(),
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

    private Task<bool> IsSellerOwnedLocationAsync(Guid locationId, Guid sellerPartyId, CancellationToken cancellationToken) =>
        _db.Locations.AsNoTracking().AnyAsync(
            x => x.LocationId == locationId
                && x.Status == InventoryLocationStatus.Active
                && x.OwnerKind == InventoryLocationOwnerKind.Seller
                && x.SellerPartyId == sellerPartyId,
            cancellationToken);
}
