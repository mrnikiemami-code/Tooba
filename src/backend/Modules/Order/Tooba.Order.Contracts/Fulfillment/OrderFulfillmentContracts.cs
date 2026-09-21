namespace Tooba.Order.Contracts.Fulfillment;

/// <summary>
/// snapshot immutable سفارش برای handoff fulfillment.
/// </summary>
public sealed record OrderFulfillmentHandoffSnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    Guid PlacedByUserId,
    bool IsPaid,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    IReadOnlyList<OrderFulfillmentLineSnapshot> Lines);

/// <summary>
/// خط سفارش برای fulfillment.
/// </summary>
public sealed record OrderFulfillmentLineSnapshot(
    Guid OrderLineId,
    decimal Quantity,
    Guid? ReservationId);

/// <summary>
/// خواندن snapshot سفارش برای Fulfillment بدون cross-DbContext.
/// </summary>
public interface IOrderFulfillmentReader
{
    /// <summary>
    /// snapshot handoff را برای SellerOrder برمی‌گرداند.
    /// </summary>
    Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// snapshot checkout را برای مشتری برمی‌گرداند.
    /// </summary>
    Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

/// <summary>
/// snapshot سبک وضعیت fulfillment برای تصمیم لغو؛ بدون وابستگی به DbContext Fulfillment.
/// </summary>
public sealed record SellerOrderCancelFulfillmentSnapshot(
    string Status,
    int ShipmentCount,
    bool HasDispatchedQuantity = false);

/// <summary>
/// درز خواندن وضعیت ارسال برای لغو Paid پیش از محموله.
/// </summary>
public interface ISellerOrderCancelFulfillmentGate
{
    /// <summary>وضعیت fulfillment سفارش فروشنده را برمی‌گرداند.</summary>
    Task<SellerOrderCancelFulfillmentSnapshot?> GetAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}
