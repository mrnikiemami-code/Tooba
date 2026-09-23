using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Seller.Policies;

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
