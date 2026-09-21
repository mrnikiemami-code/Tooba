using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Host.Admin;

/// <summary>
/// Order.Contracts adapter — Host wraps AdminOrderOperationsComposer without leaking Host types to Fulfillment.
/// </summary>
public sealed class HostAdminOrderFulfillmentOperations : IAdminOrderFulfillmentOperations
{
    private readonly AdminOrderOperationsComposer _operations;

    /// <summary>Adapter را می‌سازد.</summary>
    public HostAdminOrderFulfillmentOperations(AdminOrderOperationsComposer operations) => _operations = operations;

    /// <inheritdoc />
    public async Task<AdminOrderFulfillmentOperationOutcome> TryExecuteAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderFulfillmentOperationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _operations.ExecuteAsync(
                checkoutId,
                actorUserId,
                new AdminOrderOperationRequest(
                    request.Code,
                    request.SellerOrderId,
                    request.FulfillmentId,
                    request.ShipmentId,
                    null,
                    request.CarrierDisplayName,
                    request.TrackingReference,
                    null,
                    null,
                    null,
                    null,
                    request.ShippingMethodCode,
                    null),
                cancellationToken);
            return new AdminOrderFulfillmentOperationOutcome(true, null);
        }
        catch (PlatformHttpException ex)
        {
            return new AdminOrderFulfillmentOperationOutcome(false, ex.ErrorCode);
        }
        catch (InvalidOperationException)
        {
            return new AdminOrderFulfillmentOperationOutcome(false, null);
        }
    }
}
