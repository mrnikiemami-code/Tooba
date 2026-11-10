using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Application.Fulfillments.Models;


/// <summary>
/// snapshot خط fulfillment.
/// </summary>
public sealed record FulfillmentItemSnapshot(
    Guid FulfillmentItemId,
    Guid OrderLineId,
    decimal QuantityOrdered,
    decimal QuantityShipped,
    Guid? ReservationId,
    decimal QuantityPacked = 0,
    decimal QuantityProcessing = 0,
    Guid? ShippingGroupId = null,
    /// <summary>TB-258: physically received usable returned quantity of this line.</summary>
    decimal ReturnedUsableQuantity = 0,
    /// <summary>TB-258: returned capacity already committed to non-cancelled resend parcels.</summary>
    decimal ResendCommittedQuantity = 0,
    /// <summary>TB-258: returned capacity still available for a new resend parcel.</summary>
    decimal ResendableQuantity = 0,
    /// <summary>TB-258: quantity actually delivered to the customer (derived from delivered parcels).</summary>
    decimal DeliveredQuantity = 0);
