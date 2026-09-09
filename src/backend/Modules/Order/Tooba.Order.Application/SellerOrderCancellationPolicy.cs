using Tooba.Order.Domain;

namespace Tooba.Order.Application;

/// <summary>
/// snapshot سبک وضعیت fulfillment برای تصمیم لغو؛ بدون وابستگی به DbContext Fulfillment.
/// خط قرمز لغو کل سفارش، اولین quantity واقعی Dispatch است نه ایجاد مرسوله یا Packed.
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

/// <summary>
/// قاعدهٔ واحد لغو SellerOrder — منبع authoritative برای دامنه/اپلیکیشن/projection.
/// </summary>
public static class SellerOrderCancellationPolicy
{
    /// <summary>وضعیت‌های باز قبل از پرداخت.</summary>
    public static bool IsOpenCancellable(SellerOrderStatus status) =>
        status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested;

    /// <summary>Paid تا قبل از اولین quantity واقعی Dispatch؛ Packed و مرسولهٔ Created مانع نیستند.</summary>
    public static bool IsPaidPreShipmentCancellable(
        SellerOrderStatus status,
        SellerOrderCancelFulfillmentSnapshot? fulfillment)
    {
        if (status != SellerOrderStatus.Paid)
        {
            return false;
        }

        return fulfillment is null || !fulfillment.HasDispatchedQuantity;
    }

    /// <summary>آیا لغو در این ترکیب وضعیت مجاز است.</summary>
    public static bool CanCancel(SellerOrderStatus status, SellerOrderCancelFulfillmentSnapshot? fulfillment) =>
        IsOpenCancellable(status) || IsPaidPreShipmentCancellable(status, fulfillment);
}
