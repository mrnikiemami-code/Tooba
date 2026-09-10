using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Application;

namespace Tooba.Fulfillment.Infrastructure;

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
public sealed class FulfillmentDirectory : IFulfillmentDirectory
{
    private readonly FulfillmentDbContext _db;
    private readonly IFulfillmentUseCaseGuard _guard;
    private readonly IOrderFulfillmentReader _orders;
    private readonly IFulfillmentInventoryGateway _inventory;
    private readonly FulfillmentInstrumentation _telemetry;

    /// <summary>
    /// دایرکتوری را به schema fulfillment و درز Order/Inventory وصل می‌کند.
    /// </summary>
    public FulfillmentDirectory(
        FulfillmentDbContext db,
        IFulfillmentUseCaseGuard guard,
        IOrderFulfillmentReader orders,
        IFulfillmentInventoryGateway inventory,
        FulfillmentInstrumentation telemetry)
    {
        _db = db;
        _guard = guard;
        _orders = orders;
        _inventory = inventory;
        _telemetry = telemetry;
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

        var now = DateTimeOffset.UtcNow;
        foreach (var sellerOrderId in sellerOrderIds.Distinct())
        {
            if (await _db.Fulfillments.AnyAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken))
            {
                continue;
            }

            var handoff = await _orders.GetHandoffAsync(sellerOrderId, cancellationToken)
                ?? throw new InvalidOperationException("سفارش برای fulfillment پیدا نشد.");
            if (!handoff.IsPaid)
            {
                throw new InvalidOperationException("fulfillment فقط برای سفارش Paid ساخته می‌شود.");
            }

            var unit = FulfillmentUnit.CreateFromPaidOrder(
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
                now);
            _db.Fulfillments.Add(unit);
            _db.Items.AddRange(unit.Items);
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
        unit.MarkProcessing(DateTimeOffset.UtcNow);
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
            DateTimeOffset.UtcNow);
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
            DateTimeOffset.UtcNow);
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
        unit.MarkPacked(DateTimeOffset.UtcNow);
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
            DateTimeOffset.UtcNow);
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
            DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("unpacked");
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken,
        string? shippingMethodCode = null,
        string? providerMetadataJson = null)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var methodCode = shippingMethodCode?.Trim();
        string? normalizedMetadata = null;
        string methodLabel = carrierDisplayName;
        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var definition = ShippingMethodRegistry.Find(methodCode)
                ?? throw new InvalidOperationException("روش ارسال پشتیبانی نمی‌شود یا غیرفعال است.");
            methodLabel = definition.LabelFa;
            if (string.IsNullOrWhiteSpace(carrierDisplayName))
            {
                carrierDisplayName = definition.LabelFa;
            }

            normalizedMetadata = ShippingProviderMetadataValidator.ValidateAndNormalize(definition.Code, providerMetadataJson);
            methodCode = definition.Code;
        }

        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.CreateShipment(
            carrierDisplayName,
            items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            DateTimeOffset.UtcNow,
            methodCode,
            methodLabel,
            normalizedMetadata,
            normalizedMetadata is null ? 0 : 1);
        _db.Shipments.Add(shipment);
        _db.ShipmentItems.AddRange(shipment.Items);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordShipmentCreated();
        return await MapSnapshotAsync(unit, cancellationToken);
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
        unit.CancelShipment(shipmentId, DateTimeOffset.UtcNow);
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
            throw new InvalidOperationException("fulfillment.tracking.duplicate");
        }

        unit.AssignTracking(shipmentId, normalized, DateTimeOffset.UtcNow);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("fulfillment.tracking.duplicate");
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
            throw new InvalidOperationException("fulfillment.tracking.duplicate");
        }

        unit.CorrectTracking(shipmentId, normalized, DateTimeOffset.UtcNow);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("fulfillment.tracking.duplicate");
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

        unit.ApplyShipmentDispatched(shipmentId, DateTimeOffset.UtcNow);
        await ConsumeInventoryForShipmentAsync(unit, shipment, cancellationToken);
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

        unit.ApplyShipmentDelivered(shipmentId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordDelivered();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    private async Task ConsumeInventoryForShipmentAsync(
        FulfillmentUnit unit,
        Shipment shipment,
        CancellationToken cancellationToken)
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
                await _inventory.ConsumeReservationAsync(fulfillmentItem.ReservationId.Value, cancellationToken);
                fulfillmentItem.MarkReservationConsumed();
            }
        }
    }

    private async Task<FulfillmentUnit> LoadMutableAsync(Guid fulfillmentId, CancellationToken cancellationToken)
    {
        var unit = await _db.Fulfillments.SingleOrDefaultAsync(x => x.FulfillmentId == fulfillmentId, cancellationToken)
            ?? throw new InvalidOperationException("fulfillment پیدا نشد.");
        var items = await _db.Items.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.Where(x => x.FulfillmentId == fulfillmentId).ToListAsync(cancellationToken);
        foreach (var shipment in shipments)
        {
            var shipmentItems = await _db.ShipmentItems.Where(x => x.ShipmentId == shipment.ShipmentId).ToListAsync(cancellationToken);
            shipment.AttachLoadedItems(shipmentItems);
        }

        unit.AttachLoadedItems(items);
        unit.AttachLoadedShipments(shipments);
        return unit;
    }

    private async Task<FulfillmentSnapshot> MapSnapshotAsync(FulfillmentUnit unit, CancellationToken cancellationToken)
    {
        var items = await _db.Items.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var shipmentSnapshots = new List<ShipmentSnapshot>(shipments.Count);
        foreach (var shipment in shipments)
        {
            var shipmentItems = await _db.ShipmentItems.AsNoTracking()
                .Where(x => x.ShipmentId == shipment.ShipmentId)
                .ToListAsync(cancellationToken);
            shipmentSnapshots.Add(new ShipmentSnapshot(
                shipment.ShipmentId,
                shipment.Status,
                shipment.CarrierDisplayName,
                shipment.TrackingReference,
                shipment.DispatchedAt,
                shipment.DeliveredAt,
                shipmentItems.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray(),
                shipment.CreatedAt,
                shipment.ShippingMethodCode,
                shipment.ShippingMethodLabel,
                shipment.ProviderMetadataJson,
                shipment.ProviderMetadataVersion,
                shipment.PreviousTrackingReference));
        }

        return new FulfillmentSnapshot(
            unit.FulfillmentId,
            unit.SellerOrderId,
            unit.CheckoutId,
            unit.SellerPartyId,
            unit.Status,
            unit.RecipientName,
            unit.ContactMobile,
            unit.ProvinceName,
            unit.CityName,
            unit.PostalAddress,
            unit.PostalCode,
            unit.ShippingMethodCode,
            unit.ShippingMethodLabel,
            items.Select(x => new FulfillmentItemSnapshot(
                x.FulfillmentItemId,
                x.OrderLineId,
                x.QuantityOrdered,
                x.QuantityShipped,
                x.ReservationId,
                x.QuantityPacked,
                x.QuantityProcessing)).ToArray(),
            shipmentSnapshots,
            unit.CreatedAt,
            unit.UpdatedAt);
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
            throw new InvalidOperationException("fulfillment.unconfirm.already_started");
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
        var now = DateTimeOffset.UtcNow;
        var created = false;
        foreach (var sellerOrderId in sellerOrderIds.Distinct())
        {
            if (await _db.Fulfillments.AnyAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken))
            {
                continue;
            }

            var handoff = await _orders.GetHandoffAsync(sellerOrderId, cancellationToken)
                ?? throw new InvalidOperationException("سفارش برای fulfillment پیدا نشد.");
            if (!handoff.IsPaid)
            {
                throw new InvalidOperationException("fulfillment فقط برای سفارش Paid ساخته می‌شود.");
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
                now);
            _db.Fulfillments.Add(unit);
            _db.Items.AddRange(unit.Items);
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

        var now = DateTimeOffset.UtcNow;
        var loaded = new List<FulfillmentUnit>(units.Count);
        foreach (var unit in units)
        {
            loaded.Add(await LoadMutableAsync(unit.FulfillmentId, cancellationToken));
        }

        if (loaded.Any(x => x.HasDispatchedQuantity()))
        {
            throw new InvalidOperationException("fulfillment.cancel.already_dispatched");
        }

        foreach (var unit in loaded)
        {
            unit.AbortForOrderCancel(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
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

        var now = DateTimeOffset.UtcNow;
        var loaded = new List<FulfillmentUnit>(units.Count);
        foreach (var unit in units)
        {
            loaded.Add(await LoadMutableAsync(unit.FulfillmentId, cancellationToken));
        }

        if (loaded.Any(x => x.HasDispatchedQuantity()))
        {
            throw new InvalidOperationException("fulfillment.restore.already_dispatched");
        }

        foreach (var unit in loaded)
        {
            var handoff = await _orders.GetHandoffAsync(unit.SellerOrderId, cancellationToken)
                ?? throw new InvalidOperationException("سفارش برای fulfillment پیدا نشد.");
            var reservations = handoff.Lines.ToDictionary(x => x.OrderLineId, x => x.ReservationId);
            unit.RebindActiveReservations(reservations);
            unit.ReactivateAfterOrderRestore(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var packages = await _db.ConsolidatedPackages.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var results = new List<ConsolidatedPackageSnapshot>(packages.Count);
        foreach (var package in packages)
        {
            results.Add(await MapPackageSnapshotAsync(package, cancellationToken));
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            return [];
        }

        var ids = shipmentIds.Distinct().ToArray();
        var members = await _db.ConsolidatedPackageMembers.AsNoTracking()
            .Where(x => ids.Contains(x.ShipmentId) && x.ReleasedAt == null)
            .ToListAsync(cancellationToken);
        if (members.Count == 0)
        {
            return [];
        }

        var packageIds = members.Select(x => x.ConsolidatedPackageId).Distinct().ToArray();
        var packages = await _db.ConsolidatedPackages.AsNoTracking()
            .Where(x => packageIds.Contains(x.ConsolidatedPackageId)
                && x.Status != ConsolidatedPackageStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var byId = packages.ToDictionary(x => x.ConsolidatedPackageId);
        return members
            .Where(m => byId.ContainsKey(m.ConsolidatedPackageId))
            .Select(m =>
            {
                var package = byId[m.ConsolidatedPackageId];
                return new ActivePackageMembershipSnapshot(
                    m.ShipmentId,
                    package.ConsolidatedPackageId,
                    package.PackageNumber,
                    package.Status);
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> IsShipmentLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var memberships = await GetActiveMembershipByShipmentIdsAsync([shipmentId], cancellationToken);
        return memberships.Any(x =>
            x.PackageStatus is ConsolidatedPackageStatus.Created or ConsolidatedPackageStatus.Dispatched);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        IReadOnlyList<Guid> shipmentIds,
        string? shippingMethodCode,
        string? trackingReference,
        string? note,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            throw new InvalidOperationException("fulfillment.package.requires_multi_seller");
        }

        var distinctIds = shipmentIds.Distinct().ToArray();
        if (distinctIds.Length != shipmentIds.Count)
        {
            throw new InvalidOperationException("fulfillment.package.duplicate_shipment");
        }

        var units = await _db.Fulfillments
            .Where(x => x.CheckoutId == checkoutId)
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            throw new InvalidOperationException("fulfillment.package.checkout_required");
        }

        var unitById = units.ToDictionary(x => x.FulfillmentId);
        var fulfillmentIds = units.Select(x => x.FulfillmentId).ToArray();
        var shipments = await _db.Shipments
            .Where(x => fulfillmentIds.Contains(x.FulfillmentId) && distinctIds.Contains(x.ShipmentId))
            .ToListAsync(cancellationToken);
        if (shipments.Count != distinctIds.Length)
        {
            throw new InvalidOperationException("fulfillment.package.mixed_checkout");
        }

        var existingLocks = await GetActiveMembershipByShipmentIdsAsync(distinctIds, cancellationToken);
        if (existingLocks.Count > 0)
        {
            throw new InvalidOperationException("fulfillment.package.shipment_already_member");
        }

        var memberSpecs = new List<(Guid ShipmentId, Guid SellerPartyId, Guid FulfillmentId)>(shipments.Count);
        var inheritedMethodCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? inheritedLabel = null;
        foreach (var shipment in shipments)
        {
            if (shipment.Status != ShipmentStatus.Created
                || shipment.DispatchedAt is not null)
            {
                throw new InvalidOperationException("fulfillment.package.shipment_not_eligible");
            }

            if (!unitById.TryGetValue(shipment.FulfillmentId, out var unit)
                || unit.CheckoutId != checkoutId
                || unit.Status == FulfillmentStatus.Cancelled)
            {
                throw new InvalidOperationException("fulfillment.package.shipment_not_eligible");
            }

            var shipmentMethod = (shipment.ShippingMethodCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(shipmentMethod))
            {
                throw new InvalidOperationException("fulfillment.package.shipping_method_required");
            }

            inheritedMethodCodes.Add(shipmentMethod);
            inheritedLabel ??= string.IsNullOrWhiteSpace(shipment.ShippingMethodLabel)
                ? null
                : shipment.ShippingMethodLabel.Trim();
            memberSpecs.Add((shipment.ShipmentId, unit.SellerPartyId, unit.FulfillmentId));
        }

        if (inheritedMethodCodes.Count != 1)
        {
            throw new InvalidOperationException("fulfillment.package.shipping_method_mismatch");
        }

        var inheritedCode = inheritedMethodCodes.Single();
        var requestedCode = shippingMethodCode?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedCode)
            && !string.Equals(requestedCode, inheritedCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("fulfillment.package.shipping_method_mismatch");
        }

        var definition = ShippingMethodRegistry.Find(inheritedCode)
            ?? throw new InvalidOperationException("fulfillment.package.shipping_method_required");
        var methodLabel = string.IsNullOrWhiteSpace(inheritedLabel)
            ? ShippingMethodRegistry.ResolveLabel(definition.Code, null)
            : inheritedLabel;

        var now = DateTimeOffset.UtcNow;
        var package = ConsolidatedPackage.Create(
            checkoutId,
            memberSpecs,
            definition.Code,
            methodLabel,
            trackingReference,
            note,
            actorUserId == Guid.Empty ? null : actorUserId,
            now);

        _db.ConsolidatedPackages.Add(package);
        _db.ConsolidatedPackageMembers.AddRange(package.Members);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("fulfillment.package.shipment_already_member");
        }

        _telemetry.RecordTransition("consolidated_package_created");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.Cancel(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_cancelled");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(
        Guid consolidatedPackageId,
        string trackingReference,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.AssignTracking(trackingReference, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_tracking_assigned");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        if (package.Status is ConsolidatedPackageStatus.Dispatched or ConsolidatedPackageStatus.Delivered)
        {
            return await MapPackageSnapshotAsync(package, cancellationToken);
        }

        if (package.Status != ConsolidatedPackageStatus.Created)
        {
            throw new InvalidOperationException("fulfillment.package.dispatch_invalid_state");
        }

        var activeMembers = package.Members.Where(x => x.IsActiveMembership).ToArray();
        if (activeMembers.Length == 0)
        {
            throw new InvalidOperationException("fulfillment.package.member_state_changed");
        }

        foreach (var member in activeMembers)
        {
            var unit = await LoadMutableAsync(member.FulfillmentId, cancellationToken);
            var shipment = unit.Shipments.SingleOrDefault(x => x.ShipmentId == member.ShipmentId)
                ?? throw new InvalidOperationException("fulfillment.package.member_state_changed");
            if (shipment.Status is ShipmentStatus.Dispatched or ShipmentStatus.InTransit or ShipmentStatus.Delivered)
            {
                continue;
            }

            if (shipment.Status != ShipmentStatus.Created)
            {
                throw new InvalidOperationException("fulfillment.package.member_state_changed");
            }

            if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                && !string.IsNullOrWhiteSpace(package.TrackingReference))
            {
                var baseCode = package.TrackingReference!.Trim();
                var memberCode = $"{baseCode}-{member.ShipmentId.ToString("N")[..8].ToUpperInvariant()}";
                await AssignTrackingCoreAsync(
                    member.FulfillmentId,
                    member.ShipmentId,
                    actorUserId,
                    memberCode,
                    cancellationToken);
            }

            await DispatchShipmentCoreAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actorUserId,
                cancellationToken);
        }

        var statuses = await LoadMemberShipmentStatusesAsync(
            activeMembers.Select(x => x.ShipmentId).ToArray(),
            cancellationToken);
        package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.MarkDispatched(DateTimeOffset.UtcNow, statuses);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_dispatched");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(
        Guid consolidatedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        if (package.Status == ConsolidatedPackageStatus.Delivered)
        {
            return await MapPackageSnapshotAsync(package, cancellationToken);
        }

        if (package.Status != ConsolidatedPackageStatus.Dispatched)
        {
            throw new InvalidOperationException("fulfillment.package.deliver_before_dispatch");
        }

        var activeMembers = package.Members.Where(x => x.IsActiveMembership).ToArray();
        foreach (var member in activeMembers)
        {
            await DeliverShipmentCoreAsync(
                member.FulfillmentId,
                member.ShipmentId,
                actorUserId,
                cancellationToken);
        }

        var statuses = await LoadMemberShipmentStatusesAsync(
            activeMembers.Select(x => x.ShipmentId).ToArray(),
            cancellationToken);
        package = await LoadMutablePackageAsync(consolidatedPackageId, cancellationToken);
        package.MarkDelivered(DateTimeOffset.UtcNow, statuses);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_delivered");
        return await MapPackageSnapshotAsync(package, cancellationToken);
    }

    /// <inheritdoc />
    public async Task VoidActivePackagesForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var packages = await _db.ConsolidatedPackages
            .Where(x => x.CheckoutId == checkoutId && x.Status == ConsolidatedPackageStatus.Created)
            .ToListAsync(cancellationToken);
        if (packages.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var header in packages)
        {
            var package = await LoadMutablePackageAsync(header.ConsolidatedPackageId, cancellationToken);
            package.Cancel(now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordTransition("consolidated_package_voided_for_order_cancel");
    }

    private async Task EnsureShipmentNotLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        if (await IsShipmentLockedByPackageAsync(shipmentId, cancellationToken))
        {
            throw new InvalidOperationException("fulfillment.shipment.locked_by_consolidated_package");
        }
    }

    private async Task<ConsolidatedPackage> LoadMutablePackageAsync(
        Guid consolidatedPackageId,
        CancellationToken cancellationToken)
    {
        var package = await _db.ConsolidatedPackages
            .SingleOrDefaultAsync(x => x.ConsolidatedPackageId == consolidatedPackageId, cancellationToken)
            ?? throw new InvalidOperationException("fulfillment.package.not_found");
        var members = await _db.ConsolidatedPackageMembers
            .Where(x => x.ConsolidatedPackageId == consolidatedPackageId)
            .ToListAsync(cancellationToken);
        package.AttachLoadedMembers(members);
        return package;
    }

    private async Task<IReadOnlyDictionary<Guid, ShipmentStatus>> LoadMemberShipmentStatusesAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken)
    {
        if (shipmentIds.Count == 0)
        {
            return new Dictionary<Guid, ShipmentStatus>();
        }

        var rows = await _db.Shipments.AsNoTracking()
            .Where(x => shipmentIds.Contains(x.ShipmentId))
            .Select(x => new { x.ShipmentId, x.Status })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.ShipmentId, x => x.Status);
    }

    private async Task<ConsolidatedPackageSnapshot> MapPackageSnapshotAsync(
        ConsolidatedPackage package,
        CancellationToken cancellationToken)
    {
        var members = package.Members.Count > 0
            ? package.Members
            : await _db.ConsolidatedPackageMembers.AsNoTracking()
                .Where(x => x.ConsolidatedPackageId == package.ConsolidatedPackageId)
                .ToListAsync(cancellationToken);
        return new ConsolidatedPackageSnapshot(
            package.ConsolidatedPackageId,
            package.PackageNumber,
            package.CheckoutId,
            package.Status,
            package.ShippingMethodCode,
            package.ShippingMethodLabel,
            package.TrackingReference,
            package.Note,
            package.CreatedBy,
            package.CreatedAt,
            package.UpdatedAt,
            package.DispatchedAt,
            package.DeliveredAt,
            package.CancelledAt,
            members.Select(x => new ConsolidatedPackageMemberSnapshot(
                x.ConsolidatedPackageMemberId,
                x.ShipmentId,
                x.SellerPartyId,
                x.FulfillmentId,
                x.JoinedAt,
                x.ReleasedAt)).ToArray());
    }
}
