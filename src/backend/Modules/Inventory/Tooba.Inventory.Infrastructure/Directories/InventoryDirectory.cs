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
public sealed partial class InventoryDirectory : IInventoryDirectory, IInventoryAvailabilityGateway, ISellerOfferInventoryGateway, IFulfillmentInventoryLifecyclePort, Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort, IInventoryPaymentHoldGate, IInventoryPaymentHoldExtensionPort, Tooba.Inventory.Contracts.Replacement.IDamagedReplacementInventoryPort, ISellerOfferAllocationGateway, IOfferInventoryHistoryReader
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly ICatalogVariantLookup _catalog;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IModuleCallTracer _tracer;
    private readonly ICurrentCommerceContext? _commerce;

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
        IModuleCallTracer tracer,
        ICurrentCommerceContext? commerce = null)
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
        _commerce = commerce;
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
        var ownerKind = _commerce?.Current switch
        {
            { Edition.Edition: ToobaEdition.SingleStore, Tenant.Status: TenantStatus.Active } => InventoryLocationOwnerKind.Store,
            { Edition.Edition: ToobaEdition.Marketplace, Tenant: null } => InventoryLocationOwnerKind.Marketplace,
            null when _commerce is null => InventoryLocationOwnerKind.Store,
            _ => throw new ContractOperationException(InventoryErrorCodes.LocationOwnerInvalid),
        };
        var location = InventoryLocation.Create(_ids.NewId(), code, name, _clock.UtcNow, ownerKind, null);
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

        if (await _db.Locations.SingleOrDefaultAsync(x => x.LocationId == locationId, cancellationToken) is not { Status: InventoryLocationStatus.Active } location)
        {
            throw new ContractOperationException(InventoryErrorCodes.LocationNotFound);
        }

        if (_commerce is not null && !IsLocationEligible(location, offer))
            throw new ContractOperationException(InventoryErrorCodes.LocationNotEligible);

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

        var known = await _db.Positions.AsNoTracking().SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.PositionNotFound);
        await using var transaction = _db.Database.CurrentTransaction is null
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await LockOfferAllocationAsync(known.OfferId, cancellationToken);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == stockItemId, cancellationToken);
        await _db.Entry(position).ReloadAsync(cancellationToken);

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
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) =>
        ReserveCoreAsync(stockItemId, quantity, externalReference, idempotencyKey, expiresAt,
            StockReservationPurpose.NewSale, cancellationToken);

    private async Task<ReservationReceipt> ReserveCoreAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        StockReservationPurpose purpose,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (quantity <= 0)
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        var knownPosition = await _db.Positions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.StockItemId == stockItemId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        var location = await _db.Locations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.LocationId == knownPosition.LocationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        if (location.Status != InventoryLocationStatus.Active)
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        var offer = await FindOfferAsync(knownPosition.OfferId, cancellationToken);
        var marketplace = _commerce?.Current?.Edition.Edition == ToobaEdition.Marketplace;
        if (_commerce is not null && (offer is null || !IsLocationEligible(location, offer)))
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        var needsAllocation = location.OwnerKind == InventoryLocationOwnerKind.Marketplace
            || (marketplace && offer?.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform);
        if (marketplace && (offer is null
            || (offer.FulfillmentResponsibility == OfferFulfillmentResponsibility.Platform
                ? location.OwnerKind != InventoryLocationOwnerKind.Marketplace
                : offer.FulfillmentResponsibility != OfferFulfillmentResponsibility.Seller
                    || location.OwnerKind != InventoryLocationOwnerKind.Seller
                    || location.SellerPartyId != offer.SellerPartyId)))
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);

        var now = _clock.UtcNow;
        await using var allocationTransaction = _db.Database.CurrentTransaction is null
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await LockOfferAllocationAsync(knownPosition.OfferId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var prior = await _db.Reservations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);
            if (prior is not null)
            {
                if (prior.Status != StockReservationStatus.Held || prior.StockItemId != stockItemId
                    || (prior.Purpose ?? StockReservationPurpose.NewSale) != purpose
                    || prior.Quantity != quantity || prior.ExternalReference != (string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim()))
                    throw new ContractOperationException(InventoryErrorCodes.ReservationConflict);
                return new ReservationReceipt(prior.ReservationId, stockItemId, knownPosition.OfferId,
                    prior.Quantity, prior.Status, prior.ExpiresAt);
            }
        }
        var allocation = await _db.OfferAllocations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == knownPosition.OfferId, cancellationToken);
        if (purpose == StockReservationPurpose.NewSale && needsAllocation && allocation is null)
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        if (allocation is not null && purpose == StockReservationPurpose.NewSale)
        {
            var allocationRevision = _ids.NewId();
            var allocationReserved = await _db.OfferAllocations
                .Where(x => x.OfferId == knownPosition.OfferId && x.Quantity - x.Held >= quantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Held, x => x.Held + quantity)
                        .SetProperty(x => x.Revision, allocationRevision)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            if (allocationReserved != 1)
                throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        }

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
        await _db.Entry(position).ReloadAsync(cancellationToken);
        var hold = StockReservation.Hold(_ids.NewId(), stockItemId, quantity, externalReference, idempotencyKey, now, expiresAt, purpose);
        _db.Reservations.Add(hold);
        position.RecordReserved(hold.ReservationId, quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            if (allocationTransaction is not null)
                await allocationTransaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsReservationIdempotencyConflict(ex))
        {
            if (allocationTransaction is not null)
                await allocationTransaction.RollbackAsync(cancellationToken);
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

    private Task<ReservationConsumptionRecord?> FindConsumptionAsync(Guid reservationId, CancellationToken cancellationToken) =>
        _db.ReservationConsumptions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);

    private static bool IsProvenConsumption(
        ReservationConsumptionRecord record,
        string consumerKey,
        Guid stockItemId,
        decimal quantity) =>
        string.Equals(record.ConsumerKey, consumerKey, StringComparison.Ordinal)
        && record.StockItemId == stockItemId
        && record.Quantity == quantity;

    private static bool IsReservationConsumptionConflict(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(pg.ConstraintName, "pk_reservation_consumptions", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
