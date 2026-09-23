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

namespace Tooba.Order.Application.Admin.Supply.Ports;

/// <summary>خط سفارش برای ترکیب تأمین.</summary>
public sealed record OrderSupplyCheckoutLineSnapshot(
    Guid LineId,
    Guid OfferId,
    Guid? CategoryIdSnapshot,
    Guid? ReservationId,
    decimal Quantity,
    string? UnitDisplaySnapshot,
    string? UnitCodeSnapshot);

/// <summary>سفارش فروشنده برای تأمین.</summary>
public sealed record OrderSupplySellerOrderSnapshot(
    Guid SellerOrderId,
    string OrderNumber,
    SellerOrderStatus Status,
    IReadOnlyList<OrderSupplyCheckoutLineSnapshot> Lines);

/// <summary>Checkout برای تأمین/بازیابی (بدون DbContext در Application).</summary>
public sealed record OrderSupplyCheckoutSnapshot(
    Guid CheckoutId,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<OrderSupplySellerOrderSnapshot> SellerOrders);

/// <summary>خواندن/نوشتن binding رزرو سفارش در Order.Infrastructure.</summary>
public interface IOrderSupplyCheckoutStore
{
    /// <summary>یک checkout با خطوط.</summary>
    Task<OrderSupplyCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>checkoutهای اخیر برای audit (جدیدترین اول).</summary>
    Task<IReadOnlyList<OrderSupplyCheckoutSnapshot>> ListRecentAsync(int take, CancellationToken cancellationToken);

    /// <summary>چند checkout برای batch status.</summary>
    Task<IReadOnlyList<OrderSupplyCheckoutSnapshot>> GetManyAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);

    /// <summary>جایگزینی ReservationId خطوط و SaveChanges.</summary>
    Task ReplaceReservationsAsync(
        Guid checkoutId,
        IReadOnlyDictionary<Guid, Guid> bindingsByOrderLineId,
        CancellationToken cancellationToken);
}
