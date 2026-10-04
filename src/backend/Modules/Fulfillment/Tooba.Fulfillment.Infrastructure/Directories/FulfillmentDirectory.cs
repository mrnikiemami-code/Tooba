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
                ?? throw new ContractOperationException("fulfillment.shipping_method.unsupported");
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
            _ids.NewId(),
            () => _ids.NewId(),
            carrierDisplayName,
            items.Select(x => (x.OrderLineId, x.Quantity)).ToArray(),
            _clock.UtcNow,
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
        unit.CancelShipment(shipmentId, _clock.UtcNow);
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

        unit.ApplyShipmentDelivered(shipmentId, _clock.UtcNow);
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
            ?? throw new ContractOperationException("fulfillment.not_found");
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
