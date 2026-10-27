namespace Tooba.Order.Application.Storefront.Shipping.CapturedPreview;

/// <summary>
/// تنظیمات پیش‌نمایش خواندنی «گروه‌های ارسال capture‌شده».
/// </summary>
/// <remarks>
/// <para>
/// <b>Default OFF.</b> نصب‌هایی که این بخش را فعال نکرده‌اند همان مسیر قدیمی را عیناً می‌بینند: صفر
/// فراخوانی مالک/Offer/219/218 و صفر تغییر در فهرست روش‌ها یا قیمت‌ها.
/// </para>
/// <para>
/// روشن‌بودن این کلید یعنی مسیر <b>خواندنی</b> پیش‌نمایش فعال است؛ یعنی هیچ‌چیز قابل انتخاب/ثبت
/// نیست: انتخاب و ثبت سفارش صریحاً تا برش بعدی (parity + شواهد فشردهٔ سفارش) رد می‌شوند. این کلید جای
/// TB185 یا دروازهٔ قدیمی را نمی‌گیرد و هیچ نرخ/تقویم/قیمتی از مسیر قدیمی را تغییر نمی‌دهد.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedPreviewOptions
{
    /// <summary>نام بخش تنظیمات.</summary>
    public const string SectionName = "Tooba:StorefrontCapturedPreview";

    /// <summary>آیا مسیر خواندنی پیش‌نمایش capture‌شده ساخته شود.</summary>
    public bool Enabled { get; set; }
}

