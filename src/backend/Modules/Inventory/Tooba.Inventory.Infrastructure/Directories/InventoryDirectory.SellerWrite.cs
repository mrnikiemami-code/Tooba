using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Seller stock-write use case for <see cref="InventoryDirectory"/>: resolves (or opens) the seller's
/// default position and applies a <c>Set</c> adjustment, returning a canonical <see cref="Result"/>.
/// Extracted into a cohesive partial of the same class so the seller-facing use case has one reason to
/// change and behavior is unchanged.
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
        var positions = await _db.Positions.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId)
            .OrderBy(x => x.StockItemId)
            .ToListAsync(cancellationToken);
        Guid stockItemId;
        if (request.StockItemId is Guid target)
        {
            var targetPosition = positions.SingleOrDefault(x => x.StockItemId == target);
            if (targetPosition is null)
                return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdUnknown));
            stockItemId = targetPosition.StockItemId;
        }
        else if (positions.Count > 1)
        {
            return Result.Failure(new SemanticError(InventoryErrorCodes.PositionIdRequired));
        }
        else if (positions.Count == 1)
        {
            stockItemId = positions[0].StockItemId;
        }
        else
        {
            var locationId = await _db.Locations.AsNoTracking()
                .Where(x => x.Status == InventoryLocationStatus.Active)
                .OrderBy(x => x.Code)
                .Select(x => (Guid?)x.LocationId)
                .FirstOrDefaultAsync(cancellationToken);
            locationId ??= await CreateLocationAsync("SELLER-DEFAULT", "Default seller warehouse", cancellationToken);
            stockItemId = await OpenPositionAsync(request.OfferId, locationId.Value, cancellationToken);
        }

        var selected = positions.SingleOrDefault(x => x.StockItemId == stockItemId);
        if (selected is not null && request.OnHand < selected.Reserved)
            return Result.Failure(new SemanticError(InventoryErrorCodes.AdjustmentQuantityExceeds));

        await AdjustAsync(
            stockItemId, StockAdjustmentKind.Set, request.OnHand,
            string.IsNullOrWhiteSpace(request.Reason) ? "seller-panel-adjust" : request.Reason.Trim(),
            null, cancellationToken);
        return Result.Success();
    }
}
