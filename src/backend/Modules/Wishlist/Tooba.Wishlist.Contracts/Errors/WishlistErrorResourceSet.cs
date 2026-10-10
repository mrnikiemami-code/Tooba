using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Wishlist.Contracts.Errors;

/// <summary>نشانگر منبع خطاهای Wishlist.</summary>
public static class WishlistErrorResources
{
    /// <summary>ResourceManager برای WishlistErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Wishlist.Contracts.Resources.WishlistErrors", typeof(WishlistErrorResources).Assembly);
}

/// <summary>
/// مجموعهٔ منبع Wishlist — مالکیت محدود به <c>customer.wishlist.</c> و <c>wishlist.</c>.
/// Wishlist متن خطاهای کدهای پایدار خود را مالک است تا به‌صورت میکروسرویس مستقل بدون جاگذاشتن
/// متن در ماژول دیگر استخراج شود. انتخاب زبان در localizer مرکزی می‌ماند.
/// </summary>
public sealed class WishlistErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("customer.wishlist.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("wishlist.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        WishlistErrorResources.Manager.GetString(localizationKey, culture);
}
