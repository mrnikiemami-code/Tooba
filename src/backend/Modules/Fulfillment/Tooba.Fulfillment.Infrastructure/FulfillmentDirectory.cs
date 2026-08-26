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
    public async Task<FulfillmentSnapshot> CreateShipmentAsync(
        Guid fulfillmentId,
        Guid actorUserId,
        string carrierDisplayName,
        IReadOnlyList<ShipmentLineCommand> items,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.CreateShipment(
            carrierDisplayName,
            items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            DateTimeOffset.UtcNow);
        _db.Shipments.Add(shipment);
        _db.ShipmentItems.AddRange(shipment.Items);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordShipmentCreated();
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
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        unit.AssignTracking(shipmentId, trackingReference, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
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
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
        var shipment = unit.Shipments.Single(x => x.ShipmentId == shipmentId);
        unit.ApplyShipmentDispatched(shipmentId, DateTimeOffset.UtcNow);
        await ConsumeInventoryForShipmentAsync(unit, shipment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _telemetry.RecordDispatched();
        return await MapSnapshotAsync(unit, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FulfillmentSnapshot> DeliverShipmentAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var unit = await LoadMutableAsync(fulfillmentId, cancellationToken);
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
                shipmentItems.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray()));
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
                x.ReservationId)).ToArray(),
            shipmentSnapshots);
    }
}
