using Tooba.Fulfillment.Contracts.Shipping;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Order.Contracts.Fulfillment;
using TrackingReference = Tooba.Fulfillment.Contracts.Operations.FulfillmentInternalTrackingReference;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد Fulfillment.
/// </summary>
public sealed class OpenFulfillmentUseCaseGuard : IFulfillmentUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون fulfillment در schema fulfillment.
/// </summary>
public sealed partial class FulfillmentDirectory : IFulfillmentDirectory
{
    private readonly FulfillmentDbContext _db;
    private readonly IFulfillmentUseCaseGuard _guard;
    private readonly IOrderFulfillmentReader _orders;
    private readonly IFulfillmentInventoryGateway _inventory;
    private readonly FulfillmentInstrumentation _telemetry;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema fulfillment و درز Order/Inventory وصل می‌کند.
    /// </summary>
    public FulfillmentDirectory(
        FulfillmentDbContext db,
        IFulfillmentUseCaseGuard guard,
        IOrderFulfillmentReader orders,
        IFulfillmentInventoryGateway inventory,
        FulfillmentInstrumentation telemetry,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _inventory = inventory;
        _telemetry = telemetry;
        _clock = clock;
        _ids = ids;
    }

    /// <summary>
    /// از رویداد payment.succeeded با dedup inbox، fulfillment idempotent می‌سازد.
    /// </summary>
    public async Task CreateFromPaidSellerOrdersAsync(
        Guid paymentId,
        Guid eventId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (await _db.PaymentInbox.AnyAsync(x => x.EventId == eventId, cancellationToken))
        {
            return;
        }

        var now = _clock.UtcNow;
        foreach (var sellerOrderId in sellerOrderIds.Distinct())
        {
            if (await _db.Fulfillments.AnyAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken))
            {
                continue;
            }

            var handoff = await _orders.GetHandoffAsync(sellerOrderId, cancellationToken)
                ?? throw new ContractOperationException("fulfillment.order.not_found");
            if (!handoff.IsPaid)
            {
                throw new ContractOperationException("fulfillment.order.not_paid");
            }

            var unit = FulfillmentUnit.CreateFromPaidOrder(
                _ids.NewId(),
                () => _ids.NewId(),
                handoff.SellerOrderId,
                handoff.CheckoutId,
                handoff.SellerPartyId,
                handoff.PlacedByUserId,
                handoff.RecipientName,
                handoff.ContactMobile,
                handoff.ProvinceName,
                handoff.CityName,
                handoff.PostalAddress,
                handoff.PostalCode,
                handoff.ShippingMethodCode,
                handoff.ShippingMethodLabel,
                handoff.Lines.Select(x => (x.OrderLineId, x.Quantity, x.ReservationId)),
                now,
                MapShippingGroupPlans(handoff));
            _db.Fulfillments.Add(unit);
            _db.Items.AddRange(unit.Items);
            PersistShippingGroups(unit);

            _telemetry.RecordCreated();
        }

        _db.PaymentInbox.Add(new FulfillmentPaymentInboxRecord
        {
            EventId = eventId,
            PaymentId = paymentId,
            ProcessedAt = now,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken)
    {
        var unit = await _db.Fulfillments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.FulfillmentId == fulfillmentId, cancellationToken);
        return unit is null ? null : await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot?> GetBySellerOrderAsync(Guid sellerOrderId, CancellationToken cancellationToken)
    {
        var unit = await _db.Fulfillments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken);
        return unit is null ? null : await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FulfillmentSnapshot>> ListForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var units = await _db.Fulfillments.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var results = new List<FulfillmentSnapshot>(units.Count);
        foreach (var unit in units)
        {
            results.Add(await MapSnapshotAsync(unit, cancellationToken));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FulfillmentSnapshot>> ListAllAsync(CancellationToken cancellationToken)
    {
        var units = await _db.Fulfillments.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var results = new List<FulfillmentSnapshot>(units.Count);
        foreach (var unit in units)
        {
            results.Add(await MapSnapshotAsync(unit, cancellationToken));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var units = await _db.Fulfillments.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var results = new List<FulfillmentSnapshot>(units.Count);
        foreach (var unit in units)
        {
            results.Add(await MapSnapshotAsync(unit, cancellationToken));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> MarkProcessingAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.MarkProcessing(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("processing");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> ProcessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.ProcessSelections(
            selections.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("processing");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> UnprocessSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.UnprocessSelections(
            selections.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("unprocessed");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> MarkPackedAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.MarkPacked(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("packed");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> PackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.PackSelections(
            selections.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("packed");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> UnpackSelectionsAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        IReadOnlyList<FulfillmentSelectionCommand> selections,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.UnpackSelections(
            selections.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("unpacked");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? providerMetadataJson = null,
        Guid? shippingGroupId = null,
        string? shippingDispatchKey = null)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var methodCode = shippingMethodCode?.Trim();
        string? normalizedMetadata = null;
        string methodLabel = carrierDisplayName;
        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var definition = ShippingMethodRegistry.Find(methodCode)
                ?? throw new ContractOperationException("fulfillment.shipping_method.unsupported");
            methodLabel = definition.LabelFa;
            if (string.IsNullOrWhiteSpace(carrierDisplayName))
            {
                carrierDisplayName = definition.LabelFa;
            }

            normalizedMetadata = ShippingProviderMetadataValidator.ValidateAndNormalize(definition.Code, providerMetadataJson);
            methodCode = definition.Code;
        }

        var normalizedDispatchKey = string.IsNullOrWhiteSpace(shippingDispatchKey)
            ? null
            : shippingDispatchKey.Trim();
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var knownIds = unit.Shipments.Select(x => x.ShipmentId).ToHashSet();
        var shipment = unit.CreateShipment(
            _ids.NewId(),
            () => _ids.NewId(),
            carrierDisplayName,
            items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow,
            methodCode,
            methodLabel,
            normalizedMetadata,
            normalizedMetadata is null ? 0 : 1,
            shippingGroupId,
            normalizedDispatchKey);
        if (!knownIds.Contains(shipment.ShipmentId))
        {
            _db.Shipments.Add(shipment);
            _db.ShipmentItems.AddRange(shipment.Items);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException) when (normalizedDispatchKey is not null)
            {
                // Concurrent first-insert race on unique dispatch key: reload and reuse winner.
                _db.ChangeTracker.Clear();
                unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
                shipment = unit.CreateShipment(
                    shipment.ShipmentId,
                    () => _ids.NewId(),
                    carrierDisplayName,
                    items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
                    _clock.UtcNow,
                    methodCode,
                    methodLabel,
                    normalizedMetadata,
                    normalizedMetadata is null ? 0 : 1,
                    shippingGroupId,
                    normalizedDispatchKey);
                var snapshotRace = await MapSnapshotAsync(unit, cancellationToken);
                return new FulfillmentShipmentCreateResult(snapshotRace, shipment.ShipmentId);
            }

            _telemetry.RecordShipmentCreated();
        }

        var snapshot = await MapSnapshotAsync(unit, cancellationToken);
        return new FulfillmentShipmentCreateResult(snapshot, shipment.ShipmentId);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> CancelShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        // TB-262 RETENTION RULE («تا سفارش لغو نشده نباید آزاد بشه رزرو»): cancelling a replacement parcel detaches
        // ONLY that parcel allocation. Its purpose-bound healthy-stock reservation stays held for the ACTIVE order,
        // so saleable availability does not increase and the next legitimate attempt rebinds the same allocation.
        unit.CancelShipment(shipmentId, _clock.UtcNow);
        unit.RetainReplacementStockForCancelledParcel(shipmentId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        _telemetry.RecordTransition("shipment_cancelled");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> AssignTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        return await AssignTrackingCoreAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            trackingReference,
            cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// TB-257: the candidate is owned by the server and is derived from the whole parcel identity, so a
    /// deterministic operator "auto" value can never be shared by two parcels. The real uniqueness scope is the
    /// database unique index; a collision (another parcel already owns the candidate) moves to the next bounded
    /// candidate inside this same scoped context instead of returning the same failing value. A parcel that
    /// already carries a code is reconciled unchanged — a deliberate retry, double click or lost response never
    /// replaces a real reference.
    /// </remarks>
    public async Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == shipmentId)
            ?? throw new ContractOperationException("fulfillment.shipment.not_found");

        if (!string.IsNullOrWhiteSpace(shipment.TrackingReference))
        {
            // Reconcile the existing assignment; never silently replace a real reference.
            return await MapSnapshotAsync(unit, cancellationToken);
        }

        var originalUnitUpdatedAt = unit.UpdatedAt;
        for (var attempt = 0; attempt < TrackingReference.MaxAllocationAttempts; attempt++)
        {
            var candidate = TrackingReference.ForShipment(shipmentId, attempt);
            var normalized = candidate.ToLowerInvariant();
            var taken = await _db.Shipments.AsNoTracking()
                .AnyAsync(
                    x => x.ShipmentId != shipmentId
                        && x.TrackingReference != null
                        && x.TrackingReference.ToLower() == normalized,
                    cancellationToken);
            if (taken)
            {
                continue;
            }

            unit.AssignTracking(shipmentId, candidate, _clock.UtcNow);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsTrackingReferenceUniqueViolation(ex))
            {
                // A concurrent writer won the same candidate after our read; drop only this assignment and try
                // the next bounded candidate. Unrelated persistence failures are never swallowed.
                RestoreUntrackedShipment(unit, shipment, originalUnitUpdatedAt);
                continue;
            }

            _telemetry.RecordTrackingAssigned();
            return await MapSnapshotAsync(unit, cancellationToken);
        }

        throw new ContractOperationException("fulfillment.tracking.duplicate");
    }

    /// <summary>
    /// Reverts only the failed tracking assignment so the next bounded candidate can be attempted in this same
    /// tracked context. Nothing else of the unit is altered by this operation.
    /// </summary>
    private void RestoreUntrackedShipment(FulfillmentUnit unit, Shipment shipment, DateTimeOffset unitUpdatedAt)
    {
        var shipmentEntry = _db.Entry(shipment);
        shipmentEntry.Property(x => x.TrackingReference).CurrentValue = null;
        shipmentEntry.Property(x => x.TrackingReference).IsModified = true;
        var unitEntry = _db.Entry(unit);
        unitEntry.Property(x => x.UpdatedAt).CurrentValue = unitUpdatedAt;
        unitEntry.Property(x => x.UpdatedAt).IsModified = true;
    }

    /// <summary>
    /// Exact uniqueness fault of <c>ix_shipments_tracking_reference</c> only (SQLSTATE 23505 on that index);
    /// lookalike indexes and every other <see cref="DbUpdateException"/> keep their original meaning.
    /// </summary>
    private static bool IsTrackingReferenceUniqueViolation(DbUpdateException exception)
        => exception.InnerException is Npgsql.PostgresException
        {
            SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ix_shipments_tracking_reference",
        };

    private async Task<FulfillmentSnapshot> AssignTrackingCoreAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var normalized = trackingReference.Trim();
        var duplicate = await _db.Shipments.AsNoTracking()
            .AnyAsync(
                x => x.ShipmentId != shipmentId
                    && x.TrackingReference != null
                    && x.TrackingReference.ToLower() == normalized.ToLower(),
                cancellationToken);
        if (duplicate)
        {
            throw new ContractOperationException("fulfillment.tracking.duplicate");
        }

        unit.AssignTracking(shipmentId, normalized, _clock.UtcNow);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new ContractOperationException("fulfillment.tracking.duplicate", ex);
        }

        _telemetry.RecordTrackingAssigned();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> CorrectTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        string trackingReference,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var normalized = trackingReference.Trim();
        var duplicate = await _db.Shipments.AsNoTracking()
            .AnyAsync(
                x => x.ShipmentId != shipmentId
                    && x.TrackingReference != null
                    && x.TrackingReference.ToLower() == normalized.ToLower(),
                cancellationToken);
        if (duplicate)
        {
            throw new ContractOperationException("fulfillment.tracking.duplicate");
        }

        unit.CorrectTracking(shipmentId, normalized, _clock.UtcNow);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new ContractOperationException("fulfillment.tracking.duplicate", ex);
        }

        _telemetry.RecordTrackingAssigned();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> DispatchShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        return await DispatchShipmentCoreAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureShipmentNotLockedByPackageAsync(shipmentId, cancellationToken);
        return await DeliverShipmentCoreAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<FulfillmentSnapshot> DispatchShipmentCoreAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.Shipments.Single(x => x.ShipmentId == shipmentId);
        if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
        {
            return await MapSnapshotAsync(unit, cancellationToken);
        }

        unit.ApplyShipmentDispatched(shipmentId, _clock.UtcNow);
        if (shipment.IsReplacement)
        {
            // TB-262: the replacement consumes its OWN new healthy-stock reservation exactly once. The original sale
            // reservation of these units was already consumed by the first dispatch and is never touched again.
            if (shipment.ReplacementReservationId is { } replacementReservationId)
            {
                var resolution = shipment.DamagedResolutionId is { } resolutionId
                    ? unit.FindDamagedResolution(resolutionId)
                    : null;
                await _inventory.ConsumeReplacementReservationAsync(
                    replacementReservationId,
                    resolution?.ReservationConsumerKey ?? $"damage-replacement:{replacementReservationId:N}",
                    cancellationToken);
            }
        }
        else if (!shipment.IsResend)
        {
            // TB-258: the reservation was already consumed for the first dispatch of these goods. A resend must not
            // consume/commit the same reservation a second time.
            await ConsumeInventoryForShipmentAsync(unit, shipment, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordDispatched();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    private async Task<FulfillmentSnapshot> DeliverShipmentCoreAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.Shipments.Single(x => x.ShipmentId == shipmentId);
        if (shipment.Status == ShipmentStatus.Delivered)
        {
            return await MapSnapshotAsync(unit, cancellationToken);
        }

        unit.ApplyShipmentDelivered(shipmentId, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordDelivered();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    private async Task ConsumeInventoryForShipmentAsync(
        FulfillmentUnit unit,
        Shipment shipment,
        CancellationToken cancellationToken,
        Guid? consolidatedPackageId = null)
    {
        foreach (var shipmentLine in shipment.Items)
        {
            var fulfillmentItem = unit.Items.Single(x => x.OrderLineId == shipmentLine.OrderLineId);
            if (fulfillmentItem.ReservationId is null || fulfillmentItem.ReservationConsumed)
            {
                continue;
            }

            if (fulfillmentItem.QuantityShipped >= fulfillmentItem.QuantityOrdered)
            {
                await ConsumeReservationAsync(fulfillmentItem, shipment, consolidatedPackageId, cancellationToken);
                fulfillmentItem.MarkReservationConsumed();
            }
        }
    }

    private async Task<FulfillmentUnit> LoadMutableAsync(Guid fulfillmentId, CancellationToken cancellationToken)    {
        var unit = await _db.Fulfillments.SingleOrDefaultAsync(x => x.FulfillmentId == fulfillmentId, cancellationToken)
            ?? throw new ContractOperationException("fulfillment.not_found");
        var items = await _db.Items.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        foreach (var shipment in shipments)
        {
            var shipmentItems = await _db.ShipmentItems.Where(x => x.ShipmentId == shipment.ShipmentId).ToListAsync(cancellationToken);
            shipment.AttachLoadedItems(shipmentItems);
        }

        var shippingGroups = await _db.ShippingGroups.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        var receipts = await _db.ShipmentReturnReceipts.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        foreach (var receipt in receipts)
        {
            var receiptItems = await _db.ShipmentReturnReceiptItems
                .Where(x => x.ShipmentReturnReceiptId == receipt.ShipmentReturnReceiptId)
                .ToListAsync(cancellationToken);
            receipt.AttachLoadedItems(receiptItems);
        }

        unit.AttachLoadedItems(items);
        unit.AttachLoadedShipments(shipments);
        unit.AttachLoadedShippingGroups(shippingGroups);
        unit.AttachLoadedReturnReceipts(receipts);
        // TB-262: durable damaged-goods decisions are loaded through their own DbSet, exactly like receipts.
        var resolutions = await _db.DamagedResolutions
            .Where(x => x.FulfillmentId == fulfillmentId)
            .ToListAsync(cancellationToken);
        unit.AttachLoadedDamagedResolutions(resolutions);
        // TB-265: still-binding courier-origin Returns claims are capacity already promised to a case, so the
        // aggregate must never grant it again to a resend or a replacement.
        var claims = await _db.CourierReturnClaims.AsNoTracking()
            .Where(x => x.FulfillmentId == fulfillmentId
                        && (x.Status == CourierReturnClaimStatus.Requested || x.Status == CourierReturnClaimStatus.Approved))
            .ToListAsync(cancellationToken);
        unit.AttachLoadedReturnCapacityClaims(claims.SelectMany(x => x.Lines.Select(l =>
            new ReturnedCapacityClaim(x.ShipmentId, l.OrderLineId, l.Quantity, l.Condition))).ToArray());
        return unit;
    }

    /// <inheritdoc />
    public async Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return;
        }

        var ids = units.Select(x => x.FulfillmentId).ToList();
        var items = await _db.Items.Where(x => ids.Contains(x.FulfillmentId)).ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.Where(x => ids.Contains(x.FulfillmentId)).ToListAsync(cancellationToken);
        if (units.Any(x => x.Status != FulfillmentStatus.ReadyToFulfill)
            || items.Any(x => x.QuantityPacked > 0 || x.QuantityProcessing > 0)
            || shipments.Any(x => x.Status != ShipmentStatus.Cancelled))
        {
            throw new ContractOperationException("fulfillment.unconfirm.already_started");
        }

        var shipmentIds = shipments.Select(x => x.ShipmentId).ToList();
        var shipmentItems = shipmentIds.Count == 0
            ? []
            : await _db.ShipmentItems.Where(x => shipmentIds.Contains(x.ShipmentId)).ToListAsync(cancellationToken);
        _db.ShipmentItems.RemoveRange(shipmentItems);
        _db.Shipments.RemoveRange(shipments);
        _db.Items.RemoveRange(items);
        _db.Fulfillments.RemoveRange(units);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureCreatedForPaidCheckoutAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        _ = checkoutId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var now = _clock.UtcNow;
        var created = false;
        foreach (var sellerOrderId in sellerOrderIds.Distinct())
        {
            if (await _db.Fulfillments.AnyAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken))
            {
                continue;
            }

            var handoff = await _orders.GetHandoffAsync(sellerOrderId, cancellationToken)
                ?? throw new ContractOperationException("fulfillment.order.not_found");
            if (!handoff.IsPaid)
            {
                throw new ContractOperationException("fulfillment.order.not_paid");
            }

            foreach (var line in handoff.Lines)
            {
                if (line.ReservationId is not { } reservationId)
                {
                    continue;
                }

                await _inventory.CommitReservationForPaidOrderAsync(reservationId, cancellationToken);
            }

            var unit = FulfillmentUnit.CreateFromPaidOrder(
                _ids.NewId(),
                () => _ids.NewId(),
                handoff.SellerOrderId,
                handoff.CheckoutId,
                handoff.SellerPartyId,
                handoff.PlacedByUserId,
                handoff.RecipientName,
                handoff.ContactMobile,
                handoff.ProvinceName,
                handoff.CityName,
                handoff.PostalAddress,
                handoff.PostalCode,
                handoff.ShippingMethodCode,
                handoff.ShippingMethodLabel,
                handoff.Lines.Select(x => (x.OrderLineId, x.Quantity, x.ReservationId)),
                now,
                MapShippingGroupPlans(handoff));
            _db.Fulfillments.Add(unit);
            _db.Items.AddRange(unit.Items);
            PersistShippingGroups(unit);

            _telemetry.RecordCreated();
            created = true;
        }

        if (created)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await VoidActivePackagesForCheckoutCancelAsync(checkoutId, cancellationToken);
        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        var loaded = new List<FulfillmentUnit>(units.Count);
        foreach (var unit in units)
        {
            loaded.Add(await LoadMutableAsync(unit.FulfillmentId, cancellationToken));
        }

        if (loaded.Any(x => x.HasDispatchedQuantity()))
        {
            throw new ContractOperationException("fulfillment.cancel.already_dispatched");
        }

        var releasedReplacementReservations = new List<Guid>();
        foreach (var unit in loaded)
        {
            unit.AbortForOrderCancel(now);
            // TB-262 RETENTION RULE: an authorized ORDER cancellation is the ONLY event that may release the
            // unconsumed healthy-stock reservations of this checkout's replacement decisions — exactly once.
            releasedReplacementReservations.AddRange(unit.ReleaseReplacementStockForOrderCancel(now));
        }

        await _db.SaveChangesAsync(cancellationToken);
        foreach (var reservationId in releasedReplacementReservations)
        {
            await _inventory.ReleaseReplacementReservationAsync(reservationId, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return;
        }

        var now = _clock.UtcNow;
        var loaded = new List<FulfillmentUnit>(units.Count);
        foreach (var unit in units)
        {
            loaded.Add(await LoadMutableAsync(unit.FulfillmentId, cancellationToken));
        }

        if (loaded.Any(x => x.HasDispatchedQuantity()))
        {
            throw new ContractOperationException("fulfillment.restore.already_dispatched");
        }

        foreach (var unit in loaded)
        {
            var handoff = await _orders.GetHandoffAsync(unit.SellerOrderId, cancellationToken)
                ?? throw new ContractOperationException("fulfillment.order.not_found");
            var reservations = handoff.Lines.ToDictionary(x => x.OrderLineId, x => x.ReservationId);
            unit.RebindActiveReservations(reservations);
            unit.ReactivateAfterOrderRestore(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return;
        }

        foreach (var unitRow in units)
        {
            var unit = await LoadMutableAsync(unitRow.FulfillmentId, cancellationToken);
            var handoff = await _orders.GetHandoffAsync(unit.SellerOrderId, cancellationToken)
                ?? throw new ContractOperationException("fulfillment.order.not_found");
            var reservations = handoff.Lines.ToDictionary(x => x.OrderLineId, x => x.ReservationId);
            unit.RebindActiveReservations(reservations);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

}
