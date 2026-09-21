using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum CartConversionIntent
{
    /// <summary>
    /// هنوز تبدیل نشده.
    /// </summary>
    None = 0,

    /// <summary>
    /// مبدأ سفارش Request-to-Reserve آینده.
    /// </summary>
    RequestToReserve = 1,

    /// <summary>
    /// مبدأ خرید آنلاین آینده.
    /// </summary>
    OnlinePurchase = 2,
}
