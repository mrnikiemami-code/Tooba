using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Domain.Aggregates;
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
                shipment.PreviousTrackingReference,
                shipment.ShippingGroupId));
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
                x.ShippingGroupId)).ToArray(),
            shipmentSnapshots,
            unit.CreatedAt,
            unit.UpdatedAt,
            ShippingGroups: groupSnapshots.Count == 0 ? null : groupSnapshots);
    }

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
