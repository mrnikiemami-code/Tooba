namespace Tooba.Fulfillment.Contracts.Returns;

/// <summary>
/// برش تحویل یک خط در یک مرسولهٔ Delivered (ساعت مرجوعی per-slice).
/// </summary>
public sealed record LineDeliverySlice(
    Guid OrderLineId,
    decimal Quantity,
    DateTimeOffset DeliveredAt);

/// <summary>
/// snapshot eligibility مرجوعی از fulfillment.
/// </summary>
public sealed record FulfillmentReturnEligibilitySnapshot(
    Guid SellerOrderId,
    IReadOnlyDictionary<Guid, decimal> DeliveredQuantities,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyDictionary<Guid, DateTimeOffset>? LineDeliveredAt = null,
    IReadOnlyList<LineDeliverySlice>? DeliverySlices = null);

/// <summary>
/// خواندن evidence تحویل برای Returns بدون cross-DbContext.
/// </summary>
public interface IFulfillmentReturnReader
{
    /// <summary>
    /// snapshot eligibility مرجوعی را برمی‌گرداند.
    /// </summary>
    Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken);
}
