namespace Tooba.Fulfillment.Contracts.Shipping;

/// <summary>
/// یک ترجمهٔ خام و ذخیره‌شدهٔ نوع ارسال برای یک زبان محتوا.
/// </summary>
/// <remarks>
/// شناسهٔ زبان همان <c>LanguageId</c> واقعی رجیستری Localization است؛ این قرارداد هیچ نوع EF، DbContext یا
/// رشتهٔ زبان هاردکدشده ندارد. متن یک زبان غیرفعال حذف نمی‌شود و هنگام فعال‌شدن دوباره خوانده می‌شود.
/// </remarks>
/// <param name="LanguageId">شناسهٔ پایدار زبان محتوا.</param>
/// <param name="Name">نام غیرتهی نمایشی.</param>
/// <param name="Description">توضیح اختیاری.</param>
public sealed record ShippingTypeTranslationSnapshot(Guid LanguageId, string Name, string? Description);

/// <summary>
/// عکس فوری فقط-خواندنی یک تعریف نوع ارسال عملیاتی مشترک.
/// </summary>
/// <remarks>
/// این تعریف فقط هویت و متن چندزبانه را حمل می‌کند: هیچ نرخ، جغرافیا، بسته‌بندی، برنامه، ارز، پرداخت،
/// ظرفیت، مزیت عضویت یا وعدهٔ تحویل در آن نیست. فعال‌بودن یک تعریف به‌تنهایی هیچ روش ارسالی را برای
/// مشتری قابل انتخاب نمی‌کند.
/// </remarks>
/// <param name="ShippingTypeId">شناسهٔ پایدار سرورساخته.</param>
/// <param name="Code">کد canonical تغییرناپذیر.</param>
/// <param name="IsActive">وضعیت فعال‌بودن صریح.</param>
/// <param name="SortOrder">ترتیب نمایش صریح.</param>
/// <param name="Revision">شمارهٔ بازبینی یکنوا.</param>
/// <param name="Translations">ترجمه‌های خام ذخیره‌شده به‌ازای زبان.</param>
public sealed record ShippingTypeSnapshot(
    Guid ShippingTypeId,
    string Code,
    bool IsActive,
    int SortOrder,
    long Revision,
    IReadOnlyList<ShippingTypeTranslationSnapshot> Translations);

/// <summary>
/// درز فقط-خواندنی Fulfillment برای نوع‌های ارسال عملیاتی.
/// </summary>
/// <remarks>
/// این درز برای مصرف آیندهٔ Shipping (انتساب مالک‌محور) و نمایش تاریخی طراحی شده است. هیچ نوشتنی در آن
/// نیست و هیچ نوع EF/DbContext افشا نمی‌شود. کاتالوگ خالی معتبر با «دسترسی‌نبودن/مهاجرت‌نشده» یکی نیست:
/// شکست زیرساخت یا لغو استثنا می‌ماند و به فهرست خالی تبدیل نمی‌شود.
/// </remarks>
public interface IShippingTypeReader
{
    /// <summary>تعریف‌های فعال، مرتب‌شده بر اساس ترتیب اعلام‌شده و سپس شناسه.</summary>
    Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// تعریف‌های درخواستی با همان شناسه‌ها؛ شناسهٔ ناموجود در نتیجه نمی‌آید. عمداً می‌تواند ردیف غیرفعال
    /// را برای نمایش صادقانهٔ تاریخی برگرداند؛ انتخاب عملیاتی باید <c>IsActive</c> را جدا بررسی کند.
    /// </summary>
    Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
        IReadOnlyCollection<Guid> shippingTypeIds,
        CancellationToken cancellationToken);
}
