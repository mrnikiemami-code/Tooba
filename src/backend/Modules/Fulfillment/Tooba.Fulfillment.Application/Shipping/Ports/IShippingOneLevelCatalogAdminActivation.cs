namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>
/// نتیجهٔ تغییر فعال‌بودن یک روش یک‌سطحی گزینه‌دار. هر حالت شکست صریح است تا هیچ نوشتنی خاموش رخ
/// ندهد و «موفق» هرگز برای دروازهٔ خاموش/کاتالوگ مهاجرت‌نشده/والد غیرفعال ادعا نشود.
/// </summary>
public enum ShippingOneLevelMethodActivationStatus
{
    /// <summary>گزینهٔ قدیمی و ردیف روش، هر دو در یک تراکنش هم‌راستا شدند.</summary>
    Applied = 0,

    /// <summary>وضعیت درخواستی از قبل برقرار بود؛ نوشتنی انجام نشد (idempotent).</summary>
    NoChange = 1,

    /// <summary>دروازهٔ کاتالوگ یک‌سطحی خاموش است ⇒ هیچ پرس‌وجو/نوشتنی انجام نمی‌شود.</summary>
    GateDisabled = 2,

    /// <summary>جدول کاتالوگ یک‌سطحی مهاجرت نشده است ⇒ نتیجهٔ کنترل‌شده، نه خطای ۵۰۰.</summary>
    CatalogNotReady = 3,

    /// <summary>کد روش خام/نامعتبر است و هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.</summary>
    InvalidMethodCode = 4,

    /// <summary>روشی با این کد canonical وجود ندارد.</summary>
    MethodNotFound = 5,

    /// <summary>پیوند قدیمی روش به سرویس (تغییرناپذیر) در کاتالوگ قدیمی پیدا نشد.</summary>
    LegacyBindingMissing = 6,

    /// <summary>سرویس والد قدیمی غیرفعال است؛ هرگز به‌صورت خودکار فعال نمی‌شود.</summary>
    LegacyParentInactive = 7,

    /// <summary>
    /// گزینهٔ قدیمیِ نگاشت‌شده در کاتالوگ قدیمی وجود ندارد (پیوند کهنه).
    /// </summary>
    /// <remarks>
    /// TB-SHIPPING-NEW-METHOD-ACTIVATION-170: دیگر بازگردانده نمی‌شود. نبودِ ردیف گزینه برای یک روش
    /// canonical یعنی روش «تازه و بدون پیوند گزینه» است و فعال‌سازی آن با
    /// <see cref="ActivatedCanonicalOnly"/> اعلام می‌شود؛ ردیف قدیمی جعلی ساخته نمی‌شود.
    /// </remarks>
    LegacyOptionMissing = 8,

    /// <summary>
    /// روش به سرویس بدون گزینه نگاشت شده است؛ فعال‌سازی آن یعنی وضعیت سطح سرویس و در این عملیات
    /// حدس زده نمی‌شود.
    /// </summary>
    OptionlessRequiresServiceDecision = 9,

    /// <summary>
    /// روش تازهٔ canonical (بدون ردیف گزینهٔ قدیمی) فقط در فرافکنی ادمین فعال/غیرفعال شد.
    /// </summary>
    /// <remarks>
    /// این وضعیت «قابل انتخاب بودن مشتری» یا «آمادهٔ قیمت» نیست: ردیف روش در Fulfillment به‌روز
    /// شده و هیچ ردیف <c>shipping_service_options</c> ساخته نشده است. دیده‌شدن در فروشگاه فقط با
    /// نرخ/پوشش واقعی مالک Shipping و دروازهٔ نرخ مالک ممکن است.
    /// </remarks>
    ActivatedCanonicalOnly = 10,
}

/// <summary>
/// نتیجهٔ تغییر فعال‌بودن روش یک‌سطحی. در شکست، هیچ نوشتنی رخ نداده است.
/// </summary>
/// <param name="Status">حالت صریح نتیجه.</param>
/// <param name="MethodCode">کد canonical روش (در کد نامعتبر تهی).</param>
/// <param name="RequestedIsActive">وضعیت درخواستی.</param>
/// <param name="EffectiveIsActive">وضعیت مؤثر پس از عملیات (در شکست، همان وضعیت فعلی یا درخواستی).</param>
public sealed record ShippingOneLevelMethodActivationResult(
    ShippingOneLevelMethodActivationStatus Status,
    string MethodCode,
    bool RequestedIsActive,
    bool EffectiveIsActive);

/// <summary>
/// تنها مرجع تغییر فعال‌بودن یک روش یک‌سطحی گزینه‌دار روی مالک Fulfillment.
/// </summary>
/// <remarks>
/// <para>
/// مرجع عملیاتی فعال‌سازی همچنان ستون‌های قدیمی <c>shipping_services.is_active</c> و
/// <c>shipping_service_options.is_active</c> است، چون فروشگاه همان‌ها را می‌خواند. ردیف یک‌سطحی
/// <c>shipping_methods.is_active</c> یک فرافکنی ادمین/کاتالوگ است و هرگز مرجع مستقل نمی‌شود؛ این درز
/// هر دو را در یک تراکنش هم‌راستا می‌کند تا هیچ‌گاه دو حقیقت متناقض نمایش داده نشود.
/// </para>
/// <para>
/// ورودی فقط کد تغییرناپذیر روش و وضعیت درخواستی است. پیوند قدیمی از سمت سرور و از خود ردیف روش
/// حل می‌شود؛ هیچ <c>serviceCode</c>، <c>optionCode</c> یا UUID گزینه‌ای از کلاینت پذیرفته نمی‌شود.
/// روش بدون گزینه (<c>LegacyOptionCode</c> تهی) و والد غیرفعال رد می‌شوند و هیچ گزینهٔ خواهر، ترتیب،
/// کد یا ترجمه‌ای بازنویسی نمی‌شود.
/// </para>
/// </remarks>
public interface IShippingOneLevelCatalogAdminActivation
{
    /// <summary>
    /// فعال‌بودن یک روش گزینه‌دار را روی گزینهٔ موجود کاتالوگ قدیمی و ردیف روش، در یک تراکنش،
    /// هم‌راستا می‌کند. اجرای دوباره با همان مقدار، بی‌اثر و idempotent است.
    /// </summary>
    /// <param name="methodCode">کد روش canonical انتخاب‌شده در UI.</param>
    /// <param name="isActive">وضعیت فعال‌بودن درخواستی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingOneLevelMethodActivationResult> SetActivationAsync(
        string methodCode,
        bool isActive,
        CancellationToken cancellationToken);
}
