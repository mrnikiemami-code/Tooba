using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Ports;
using IOfferLookup = Tooba.Offer.Contracts.Ports.IOfferLookupGateway;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>Inventory-owned platform-fulfilled offered quantity and durable owner facts.</summary>
public sealed partial class InventoryDirectory : ISellerOfferAllocationGateway, IOfferInventoryHistoryReader
{
    /// <inheritdoc />
    public async Task<Result<OfferAllocationReceipt>> SetAllocationAsync(
        SetSellerOfferAllocation request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Quantity < 0)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.QuantityInvalid));

        var offer = await FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(OfferErrorCodes.NotFound));
        if (offer.FulfillmentResponsibility != OfferFulfillmentResponsibility.Platform)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.LocationNotEligible));

        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.ExpectedRevision))
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationRevisionConflict));
        if (_commerce is not null && _commerce.Current is not { Edition.Edition: ToobaEdition.Marketplace, Tenant: null })
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.LocationNotEligible));
        var variant = await FindVariantAsync(offer.CatalogVariantId, cancellationToken);
        if (variant is null)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.CatalogVariantMissing));
        if (decimal.Round(request.Quantity, Math.Clamp(variant.QuantityDecimalPlaces, 0, 28)) != request.Quantity
            || (variant.QuantityStep is decimal step && step > 0 && request.Quantity % step != 0))
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.QuantityInvalid));
        var operationId = request.OperationId;
        var now = _clock.UtcNow;

        await _guard.EnsureCanMutateAsync(cancellationToken);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await LockOfferAllocationAsync(request.OfferId, cancellationToken);
        var replay = await _db.OfferInventoryChanges.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (replay is not null)
        {
            if (replay.OfferId != request.OfferId || replay.Kind != "AllocationChanged"
                || replay.NewAllocation != request.Quantity || replay.ActorUserId != request.ActorUserId)
                return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationRevisionConflict));
            var currentReplay = await _db.OfferAllocations.AsNoTracking()
                .SingleAsync(x => x.OfferId == request.OfferId, cancellationToken);
            return Result.Success(ToAllocationReceipt(currentReplay, operationId, request.ActorUserId));
        }

        var existing = await _db.OfferAllocations
            .SingleOrDefaultAsync(x => x.OfferId == request.OfferId, cancellationToken);
        if (existing is not null)
            await _db.Entry(existing).ReloadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ExpectedRevision)
            || (existing is null
                ? request.ExpectedRevision != "absent"
                : !string.Equals(existing.Revision.ToString("N"), request.ExpectedRevision, StringComparison.Ordinal)))
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationRevisionConflict));
        var physical = await (from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where position.OfferId == request.OfferId && location.Status == InventoryLocationStatus.Active
                && location.OwnerKind == InventoryLocationOwnerKind.Marketplace && location.SellerPartyId == null
            select (decimal?)position.OnHand).SumAsync(cancellationToken) ?? 0m;
        var activeHeld = await (from reservation in _db.Reservations.AsNoTracking()
            join position in _db.Positions.AsNoTracking() on reservation.StockItemId equals position.StockItemId
            where position.OfferId == request.OfferId && reservation.Status == StockReservationStatus.Held
                && (reservation.Purpose == null || reservation.Purpose == StockReservationPurpose.NewSale)
            select reservation.Quantity).SumAsync(cancellationToken);
        var held = existing?.Held ?? activeHeld;
        if (request.Quantity < held)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationBelowHeld));
        if (request.Quantity > physical)
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationExceedsPhysical));

        var oldQuantity = existing?.Quantity;
        var revision = _ids.NewId();
        if (existing is null)
        {
            existing = OfferAllocation.Create(request.OfferId, request.Quantity, revision, now);
            existing.Sync(request.Quantity, held, revision, now);
            _db.OfferAllocations.Add(existing);
        }
        else
        {
            existing.SetQuantity(request.Quantity, revision, now);
        }

        _db.OfferInventoryChanges.Add(new InventoryChangeFact
        {
            FactId = _ids.NewId(),
            OfferId = request.OfferId,
            Kind = "AllocationChanged",
            OperationId = operationId,
            ActorUserId = request.ActorUserId,
            OccurredAt = now,
            OldAllocation = oldQuantity,
            NewAllocation = request.Quantity,
            Reason = "seller-offered-quantity",
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<OfferAllocationReceipt>(new SemanticError(InventoryErrorCodes.AllocationRevisionConflict));
        }

        return Result.Success(ToAllocationReceipt(existing, operationId, request.ActorUserId));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferInventoryChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken) =>
        await _db.OfferInventoryChanges.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.FactId)
            .Select(x => new OfferInventoryChangeFact(x.FactId, x.OfferId, x.StockItemId, x.LocationId,
                x.Kind, x.OperationId, x.ActorUserId, x.OccurredAt, x.OldOnHand, x.NewOnHand,
                x.OldAllocation, x.NewAllocation, x.Reason))
            .ToListAsync(cancellationToken);

    // Same transaction-scoped lock for first allocation, reservation and workspace physical mutation.
    private Task<int> LockOfferAllocationAsync(Guid offerId, CancellationToken cancellationToken) =>
        LockOfferAllocationAsync(_db, offerId, cancellationToken);

    internal static Task<int> LockOfferAllocationAsync(Tooba.Inventory.Infrastructure.Persistence.InventoryDbContext db, Guid offerId, CancellationToken cancellationToken)
    {
        var lockKey = $"inventory.offer-allocation:{offerId:N}";
        return db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
    }

    private bool IsLocationEligible(InventoryLocation location, OfferReference offer) =>
        _commerce?.Current switch
        {
            { Edition.Edition: ToobaEdition.SingleStore, Tenant.Status: TenantStatus.Active } =>
                location.OwnerKind == InventoryLocationOwnerKind.Store && location.SellerPartyId is null,
            { Edition.Edition: ToobaEdition.Marketplace, Tenant: null } =>
                offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform
                    ? location.OwnerKind == InventoryLocationOwnerKind.Marketplace && location.SellerPartyId is null
                    : location.OwnerKind == InventoryLocationOwnerKind.Seller && location.SellerPartyId == offer.SellerPartyId,
            _ => false,
        };

    private static OfferAllocationReceipt ToAllocationReceipt(OfferAllocation row, Guid operationId, Guid? actor) =>
        new(row.OfferId, row.Quantity, row.Held, Math.Max(0, row.Quantity - row.Held),
            row.Revision.ToString("N"), operationId, actor, row.UpdatedAt);
}
