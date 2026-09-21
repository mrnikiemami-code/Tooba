using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Catalog.Contracts;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Fulfillment;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Domain.Events;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس انبار اینجا نیست.
/// </summary>
public sealed class OpenInventoryUseCaseGuard : IInventoryUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن و خواندن موجودی با قرارداد Offer. DbContext کاتالوگ و Offer لمس نمی‌شود.
/// رزرو با UPDATE اتمی PostgreSQL است تا آخرین واحد دو بار فروخته نشود.
/// </summary>
public sealed class InventoryDirectory : IInventoryDirectory, IInventoryAvailabilityGateway, ISellerOfferInventoryGateway, IFulfillmentInventoryLifecyclePort, Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort
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
    public async Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var rows = await (
            from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where position.OfferId == offerId
            select new { position, location }).ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return null;
        }

        var locations = rows.Select(row => new LocationAvailability(
            row.position.StockItemId,
            row.location.LocationId,
            row.location.Code,
            row.position.OnHand,
            row.position.Reserved,
            row.position.OnHand - row.position.Reserved)).ToList();
        return new InventoryAvailability(
            offerId,
            rows[0].position.CatalogVariantId,
            locations.Sum(x => x.OnHand),
            locations.Sum(x => x.Reserved),
            locations.Sum(x => x.Available),
            locations);
    }

    async Task<IReadOnlyDictionary<Guid, OfferInventorySummary>> ISellerOfferInventoryGateway.GetAvailabilityAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        var values = await GetAvailabilityBatchAsync(offerIds, cancellationToken);
        return values.ToDictionary(
            x => x.Key,
            x => new OfferInventorySummary(x.Key, x.Value.OnHand, x.Value.Reserved, Math.Max(0, x.Value.Available)));
    }

    /// <inheritdoc />
    public async Task<Result> SetInventoryAsync(SetSellerOfferInventory request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OnHand < 0)
            return Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid));
        var offer = await FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
        var position = await _db.Positions.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId)
            .OrderBy(x => x.StockItemId)
            .FirstOrDefaultAsync(cancellationToken);
        var stockItemId = position?.StockItemId;
        if (stockItemId is null)
        {
            var locationId = await _db.Locations.AsNoTracking()
                .Where(x => x.Status == InventoryLocationStatus.Active)
                .OrderBy(x => x.Code)
                .Select(x => (Guid?)x.LocationId)
                .FirstOrDefaultAsync(cancellationToken);
            locationId ??= await CreateLocationAsync("SELLER-DEFAULT", "Default seller warehouse", cancellationToken);
            stockItemId = await OpenPositionAsync(request.OfferId, locationId.Value, cancellationToken);
        }

        await AdjustAsync(
            stockItemId.Value, StockAdjustmentKind.Set, request.OnHand,
            string.IsNullOrWhiteSpace(request.Reason) ? "seller-panel-adjust" : request.Reason.Trim(),
            null, cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, InventoryAvailability>> GetAvailabilityBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        if (offerIds is null || offerIds.Count == 0)
        {
            return new Dictionary<Guid, InventoryAvailability>();
        }

        var distinct = offerIds.Distinct().ToArray();
        var rows = await (
            from position in _db.Positions.AsNoTracking()
            join location in _db.Locations.AsNoTracking() on position.LocationId equals location.LocationId
            where distinct.Contains(position.OfferId)
            select new { position, location }).ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.position.OfferId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var locations = group.Select(row => new LocationAvailability(
                        row.position.StockItemId,
                        row.location.LocationId,
                        row.location.Code,
                        row.position.OnHand,
                        row.position.Reserved,
                        row.position.OnHand - row.position.Reserved)).ToList();
                    return new InventoryAvailability(
                        group.Key,
                        group.First().position.CatalogVariantId,
                        locations.Sum(x => x.OnHand),
                        locations.Sum(x => x.Reserved),
                        locations.Sum(x => x.Available),
                        locations);
                });
    }

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
            ?? throw new InvalidOperationException("domain.invariant");
        if (await FindVariantAsync(offer.CatalogVariantId, cancellationToken) is null)
        {
            throw new InvalidOperationException("inventory.catalog_variant.missing");
        }

        if (await _db.Locations.SingleOrDefaultAsync(x => x.LocationId == locationId, cancellationToken) is not { Status: InventoryLocationStatus.Active })
        {
            throw new InvalidOperationException("domain.invariant");
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
            throw new InvalidOperationException("domain.invariant");
        }

        if (quantity < 0)
        {
            throw new InvalidOperationException("inventory.adjustment.quantity_invalid");
        }

        var position = await _db.Positions.SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken)
            ?? throw new InvalidOperationException("domain.invariant");

        var delta = kind switch
        {
            StockAdjustmentKind.Increase => quantity,
            StockAdjustmentKind.Decrease => -quantity,
            StockAdjustmentKind.Set => quantity - position.OnHand,
            _ => throw new InvalidOperationException("inventory.adjustment.kind_unknown"),
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
            throw new InvalidOperationException("domain.invariant");
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
            throw new InvalidOperationException("domain.invariant");
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
            throw new InvalidOperationException("inventory.reservation.conflict");
        }

        return new ReservationReceipt(hold.ReservationId, stockItemId, position.OfferId, quantity, hold.Status, hold.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task<int> ReleaseExpiredHoldsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var limit = Math.Max(1, batchSize);
        var total = 0;
        while (true)
        {
            var released = await ReleaseExpiredBatchAsync(utcNow, limit, cancellationToken).ConfigureAwait(false);
            total += released;
            if (released < limit)
            {
                break;
            }
        }

        return total;
    }

    private async Task<int> ReleaseExpiredBatchAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var reservationIds = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT r.reservation_id AS "Value"
                 FROM inventory.reservations AS r
                 WHERE r.status = 'Held'
                   AND r.expires_at IS NOT NULL
                   AND r.expires_at <= {utcNow}
                 ORDER BY r.expires_at
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (reservationIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }

        foreach (var reservationId in reservationIds)
        {
            await ReleaseAsync(reservationId, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return reservationIds.Count;
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
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
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
            throw new InvalidOperationException("inventory.reservation.release_mismatch");
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
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
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
            throw new InvalidOperationException("domain.invariant");
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
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
        reservation.CommitForPaidOrder(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await FindReservationAsync(reservationId, cancellationToken)
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
    }

    /// <inheritdoc />
    public async Task<ReservationReceipt> PromoteReservationForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
        reservation.PromoteForManualPaymentReview(reviewExpiresAt, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return await FindReservationAsync(reservationId, cancellationToken)
            ?? throw new InvalidOperationException("inventory.reservation.not_found");
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

    /// <inheritdoc />
    public async Task<EnsureOrderSupplyResult> EnsureOrderSupplyAsync(
        EnsureOrderSupplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines is null || request.Lines.Count == 0)
        {
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.NotApplicable,
                OrderSupplyStatusKind.NotApplicable,
                [],
                new Dictionary<Guid, Guid>());
        }

        if (request.Lines.All(x => x.RemainingQuantity <= 0))
        {
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.AlreadyReserved,
                OrderSupplyStatusKind.Fulfilled,
                [],
                new Dictionary<Guid, Guid>());
        }

        var requireDurable = request.Mode is OrderSupplyMode.EnsurePaidDurable or OrderSupplyMode.EnsureFulfillmentSupply;
        var evaluation = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);

        if (request.Mode == OrderSupplyMode.CheckOnly)
        {
            var checkOutcome = evaluation.Status switch
            {
                OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled => OrderSupplyOutcome.AlreadyReserved,
                OrderSupplyStatusKind.Unavailable => OrderSupplyOutcome.Unavailable,
                OrderSupplyStatusKind.PartiallyUnavailable => OrderSupplyOutcome.PartiallyUnavailable,
                OrderSupplyStatusKind.AvailableForReacquire => OrderSupplyOutcome.AlreadyReserved,
                _ => OrderSupplyOutcome.NotApplicable,
            };
            return new EnsureOrderSupplyResult(
                checkOutcome,
                evaluation.Status,
                evaluation.Lines,
                new Dictionary<Guid, Guid>());
        }

        if (evaluation.Status is OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled)
        {
            // Promote/commit in place when needed without new reservation.
            if (IsTimedHold(request.Mode) && request.ReviewExpiresAt is { } reviewAt)
            {
                foreach (var line in evaluation.Lines.Where(x => x.BoundReservationId is not null && x.LineStatus == OrderSupplyStatusKind.Reserved))
                {
                    await PromoteReservationForManualPaymentReviewAsync(line.BoundReservationId!.Value, reviewAt, cancellationToken);
                }
            }

            if (requireDurable)
            {
                foreach (var line in evaluation.Lines.Where(x => x.BoundReservationId is not null && x.LineStatus == OrderSupplyStatusKind.Reserved))
                {
                    var boundId = line.BoundReservationId!.Value;
                    var bound = await FindReservationAsync(boundId, cancellationToken);
                    if (bound is { Status: StockReservationStatus.Held })
                    {
                        await CommitReservationForPaidOrderAsync(boundId, cancellationToken);
                    }
                }
            }

            evaluation = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);
            if (evaluation.Status is OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled)
            {
                return new EnsureOrderSupplyResult(
                    OrderSupplyOutcome.AlreadyReserved,
                    evaluation.Status,
                    evaluation.Lines,
                    new Dictionary<Guid, Guid>());
            }
        }

        if (!request.AllowReacquire
            || evaluation.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable
            || evaluation.Status is OrderSupplyStatusKind.NotApplicable)
        {
            return new EnsureOrderSupplyResult(
                MapOutcome(evaluation.Status, mutated: false),
                evaluation.Status,
                evaluation.Lines,
                new Dictionary<Guid, Guid>());
        }

        // AvailableForReacquire (or mixed reserved+available): reacquire ALL remaining lines that are not already secured.
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var acquired = new List<Guid>();
        var bindings = new Dictionary<Guid, Guid>();
        try
        {
            foreach (var input in request.Lines.Where(x => x.RemainingQuantity > 0))
            {
                var existingEval = evaluation.Lines.FirstOrDefault(x => x.OrderLineId == input.OrderLineId);
                if (existingEval is { LineStatus: OrderSupplyStatusKind.Reserved, BoundReservationId: not null })
                {
                    if (requireDurable)
                    {
                        await CommitReservationForPaidOrderAsync(existingEval.BoundReservationId.Value, cancellationToken);
                    }
                    else if (IsTimedHold(request.Mode) && request.ReviewExpiresAt is { } reviewAt)
                    {
                        await PromoteReservationForManualPaymentReviewAsync(
                            existingEval.BoundReservationId.Value,
                            reviewAt,
                            cancellationToken);
                    }

                    bindings[input.OrderLineId] = existingEval.BoundReservationId.Value;
                    continue;
                }

                var stockItemId = await ResolveStockItemIdAsync(input, cancellationToken)
                    ?? throw new InvalidOperationException("inventory.manual_review.unavailable");
                DateTimeOffset? expiresAt = IsTimedHold(request.Mode)
                    ? request.ReviewExpiresAt
                    : null;
                // idempotency_key column is varchar(128); keep this compact.
                var idempotencyKey =
                    $"os-{(int)request.Mode}-{input.OrderLineId:N}-{request.CheckoutId:N}-{_clock.UtcNow.UtcTicks}";
                var receipt = await ReserveAsync(
                    stockItemId,
                    input.RemainingQuantity,
                    $"order-supply-{input.OrderLineId:N}",
                    idempotencyKey,
                    expiresAt,
                    cancellationToken);
                acquired.Add(receipt.ReservationId);
                if (requireDurable)
                {
                    await CommitReservationForPaidOrderAsync(receipt.ReservationId, cancellationToken);
                }

                bindings[input.OrderLineId] = receipt.ReservationId;
            }

            var refreshed = await EvaluateLinesAsync(
                request.Lines.Select(l => bindings.TryGetValue(l.OrderLineId, out var rid)
                    ? l with { CurrentReservationId = rid }
                    : l).ToArray(),
                requireDurable,
                cancellationToken);
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.Reacquired,
                refreshed.Status,
                refreshed.Lines,
                bindings);
        }
        catch (InvalidOperationException)
        {
            foreach (var id in acquired)
            {
                await ReleaseAsync(id, cancellationToken);
            }

            var failed = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.Unavailable,
                OrderSupplyStatusKind.Unavailable,
                failed.Lines,
                new Dictionary<Guid, Guid>());
        }
    }

    private async Task<(OrderSupplyStatusKind Status, IReadOnlyList<OrderSupplyLineShortage> Lines)> EvaluateLinesAsync(
        IReadOnlyList<OrderSupplyLineInput> lines,
        bool requireDurable,
        CancellationToken cancellationToken)
    {
        var results = new List<OrderSupplyLineShortage>();
        foreach (var line in lines)
        {
            if (line.RemainingQuantity <= 0)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    0,
                    0,
                    0,
                    OrderSupplyStatusKind.Fulfilled,
                    line.CurrentReservationId));
                continue;
            }

            ReservationReceipt? existing = null;
            if (line.CurrentReservationId is { } reservationId)
            {
                existing = await FindReservationAsync(reservationId, cancellationToken);
            }

            var heldValid = existing is { Status: StockReservationStatus.Held }
                && existing.Quantity >= line.RemainingQuantity
                && (existing.ExpiresAt is null || existing.ExpiresAt > _clock.UtcNow);

            if (heldValid)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    line.RemainingQuantity,
                    0,
                    OrderSupplyStatusKind.Reserved,
                    existing!.ReservationId));
                continue;
            }

            var available = await ResolveAvailableAsync(line, cancellationToken);
            if (available >= line.RemainingQuantity)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    available,
                    0,
                    OrderSupplyStatusKind.AvailableForReacquire,
                    null));
            }
            else
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    available,
                    line.RemainingQuantity - available,
                    OrderSupplyStatusKind.Unavailable,
                    null));
            }
        }

        var active = results.Where(x => x.LineStatus != OrderSupplyStatusKind.Fulfilled).ToList();
        if (active.Count == 0)
        {
            return (OrderSupplyStatusKind.Fulfilled, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.Reserved))
        {
            return (OrderSupplyStatusKind.Reserved, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.AvailableForReacquire))
        {
            return (OrderSupplyStatusKind.AvailableForReacquire, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.Unavailable))
        {
            return (OrderSupplyStatusKind.Unavailable, results);
        }

        return (OrderSupplyStatusKind.PartiallyUnavailable, results);
    }

    private async Task<decimal> ResolveAvailableAsync(OrderSupplyLineInput line, CancellationToken cancellationToken)
    {
        if (line.CurrentReservationId is { } rid)
        {
            var existing = await FindReservationAsync(rid, cancellationToken);
            if (existing is not null)
            {
                var position = await _db.Positions.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.StockItemId == existing.StockItemId, cancellationToken);
                if (position is not null)
                {
                    return Math.Max(0, position.OnHand - position.Reserved);
                }
            }
        }

        var availability = await GetAvailabilityAsync(line.OfferId, cancellationToken);
        return availability?.Available ?? 0m;
    }

    private async Task<Guid?> ResolveStockItemIdAsync(OrderSupplyLineInput line, CancellationToken cancellationToken)
    {
        if (line.CurrentReservationId is { } rid)
        {
            var existing = await FindReservationAsync(rid, cancellationToken);
            if (existing is not null)
            {
                return existing.StockItemId;
            }
        }

        var availability = await GetAvailabilityAsync(line.OfferId, cancellationToken);
        return availability?.Locations.OrderByDescending(x => x.Available).FirstOrDefault()?.StockItemId;
    }

    private static bool IsTimedHold(OrderSupplyMode mode) =>
        mode is OrderSupplyMode.EnsureReviewHold or OrderSupplyMode.EnsureUnpaidRetryHold;

    private static OrderSupplyOutcome MapOutcome(OrderSupplyStatusKind status, bool mutated) =>
        status switch
        {
            OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled =>
                mutated ? OrderSupplyOutcome.Reacquired : OrderSupplyOutcome.AlreadyReserved,
            OrderSupplyStatusKind.AvailableForReacquire => OrderSupplyOutcome.AlreadyReserved,
            OrderSupplyStatusKind.Unavailable => OrderSupplyOutcome.Unavailable,
            OrderSupplyStatusKind.PartiallyUnavailable => OrderSupplyOutcome.PartiallyUnavailable,
            _ => OrderSupplyOutcome.NotApplicable,
        };

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

    private async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        using var trace = _tracer.Begin("Inventory", "Offer", "LookupOffer");
        try
        {
            var offer = await _offers.FindOfferAsync(offerId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return offer;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    private async Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        using var trace = _tracer.Begin("Inventory", "Catalog", "LookupVariant");
        try
        {
            var variant = await _catalog.FindVariantAsync(variantId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return variant;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }
}

