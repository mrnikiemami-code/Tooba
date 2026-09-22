using System.Globalization;
using Tooba.BuildingBlocks;

namespace Tooba.Order.Application.Admin.Completeness.History;

/// <summary>
/// قالب‌بندی نمایشی تاریخچه؛ در دسترس تست‌های Host و Order است.
/// </summary>
public static class AdminOrderHistoryFormatting
{
    private const string FaDigits = "۰۱۲۳۴۵۶۷۸۹";

    /// <summary>عنوان پیش‌فرض فروشنده وقتی نام نمایشی موجود نیست.</summary>
    public const string FallbackSellerName = "فروشنده";

    /// <summary>عنوان پیش‌فرض کالا وقتی عنوان محلی‌سازی‌شده موجود نیست.</summary>
    public const string FallbackProductTitle = "کالای سفارش";

    /// <summary>مقدار را با ارقام فارسی قالب‌بندی می‌کند.</summary>
    public static string ToFaDigits(decimal value) =>
        ToFaDigits(QuantityDisplay.Format(value, 6));

    /// <summary>ارقام لاتین یک رشته را به فارسی تبدیل می‌کند.</summary>
    public static string ToFaDigits(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Concat(value.Select(ch => ch is >= '0' and <= '9' ? FaDigits[ch - '0'] : ch));
    }

    /// <summary>مبلغ گردشده با جداکنندهٔ هزارگان و ارقام فارسی.</summary>
    public static string FormatMoneyFa(decimal amount) =>
        ToFaDigits(decimal
            .Round(amount, 0, MidpointRounding.AwayFromZero)
            .ToString("#,##0", CultureInfo.InvariantCulture));

    /// <summary>خلاصهٔ محدودهٔ بسته‌بندی فروشنده (فروشنده — تعداد قلم).</summary>
    public static string FormatPackScopeFa(string sellerDisplayName, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(sellerDisplayName) ? FallbackSellerName : sellerDisplayName)} — {ToFaDigits(quantity)} قلم";

    /// <summary>خلاصهٔ کالایی تعداددار برای تحویل و مرجوعی.</summary>
    public static string FormatProductQtyScopeFa(string productTitle, decimal quantity) =>
        $"{(string.IsNullOrWhiteSpace(productTitle) ? FallbackProductTitle : productTitle)} — تعداد {ToFaDigits(quantity)}";

    /// <summary>متن یادداشت را با «…» کوتاه می‌کند.</summary>
    public static string Truncate(string value, int max)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }
}
