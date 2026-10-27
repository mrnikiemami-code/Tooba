namespace Tooba.Order.Application.Storefront.Models;

/// <summary>
/// قصد کوپن به‌شکل <b>توکنِ تأیید</b> (TB-236)، نه موتور تخفیف.
/// </summary>
/// <remarks>
/// <para>
/// <b>No coupon policy.</b> هیچ case-fold، حذف فاصلهٔ درونی، برش طول، تطبیق کد یا ارزیابی صلاحیت این‌جا
/// انجام نمی‌شود؛ نمایندگی تطبیق/صلاحیت/تخفیف همان Promotion و مسیر موجود ۲۱۷ است. این عضو فقط
/// «بود/نبود» و «همان متن یا متن دیگر» را یکسان می‌کند تا خواندن/انتخاب/پیش‌نمایش/ثبت همه یک قصد را
/// ببینند.
/// </para>
/// <para>
/// <b>Lossless.</b> متن فقط trim می‌شود (همان قاعده‌ای که مسیرهای فعلی Order به‌کار می‌برند) و بقیهٔ
/// کاراکترها دست‌نخورده می‌مانند. فاصله/تهی یعنی <c>null</c> (نبود)، نه کد و نه صفرِ تخفیف.
/// </para>
/// </remarks>
public static class StorefrontCouponIntent
{
    /// <summary>قصد کوپن را یکسان می‌کند: فاصله/تهی ⇒ نبود، در غیر این صورت همان متن trim‌شده.</summary>
    /// <param name="couponCode">متن خام کوپن از هر مسیر ورودی.</param>
    public static string? Normalize(string? couponCode) =>
        string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim();

    /// <summary>آیا دو قصد، <b>همان</b> قصدند (هر دو نبود، یا متن یکسان Ordinal).</summary>
    /// <param name="left">قصد نخست.</param>
    /// <param name="right">قصد دوم.</param>
    public static bool Same(string? left, string? right) =>
        string.Equals(
            Normalize(left),
            Normalize(right),
            StringComparison.Ordinal);
}
