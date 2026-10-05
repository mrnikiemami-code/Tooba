using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Fulfillment;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// نوشتن و خواندن موجودی با قرارداد Offer. DbContext کاتالوگ و Offer لمس نمی‌شود.
/// رزرو با UPDATE اتمی PostgreSQL است تا آخرین واحد دو بار فروخته نشود.
/// همکارهای تخصصی (موتور تأمین سفارش، بازپس‌گیر رزرو منقضی، lookupهای بین‌ماژولی) در partialهای مجاور جدا شده‌اند.
/// </summary>
public sealed partial class InventoryDirectory : IInventoryDirectory, IInventoryAvailabilityGateway, ISellerOfferInventoryGateway, IFulfillmentInventoryLifecyclePort, Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly ICatalogVariantLookup _catalog;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IModuleCallTracer _tracer;

    /// <summary>
    /// دایرکتوری را به schema Inventory و درز Offer/Catalog وصل می‌کند نه به join بین‌schema.
    /// </summary>
    public InventoryDirectory(
        InventoryDbContext db,
        IInventoryUseCaseGuard guard,
        IOfferLookupGateway offers,
        ICatalogVariantLookup catalog,
        IClock clock,
        IIdGenerator ids,
        IModuleCallTracer tracer)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
        _catalog = catalog;
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
        ArgumentNullException.ThrowIfNull(ids);
        _ids = ids;
        ArgumentNullException.ThrowIfNull(tracer);
        _tracer = tracer;
    }

    /// <inheritdoc />
    public Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken) =>
        GetAvailabilityCoreAsync(offerId, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken) =>
        GetAvailabilityBatchCoreAsync(offerIds, cancellationToken);

    /// <inheritdoc />
    public async Task<Guid> CreateLocationAsync(string code, string name, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var location = InventoryLocation.Create(_ids.NewId(), code, name, _clock.UtcNow);
        _db.Locations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);
        return location.LocationId;
    }

    /// <inheritdoc />
    public async Task<Guid> OpenPositionAsync(Guid offerId, Guid locationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var offer = await FindOfferAsync(offerId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.PositionIdUnknown);
        if (await FindVariantAsync(offer.CatalogVariantId, cancellationToken) is null)
        {
            throw new ContractOperationException(InventoryErrorCodes.CatalogVariantMissing);
        }

        if (await _db.Locations.SingleOrDefaultAsync(x => x.LocationId == locationId, cancellationToken) is not { Status: InventoryLocationStatus.Active })
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationNotFound);
        }

        var existing = await _db.Positions.SingleOrDefaultAsync(
            x => x.OfferId == offerId && x.LocationId == locationId,
            cancellationToken);
        if (existing is not null)
        {
            return existing.StockItemId;
        }

        var position = StockPosition.Open(_ids.NewId(), offerId, offer.CatalogVariantId, locationId, _clock.UtcNow);
        _db.Positions.Add(position);
        await _db.SaveChangesAsync(cancellationToken);
        return position.StockItemId;
    }

    /// <inheritdoc />
    public async Task AdjustAsync(
        Guid stockItemId,
        StockAdjustmentKind kind,
        decimal quantity,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ContractOperationException(InventoryErrorCodes.AdjustmentReasonRequired);
        }

        if (quantity < 0)
        {
            throw new ContractOperationException(InventoryErrorCodes.AdjustmentQuantityInvalid);
        }

        var position = await _db.Positions.SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.PositionNotFound);

        var delta = kind switch
        {
            StockAdjustmentKind.Increase => quantity,
            StockAdjustmentKind.Decrease => -quantity,
            StockAdjustmentKind.Set => quantity - position.OnHand,
            _ => throw new ContractOperationException(InventoryErrorCodes.AdjustmentKindUnknown),
        };

        var now = _clock.UtcNow;
        var affected = await _db.Positions
            .Where(x => x.StockItemId == stockItemId && x.OnHand + delta >= x.Reserved && x.OnHand + delta >= 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OnHand, x => x.OnHand + delta)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (affected != 1)
        {
            throw new ContractOperationException(InventoryErrorCodes.AdjustmentQuantityExceeds);
        }

        await _db.Entry(position).ReloadAsync(cancellationToken);
        position.RecordAdjustment(kind, delta, reason.Trim());
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
        _ = idempotencyKey;
    }

    /// <inheritdoc />
    public async Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var prior = await _db.Reservations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);
            if (prior is { Status: StockReservationStatus.Held })
            {
                var known = await _db.Positions.AsNoTracking().SingleAsync(x => x.StockItemId == prior.StockItemId, cancellationToken);
                return new ReservationReceipt(prior.ReservationId, prior.StockItemId, known.OfferId, prior.Quantity, prior.Status, prior.ExpiresAt);
            }
        }

        var now = _clock.UtcNow;
        var reserved = await _db.Positions
            .Where(x => x.StockItemId == stockItemId && x.OnHand - x.Reserved >= quantity && quantity > 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Reserved, x => x.Reserved + quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (reserved != 1)
        {
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        }

        var position = await _db.Positions.SingleAsync(x => x.StockItemId == stockItemId, cancellationToken);
        var hold = StockReservation.Hold(_ids.NewId(), stockItemId, quantity, externalReference, idempotencyKey, now, expiresAt);
        _db.Reservations.Add(hold);
        position.RecordReserved(hold.ReservationId, quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsReservationIdempotencyConflict(ex))
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationConflict);
        }

        return new ReservationReceipt(hold.ReservationId, stockItemId, position.OfferId, quantity, hold.Status, hold.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task<ReservationReceipt?> FindReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        var reservation = await _db.Reservations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            .ConfigureAwait(false);
        if (reservation is null)
        {
            return null;
        }

        var position = await _db.Positions.AsNoTracking()
            .SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken)
            .ConfigureAwait(false);
        return new ReservationReceipt(
            reservation.ReservationId,
            reservation.StockItemId,
            position.OfferId,
            reservation.Quantity,
            reservation.Status,
            reservation.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        if (reservation.Status is StockReservationStatus.Released or StockReservationStatus.Consumed)
        {
            return;
        }

        var now = _clock.UtcNow;
        var released = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId && x.Reserved >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (released != 1)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationReleaseMismatch);
        }

        reservation.MoveTo(StockReservationStatus.Released, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordReleased(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        var now = _clock.UtcNow;
        var consumed = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId
                        && x.Reserved >= reservation.Quantity
                        && x.OnHand >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OnHand, x => x.OnHand - reservation.Quantity)
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (consumed != 1)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }

        reservation.MoveTo(StockReservationStatus.Consumed, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordConsumed(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    Task IFulfillmentInventoryLifecyclePort.CommitReservationForPaidOrderAsync(
        Guid reservationId,
        CancellationToken cancellationToken) =>
        CommitReservationForPaidOrderAsync(reservationId, cancellationToken);

    /// <inheritdoc />
    public async Task<ReservationReceipt> CommitReservationForPaidOrderAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        reservation.CommitForPaidOrder(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await FindReservationAsync(reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
    }

    /// <inheritdoc />
    public async Task<ReservationReceipt> PromoteReservationForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        reservation.PromoteForManualPaymentReview(reviewExpiresAt, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await FindReservationAsync(reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
    }

    /// <inheritdoc />
    public async Task<OrderSupplyStatus> GetOrderSupplyStatusAsync(
        Guid checkoutId,
        IReadOnlyList<OrderSupplyLineInput> lines,
        CancellationToken cancellationToken)
    {
        var evaluation = await EvaluateLinesAsync(lines, requireDurable: false, cancellationToken);
        return new OrderSupplyStatus(checkoutId, evaluation.Status, evaluation.Lines);
    }

    private static bool IsReservationIdempotencyConflict(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(pg.ConstraintName, "ix_reservations_idempotency_key", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
