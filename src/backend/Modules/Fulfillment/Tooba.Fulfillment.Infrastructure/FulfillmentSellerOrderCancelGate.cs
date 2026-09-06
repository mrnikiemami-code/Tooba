using Tooba.Fulfillment.Application;
using Tooba.Order.Application;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// درز Fulfillment برای قاعدهٔ لغو Paid پیش از محموله در Order.
/// </summary>
public sealed class FulfillmentSellerOrderCancelGate : ISellerOrderCancelFulfillmentGate
{
    private readonly IFulfillmentDirectory _fulfillment;

    /// <summary>gate را به دایرکتوری fulfillment وصل می‌کند.</summary>
    public FulfillmentSellerOrderCancelGate(IFulfillmentDirectory fulfillment) => _fulfillment = fulfillment;

    /// <inheritdoc />
    public async Task<SellerOrderCancelFulfillmentSnapshot?> GetAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _fulfillment.GetBySellerOrderAsync(sellerOrderId, cancellationToken);
        if (snapshot is null)
        {
            return null;
        }

        return new SellerOrderCancelFulfillmentSnapshot(
            snapshot.Status.ToString(),
            snapshot.Shipments.Count);
    }
}
