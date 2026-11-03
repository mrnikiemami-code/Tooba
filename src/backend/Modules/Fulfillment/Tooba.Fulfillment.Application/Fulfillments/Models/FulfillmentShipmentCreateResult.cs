namespace Tooba.Fulfillment.Application.Fulfillments.Models;

/// <summary>Exact shipment identity created by one CreateShipment allocation.</summary>
public sealed record FulfillmentShipmentCreateResult(
    FulfillmentSnapshot Snapshot,
    Guid CreatedShipmentId);
