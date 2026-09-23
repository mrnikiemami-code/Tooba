using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.PurchaseVerification;

/// <summary>اثبات خرید پرداخت‌شده که فقط از دادهٔ مالک Order ساخته می‌شود.</summary>
public sealed record OrderPurchaseVerification(bool IsVerified, Guid? SellerOrderId)
{
    /// <summary>نتیجهٔ بسته و بدون اثبات.</summary>
    public static OrderPurchaseVerification NotVerified { get; } = new(false, null);
}

/// <summary>درز Order-owned برای اثبات خرید محصول بدون وابستگی Order به Catalog.</summary>
public interface IOrderPurchaseVerificationGateway
{
    /// <summary>
    /// وجود سفارش Paid متعلق به Actor را برای یکی از شناسه‌های گونهٔ داده‌شده بررسی می‌کند؛
    /// نگاشت محصول به گونه‌ها پیش از فراخوانی و توسط مصرف‌کنندهٔ Catalog انجام می‌شود.
    /// </summary>
    Task<OrderPurchaseVerification> VerifyPaidPurchaseAsync(
        Guid actorUserId,
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}
