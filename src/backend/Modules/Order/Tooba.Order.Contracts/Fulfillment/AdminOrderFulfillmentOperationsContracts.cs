namespace Tooba.Order.Contracts.Fulfillment;

/// <summary>
/// Minimal admin order/fulfillment operation payload for Fulfillment work-queue bulk
/// without Host types or Order.Application.
/// </summary>
public sealed record AdminOrderFulfillmentOperationRequest(
    string Code,
    Guid? SellerOrderId,
    Guid? FulfillmentId,
    Guid? ShipmentId,
    string? CarrierDisplayName,
    string? TrackingReference,
    string? ShippingMethodCode);

/// <summary>Outcome of one admin fulfillment operation attempt.</summary>
public sealed record AdminOrderFulfillmentOperationOutcome(bool Succeeded, string? ErrorCode);

/// <summary>
/// Execute specific admin fulfillment operations used by work-queue bulk.
/// Owned by Order.Infrastructure — Host must not implement this contract.
/// </summary>
public interface IAdminOrderFulfillmentOperations
{
    /// <summary>Try one operation; expected failures return Succeeded=false with ErrorCode.</summary>
    Task<AdminOrderFulfillmentOperationOutcome> TryExecuteAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderFulfillmentOperationRequest request,
        CancellationToken cancellationToken);
}
