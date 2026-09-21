using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Domain.ValueObjects;

/// <summary>
/// Domain type.
/// </summary>
public enum CartStatus
{
    /// <summary>
    /// سبد باز است و خط می‌پذیرد.
    /// </summary>
    Active = 0,

    /// <summary>
    /// سبد به درز تبدیل سفارش آینده رفته؛ Checkout اینجا اجرا نمی‌شود.
    /// </summary>
    Converted = 1,

    /// <summary>
    /// مهلت UTC سبد گذشته و رزروها باید آزاد شوند.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// مالک سبد را رها کرده؛ رزروها آزاد می‌شوند.
    /// </summary>
    Abandoned = 3,
}
