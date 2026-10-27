namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// سیاست <b>یگانه و خالص</b> مقایسهٔ هویت نشانی ارسال بین تصویر قیمت‌گذاری‌شده، رکورد کتاب آدرس و
/// فرمان نهایی ثبت (TB-233-R3).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only canonical server text.</b> این کمک هیچ نشانی نمی‌سازد، هیچ شناسه‌ای حدس نمی‌زند و هیچ
/// جست‌وجوی بین‌کشوری انجام نمی‌دهد: فقط دو تصویر متنی را با یک سیاست واحد مقایسه می‌کند تا «آنچه
/// قیمت گرفت» با «آنچه پایدار می‌شود» یکی باشد.
/// </para>
/// <para>
/// <b>Field policy.</b> کشور با trim و بی‌توجه به بزرگی/کوچکی حروف، بقیهٔ فیلدها با trim و
/// <see cref="StringComparison.Ordinal"/> مقایسه می‌شوند؛ فیلد خالی/فاصله‌ای هرگز به مقدار دیگر
/// تشبیه نمی‌شود.
/// </para>
/// </remarks>
public static class StorefrontAddressCanonical
{
    /// <summary>
    /// آیا دو تصویر نشانی از نظر کشور/استان/شهر/نشانی پستی/کد پستی/موبایل یکسان‌اند.
    /// </summary>
    /// <param name="country">کشور تصویر اول.</param>
    /// <param name="provinceName">استان تصویر اول.</param>
    /// <param name="cityName">شهر تصویر اول.</param>
    /// <param name="postalAddress">نشانی پستی تصویر اول.</param>
    /// <param name="postalCode">کد پستی تصویر اول.</param>
    /// <param name="contactMobile">موبایل تصویر اول.</param>
    /// <param name="expectedCountry">کشور مرجع.</param>
    /// <param name="expectedProvinceName">استان مرجع.</param>
    /// <param name="expectedCityName">شهر مرجع.</param>
    /// <param name="expectedPostalAddress">نشانی پستی مرجع.</param>
    /// <param name="expectedPostalCode">کد پستی مرجع.</param>
    /// <param name="expectedContactMobile">موبایل مرجع.</param>
    public static bool Same(
        string? country,
        string? provinceName,
        string? cityName,
        string? postalAddress,
        string? postalCode,
        string? contactMobile,
        string? expectedCountry,
        string? expectedProvinceName,
        string? expectedCityName,
        string? expectedPostalAddress,
        string? expectedPostalCode,
        string? expectedContactMobile) =>
        SameCountry(country, expectedCountry)
        && SameText(provinceName, expectedProvinceName)
        && SameText(cityName, expectedCityName)
        && SameText(postalAddress, expectedPostalAddress)
        && SameText(postalCode, expectedPostalCode)
        && SameText(contactMobile, expectedContactMobile);

    /// <summary>کشور: trim و بی‌توجه به حروف؛ خالی هرگز با خالیِ دیگر «تطبیق» نمی‌شود.</summary>
    /// <param name="country">کشور ورودی.</param>
    /// <param name="expected">کشور مرجع.</param>
    public static bool SameCountry(string? country, string? expected)
    {
        var left = Canonical(country);
        var right = Canonical(expected);
        return left is not null
            && right is not null
            && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>فیلد متنی: trim و مقایسهٔ Ordinal؛ مقدار تهی فقط با تهی برابر است.</summary>
    /// <param name="value">مقدار ورودی.</param>
    /// <param name="expected">مقدار مرجع.</param>
    public static bool SameText(string? value, string? expected) =>
        string.Equals(Canonical(value), Canonical(expected), StringComparison.Ordinal);

    private static string? Canonical(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
