using App = Tooba.Fulfillment.Application.Models;
using Contracts = Tooba.Fulfillment.Contracts.Operations;

namespace Tooba.Host.Tests;

internal static class FulfillmentSnapshotTestMapping
{
    internal static Contracts.FulfillmentSnapshot ToContracts(this App.FulfillmentSnapshot s) =>
        new(
            s.FulfillmentId,
            s.SellerOrderId,
            s.CheckoutId,
            s.SellerPartyId,
            (Contracts.FulfillmentOperationStatus)(int)s.Status,
            s.RecipientName,
            s.ContactMobile,
            s.ProvinceName,
            s.CityName,
            s.PostalAddress,
            s.PostalCode,
            s.ShippingMethodCode,
            s.ShippingMethodLabel,
            s.Items.Select(i => new Contracts.FulfillmentItemSnapshot(
                i.FulfillmentItemId,
                i.OrderLineId,
                i.QuantityOrdered,
                i.QuantityShipped,
                i.ReservationId,
                i.QuantityPacked,
                i.QuantityProcessing)).ToList(),
            s.Shipments.Select(sh => new Contracts.ShipmentSnapshot(
                sh.ShipmentId,
                (Contracts.ShipmentOperationStatus)(int)sh.Status,
                sh.CarrierDisplayName,
                sh.TrackingReference,
                sh.DispatchedAt,
                sh.DeliveredAt,
                sh.Items.Select(li => new Contracts.ShipmentLineSnapshot(li.OrderLineId, li.Quantity)).ToList(),
                sh.CreatedAt,
                sh.ShippingMethodCode,
                sh.ShippingMethodLabel,
                sh.ProviderMetadataJson,
                sh.ProviderMetadataVersion,
                sh.PreviousTrackingReference)).ToList(),
            s.CreatedAt,
            s.UpdatedAt,
            s.PreferredTrackingReference);
}
