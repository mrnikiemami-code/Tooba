using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Directories;

/// <summary>
/// بخش خواندنی <see cref="FulfillmentDirectory"/>: نگاشت واحد Fulfillment به snapshot
/// شامل محموله‌های گروه‌محور و گروه‌های ارسال پذیرفته‌شدهٔ همان واحد.
/// </summary>
public sealed partial class FulfillmentDirectory
{
    private async Task<FulfillmentSnapshot> MapSnapshotAsync(FulfillmentUnit unit, CancellationToken cancellationToken)
    {
        var items = await _db.Items.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .ToListAsync(cancellationToken);
        var shipments = await _db.Shipments.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var receipts = await _db.ShipmentReturnReceipts.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .ToListAsync(cancellationToken);
        var receiptItems = receipts.Count == 0
            ? []
            : await _db.ShipmentReturnReceiptItems.AsNoTracking()
                .Where(x => receipts.Select(r => r.ShipmentReturnReceiptId).Contains(x.ShipmentReturnReceiptId))
                .ToListAsync(cancellationToken);
        var receiptShipment = receipts.ToDictionary(r => r.ShipmentReturnReceiptId, r => r.ShipmentId);
        // TB-262: the durable damaged-goods decisions of this unit, read once so every parcel/line can report its own.
        var resolutions = await _db.DamagedResolutions.AsNoTracking()
            .Where(x => x.FulfillmentId == unit.FulfillmentId)
            .ToListAsync(cancellationToken);
        var resolutionsByShipment = resolutions
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DamagedGoodsResolution>)g.ToArray());
        var itemsByReceipt = receiptItems
            .GroupBy(x => x.ShipmentReturnReceiptId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ShipmentReturnReceiptItem>)g.ToArray());
        // All parcel lines are read once: the resend commitment of a source parcel must be visible while building the
        // snapshot of an earlier parcel, so per-parcel lazy loading cannot be used here.
        var allShipmentItems = shipments.Count == 0
            ? []
            : await _db.ShipmentItems.AsNoTracking()
                .Where(x => shipments.Select(s => s.ShipmentId).Contains(x.ShipmentId))
                .ToListAsync(cancellationToken);
        var shipmentItemsByShipment = allShipmentItems
            .GroupBy(x => x.ShipmentId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ShipmentItem>)g.ToArray());
        var shipmentSnapshots = new List<ShipmentSnapshot>(shipments.Count);
        foreach (var shipment in shipments)
        {
            var shipmentItems = shipmentItemsByShipment.TryGetValue(shipment.ShipmentId, out var loadedItems)
                ? loadedItems
                : [];
            var receipt = receipts.SingleOrDefault(x => x.ShipmentId == shipment.ShipmentId);
            var receiptSnapshot = receipt is null
                ? null
                : new ShipmentReturnReceiptSnapshot(
                    receipt.ShipmentReturnReceiptId,
                    receipt.ReceivedAt,
                    receipt.ReceivedByUserId,
                    receipt.Note,
                    (itemsByReceipt.TryGetValue(receipt.ShipmentReturnReceiptId, out var receivedItems)
                            ? receivedItems
                            : [])
                        .Select(x => new ShipmentReturnReceiptLineSnapshot(x.OrderLineId, x.Quantity, x.Condition))
                        .ToArray());
            shipmentSnapshots.Add(new ShipmentSnapshot(
                shipment.ShipmentId,
                shipment.Status,
                shipment.CarrierDisplayName,
                shipment.TrackingReference,
                shipment.DispatchedAt,
                shipment.DeliveredAt,
                // TB-258-R1: the resend capacity of a line is specific to THIS physically received source parcel.
                // TB-262: the damaged quantity of THIS parcel line and what it still owes a decision are separate
                // facts, so a damaged unit can never be read as usable capacity.
                shipmentItems
                    .Select(x => new ShipmentLineSnapshot(
                        x.OrderLineId,
                        x.Quantity,
                        SourceResendableQuantity(
                            receiptItems,
                            receiptShipment,
                            shipmentItemsByShipment,
                            shipments,
                            shipment.ShipmentId,
                            x.OrderLineId),
                        SourceDamagedQuantity(receiptItems, receiptShipment, shipment.ShipmentId, x.OrderLineId),
                        SourceOutstandingDamagedQuantity(
                            receiptItems,
                            receiptShipment,
                            resolutionsByShipment,
                            shipment.ShipmentId,
                            x.OrderLineId)))
                    .ToArray(),
                shipment.CreatedAt,
                shipment.ShippingMethodCode,
                shipment.ShippingMethodLabel,
                shipment.ProviderMetadataJson,
                shipment.ProviderMetadataVersion,
                shipment.PreviousTrackingReference,
                shipment.ShippingGroupId,
                shipment.ShippingDispatchKey,
                shipment.ResendOfShipmentId,
                shipment.NonDeliveryReason,
                shipment.NonDeliveryNote,
                shipment.NonDeliveredAt,
                shipment.ReturnStartedAt,
                shipment.ReturnedAt,
                receiptSnapshot,
                shipment.DamagedResolutionId,
                shipment.ReplacementReservationId,
                shipment.IsReplacement,
                (resolutionsByShipment.TryGetValue(shipment.ShipmentId, out var parcelResolutions)
                        ? parcelResolutions
                        : [])
                    .Select(MapDamagedResolution)
                    .ToArray()));
        }

        var groupSnapshots = await BuildShippingGroupSnapshotsAsync(unit.FulfillmentId, items, cancellationToken);

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
                x.QuantityProcessing,
                x.ShippingGroupId,
                UsableReturnedQuantity(receiptItems, x.OrderLineId),
                ResendCommittedQuantity(shipmentSnapshots, x.OrderLineId),
                ResendableQuantity(receiptItems, shipmentSnapshots, x.OrderLineId),
                DeliveredQuantity(shipmentSnapshots, x.OrderLineId),
                DamagedReturnedQuantity(receiptItems, x.OrderLineId),
                OutstandingDamagedQuantity(receiptItems, resolutions, x.OrderLineId))).ToArray(),
            shipmentSnapshots,
            unit.CreatedAt,
            unit.UpdatedAt,
            ShippingGroups: groupSnapshots.Count == 0 ? null : groupSnapshots);
    }

    private static decimal UsableReturnedQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        Guid orderLineId) =>
        receiptItems
            .Where(x => x.OrderLineId == orderLineId && x.Condition == ReturnReceiptCondition.Usable)
            .Sum(x => x.Quantity);

    /// <summary>
    /// TB-258-R1: usable capacity of ONE physically received source parcel for one line, minus its OWN non-cancelled
    /// resend parcels. Another parcel's receipt can never fund this source.
    /// </summary>
    private static decimal SourceResendableQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyDictionary<Guid, Guid> receiptShipment,
        IReadOnlyDictionary<Guid, IReadOnlyList<ShipmentItem>> shipmentItemsByShipment,
        IReadOnlyList<Shipment> shipments,
        Guid sourceShipmentId,
        Guid orderLineId)
    {
        var usable = receiptItems
            .Where(x => x.OrderLineId == orderLineId
                && x.Condition == ReturnReceiptCondition.Usable
                && receiptShipment.TryGetValue(x.ShipmentReturnReceiptId, out var owner)
                && owner == sourceShipmentId)
            .Sum(x => x.Quantity);
        var committed = shipments
            .Where(s => s.IsResend
                && s.Status != ShipmentStatus.Cancelled
                && s.ResendOfShipmentId == sourceShipmentId)
            .SelectMany(s => shipmentItemsByShipment.TryGetValue(s.ShipmentId, out var items) ? items : [])
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);
        var remaining = usable - committed;
        return remaining > 0m ? remaining : 0m;
    }

    private static decimal ResendCommittedQuantity(
        IReadOnlyList<ShipmentSnapshot> shipments,
        Guid orderLineId) =>
        shipments
            // TB-262: a replacement parcel is funded by its OWN healthy-stock reservation, so it must never consume the
            // returned usable capacity of its damaged source (that would make one repair eat a real resend).
            .Where(s => s.ResendOfShipmentId is not null
                && s.DamagedResolutionId is null
                && s.Status != ShipmentStatus.Cancelled)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    private static decimal ResendableQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyList<ShipmentSnapshot> shipments,
        Guid orderLineId)
    {
        var remaining = UsableReturnedQuantity(receiptItems, orderLineId)
            - ResendCommittedQuantity(shipments, orderLineId);
        return remaining > 0m ? remaining : 0m;
    }

    private static decimal DeliveredQuantity(
        IReadOnlyList<ShipmentSnapshot> shipments,
        Guid orderLineId) =>
        shipments
            .Where(s => s.Status == ShipmentStatus.Delivered)
            .SelectMany(s => s.Items)
            .Where(x => x.OrderLineId == orderLineId)
            .Sum(x => x.Quantity);

    /// <summary>TB-262: damaged quantity of ONE source parcel line, physically received back at origin.</summary>
    private static decimal SourceDamagedQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyDictionary<Guid, Guid> receiptShipment,
        Guid sourceShipmentId,
        Guid orderLineId) =>
        receiptItems
            .Where(x => x.OrderLineId == orderLineId
                && x.Condition == ReturnReceiptCondition.Damaged
                && receiptShipment.TryGetValue(x.ShipmentReturnReceiptId, out var owner)
                && owner == sourceShipmentId)
            .Sum(x => x.Quantity);

    /// <summary>TB-262: damaged quantity of ONE source parcel line still awaiting a durable decision.</summary>
    private static decimal SourceOutstandingDamagedQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyDictionary<Guid, Guid> receiptShipment,
        IReadOnlyDictionary<Guid, IReadOnlyList<DamagedGoodsResolution>> resolutionsByShipment,
        Guid sourceShipmentId,
        Guid orderLineId)
    {
        var damaged = SourceDamagedQuantity(receiptItems, receiptShipment, sourceShipmentId, orderLineId);
        var committed = resolutionsByShipment.TryGetValue(sourceShipmentId, out var resolutions)
            ? resolutions
                .Where(r => r.OrderLineId == orderLineId && r.Status != DamagedResolutionStatus.Cancelled)
                .Sum(r => r.Quantity)
            : 0m;
        var remaining = damaged - committed;
        return remaining > 0m ? remaining : 0m;
    }

    /// <summary>TB-262: damaged quantity of one line across every source parcel of the unit.</summary>
    private static decimal DamagedReturnedQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        Guid orderLineId) =>
        receiptItems
            .Where(x => x.OrderLineId == orderLineId && x.Condition == ReturnReceiptCondition.Damaged)
            .Sum(x => x.Quantity);

    /// <summary>TB-262: damaged quantity of one line still awaiting a durable decision.</summary>
    private static decimal OutstandingDamagedQuantity(
        IReadOnlyList<ShipmentReturnReceiptItem> receiptItems,
        IReadOnlyList<DamagedGoodsResolution> resolutions,
        Guid orderLineId)
    {
        var damaged = DamagedReturnedQuantity(receiptItems, orderLineId);
        var committed = resolutions
            .Where(r => r.OrderLineId == orderLineId && r.Status != DamagedResolutionStatus.Cancelled)
            .Sum(r => r.Quantity);
        var remaining = damaged - committed;
        return remaining > 0m ? remaining : 0m;
    }

    private static DamagedResolutionSnapshot MapDamagedResolution(DamagedGoodsResolution resolution) =>
        new(
            resolution.DamagedGoodsResolutionId,
            resolution.ShipmentId,
            resolution.ShipmentReturnReceiptId,
            resolution.OrderLineId,
            resolution.Quantity,
            resolution.Outcome,
            resolution.Status,
            resolution.DecidedByUserId,
            resolution.CreatedAt,
            resolution.UpdatedAt,
            resolution.ReplacementReservationId,
            resolution.ReplacementShipmentId,
            resolution.Note);

    /// <summary>
    /// snapshot گروه‌های واحد را با عضویت خطوط همان واحد می‌سازد؛ ترتیب پایدار بر اساس شناسهٔ گروه.
    /// </summary>
    private async Task<IReadOnlyList<FulfillmentShippingGroupSnapshot>> BuildShippingGroupSnapshotsAsync(
        Guid fulfillmentId,
        IReadOnlyList<FulfillmentItem> items,
        CancellationToken cancellationToken)
    {
        var groups = await _db.ShippingGroups.AsNoTracking()
            .Where(x => x.FulfillmentId == fulfillmentId)
            .OrderBy(x => x.ShippingGroupId)
            .ToListAsync(cancellationToken);
        if (groups.Count == 0)
        {
            return [];
        }

        return groups
            .Select(group => new FulfillmentShippingGroupSnapshot(
                group.ShippingGroupId,
                group.MethodCode,
                group.ServiceCode,
                group.OptionCode,
                group.MethodLabel,
                group.OriginContext,
                group.MinimumDeliveryDate,
                group.RequestedDeliveryDate,
                group.RequestedDeliveryTimeWindow,
                group.LeadDays,
                items.Where(x => x.ShippingGroupId == group.ShippingGroupId)
                    .Select(x => x.OrderLineId)
                    .ToArray()))
            .ToArray();
    }
}


