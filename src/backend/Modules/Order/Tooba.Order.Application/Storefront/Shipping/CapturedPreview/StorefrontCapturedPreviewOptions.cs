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

    /// <summary>بیشینهٔ مجاز تعداد تاریخ‌های صریح پیش‌نمایش.</summary>
    public const int MaxAvailableDateCount = 90;

    /// <summary>آیا مسیر خواندنی پیش‌نمایش capture‌شده ساخته شود.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// آیا ثبت انتخاب capture‌شده (نویسندهٔ اتمیک ۲۳۲) مجاز است. پیش‌فرض خاموش.
    /// </summary>
    /// <remarks>
    /// این کلید فقط <b>نوشتن</b> انتخاب را باز می‌کند و هیچ قیمت/فعال‌سازی/تسویه‌ای نمی‌سازد:
    /// <c>CommitAsync</c> و مسیر پرداخت نهایی حتی با روشن‌بودن آن همچنان
    /// <see cref="StorefrontCapturedPreviewReasons.PreviewNotReady"/> می‌دهند. قصد capture‌شده با
    /// کلید خاموش صریحاً رد می‌شود و هرگز به مسیر قدیمی نمی‌افتد.
    /// </remarks>
    public bool SelectionEnabled { get; set; }

    /// <summary>
    /// تعداد صریح تاریخ‌های قابل ارائهٔ تقویم دریافت در این پیش‌نمایش (۰..۹۰).
    /// </summary>
    /// <remarks>
    /// این عدد سیاست <b>همین پیش‌نمایش</b> است و هرگز از <c>DeliveryHorizonDays</c> روش‌ها بازتفسیر
    /// نمی‌شود: «افق تقویم روش» و «تعداد تاریخ‌های قابل ارائه» دو معنای متفاوت دارند. وقتی کلید روشن
    /// است این مقدار الزامی است؛ نبود/خارج از محدوده یعنی پیش‌نمایش با دلیل صریح در دسترس نیست و هیچ
    /// فراخوانی‌ای انجام نمی‌شود.
    /// </remarks>
    public int? AvailableDateCount { get; set; }

    /// <summary>
    /// آیا <b>ثبت نهایی سفارش</b> برای پیش‌نویس دارای شاهد capture مجاز است. پیش‌فرض خاموش.
    /// </summary>
    /// <remarks>
    /// <para>
    /// این کلید تنها کلید ثبت نهایی است و هیچ‌چیز را فعال نمی‌کند: نبود/خاموشی آن، ثبت نهاییِ capture را
    /// حتی با روشن‌بودن <see cref="Enabled"/> و <see cref="SelectionEnabled"/> با
    /// <see cref="StorefrontCapturedPreviewReasons.PreviewNotReady"/> رد می‌کند.
    /// </para>
    /// <para>
    /// <b>Fail-closed.</b> روشن‌بودن این کلید مسیر واقعی submit را باز می‌کند، ولی تا وقتی بازاعتبارسنجی
    /// سرور کامل نشده باشد همان <see cref="StorefrontCapturedPreviewReasons.PreviewNotReady"/> برمی‌گردد؛
    /// هیچ شاهد capture‌شده‌ای بی‌اعتبارسنجی به سفارش تبدیل نمی‌شود.
    /// </para>
    /// </remarks>
    public bool CheckoutEnabled { get; set; }

    /// <summary>آیا مقدار تاریخ‌ها برای روشن‌بودن کلید معتبر است.</summary>
    public bool HasValidAvailableDateCount =>
        AvailableDateCount is int count and >= 0 and <= MaxAvailableDateCount;
}
