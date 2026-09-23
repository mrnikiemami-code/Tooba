using Tooba.Order.Domain;

namespace Tooba.Order.Application.Seller.Ports;

/// <summary>خواندن SellerOrder/Checkout متعلق به فروشنده بدون افشای DbContext.</summary>
public interface ISellerOrderStore
{
    /// <summary>فهرست سفارش‌های فروشنده با خطوط، حداکثر take، نزولی بر SellerOrderId.</summary>
    Task<IReadOnlyList<SellerOrder>> ListBySellerAsync(
        Guid sellerPartyId,
        int take,
        CancellationToken cancellationToken);

    /// <summary>همهٔ سفارش‌های فروشنده با خطوط (داشبورد).</summary>
    Task<IReadOnlyList<SellerOrder>> ListAllBySellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>یک سفارش فقط اگر متعلق به همان فروشنده باشد.</summary>
    Task<SellerOrder?> GetOwnedAsync(
        Guid sellerPartyId,
        Guid sellerOrderId,
        CancellationToken cancellationToken);

    /// <summary>Checkoutها بر اساس شناسه.</summary>
    Task<IReadOnlyDictionary<Guid, CheckoutGroup>> GetCheckoutsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);

    /// <summary>یک Checkout.</summary>
    Task<CheckoutGroup?> GetCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);
}
