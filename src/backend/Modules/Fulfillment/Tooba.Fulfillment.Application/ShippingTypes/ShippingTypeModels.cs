using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.ShippingTypes;

/// <summary>
/// یک ورودی ترجمه در بدنهٔ نوشتن نوع ارسال. <see cref="Description"/> سه حالت دارد: مقدار، <c>null</c>
/// برای پاک‌کردن همان توضیح، و «ارسال‌نشده» که در transport به‌صورت عدم حضور کلید مدل می‌شود.
/// </summary>
/// <param name="LanguageId">شناسهٔ زبان محتوا.</param>
/// <param name="Name">نام غیرتهی.</param>
/// <param name="Description">توضیح یا <c>null</c> برای پاک‌کردن.</param>
public sealed record ShippingTypeTranslationInput(Guid LanguageId, string Name, string? Description);

/// <summary>مدل نوشتن ایجاد نوع ارسال؛ شناسه/فعال‌بودن/بازبینی از سرور است.</summary>
/// <param name="Code">کد canonical.</param>
/// <param name="SortOrder">ترتیب صریح.</param>
/// <param name="Translations">ترجمه‌های اولیه؛ باید نام زبان پیش‌فرض فعال را داشته باشد.</param>
public sealed record ShippingTypeCreateModel(
    string Code,
    int SortOrder,
    IReadOnlyList<ShippingTypeTranslationInput> Translations);

/// <summary>
/// مدل نوشتن ویرایش نوع ارسال. ترجمه‌ها فقط زبان‌های صریحاً فهرست‌شده را patch می‌کنند و
/// فهرست خالی همهٔ متن‌های موجود را دست‌نخورده نگه می‌دارد.
/// </summary>
/// <param name="ExpectedRevision">بازبینی خوانده‌شده؛ باید بزرگ‌تر از صفر باشد.</param>
/// <param name="Code">کد ارسالی در بدنه؛ اگر ارسال شود باید با کد ذخیره‌شده یکسان باشد (هویت تغییرناپذیر).</param>
/// <param name="SortOrder">ترتیب صریح.</param>
/// <param name="IsActive">وضعیت فعال‌بودن صریح.</param>
/// <param name="Translations">ترجمه‌های patch.</param>
public sealed record ShippingTypeUpdateModel(
    long ExpectedRevision,
    string? Code,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<ShippingTypeTranslationInput> Translations);

/// <summary>
/// حقایق زبان authoritative که Application برای اعتبارسنجی متن نوع ارسال لازم دارد.
/// </summary>
/// <remarks>
/// این عکس فوری از رجیستری واقعی زبان ساخته می‌شود (شامل ردیف‌های غیرفعال) و هیچ زبان/پیش‌فرضی از
/// locale درخواست یا فهرست ثابت fa/en استنتاج نمی‌شود.
/// </remarks>
/// <param name="Languages">همهٔ زبان‌های شناخته‌شده با فعال‌بودن و پیش‌فرض‌بودن authoritative.</param>
public sealed record ShippingTypeLanguageFacts(
    IReadOnlyList<ShippingTypeLanguageFact> Languages)
{
    /// <summary>زبان‌های فعال.</summary>
    public IReadOnlyList<ShippingTypeLanguageFact> ActiveLanguages =>
        Languages.Where(x => x.IsActive).ToList();

    /// <summary>پیش‌فرض فعلی محتوا؛ تهی یعنی رجیستری برای نوشتن/فعال‌سازی قابل‌استفاده نیست.</summary>
    public ShippingTypeLanguageFact? ActiveDefault =>
        Languages.Where(x => x.IsActive && x.IsDefault).OrderBy(x => x.SortOrder).FirstOrDefault();

    /// <summary>آیا شناسه در رجیستری شناخته شده است.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    public bool Contains(Guid languageId) => Languages.Any(x => x.LanguageId == languageId);

    /// <summary>آیا زبان شناخته و فعال است.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    public bool IsActive(Guid languageId) => Languages.Any(x => x.LanguageId == languageId && x.IsActive);
}

/// <summary>یک ردیف زبان authoritative.</summary>
/// <param name="LanguageId">شناسهٔ زبان.</param>
/// <param name="IsActive">فعال‌بودن اعلام‌شدهٔ مالک.</param>
/// <param name="IsDefault">پیش‌فرض‌بودن اعلام‌شدهٔ مالک.</param>
/// <param name="SortOrder">ترتیب اعلام‌شدهٔ مالک.</param>
public sealed record ShippingTypeLanguageFact(Guid LanguageId, bool IsActive, bool IsDefault, int SortOrder);

/// <summary>درز خواندن حقایق زبان برای نوشتن نوع ارسال.</summary>
public interface IShippingTypeLanguageFacts
{
    /// <summary>عکس فوری حقایق زبان؛ شکست زیرساخت استثنا می‌ماند.</summary>
    Task<ShippingTypeLanguageFacts> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>یک ردیف خلاصهٔ کاتالوگ نوع ارسال برای فهرست مدیریتی.</summary>
/// <param name="ShippingTypeId">شناسه.</param>
/// <param name="Code">کد canonical.</param>
/// <param name="IsActive">فعال؟</param>
/// <param name="SortOrder">ترتیب.</param>
/// <param name="Revision">بازبینی.</param>
/// <param name="Translations">ترجمه‌ها.</param>
public sealed record ShippingTypeSummaryView(
    Guid ShippingTypeId,
    string Code,
    bool IsActive,
    int SortOrder,
    long Revision,
    IReadOnlyList<ShippingTypeTranslationSnapshot> Translations);

/// <summary>نمای کامل یک نوع ارسال برای مسیر جزئیات/پاسخ نوشتن.</summary>
/// <param name="ShippingTypeId">شناسه.</param>
/// <param name="Code">کد canonical.</param>
/// <param name="IsActive">فعال؟</param>
/// <param name="SortOrder">ترتیب.</param>
/// <param name="Revision">بازبینی.</param>
/// <param name="Translations">ترجمه‌ها.</param>
/// <param name="DefaultLanguageId">زبان پیش‌فرض فعال همین لحظه یا <c>null</c>.</param>
/// <param name="HasDefaultLanguageName">آیا نام پیش‌فرض فعلی موجود است؛ حدس ترجمه نمی‌زنیم.</param>
public sealed record ShippingTypeView(
    Guid ShippingTypeId,
    string Code,
    bool IsActive,
    int SortOrder,
    long Revision,
    IReadOnlyList<ShippingTypeTranslationSnapshot> Translations,
    Guid? DefaultLanguageId,
    bool HasDefaultLanguageName);

/// <summary>
/// درز پایدارسازی/خواندن کاتالوگ نوع ارسال در مالکیت Fulfillment.
/// </summary>
public interface IShippingTypeStore
{
    /// <summary>ایجاد غیرفعال با بازبینی ۱؛ شناسهٔ سرورساخته برمی‌گردد.</summary>
    Task<Guid> CreateAsync(ShippingTypeCreateModel model, CancellationToken cancellationToken);

    /// <summary>جایگزینی ترتیب/فعال‌بودن و patch ترجمه‌ها با predicate بازبینی.</summary>
    Task<ShippingTypeView> UpdateAsync(
        Guid shippingTypeId,
        ShippingTypeUpdateModel model,
        CancellationToken cancellationToken);

    /// <summary>فهرست کامل برای مدیریت (شامل غیرفعال‌ها).</summary>
    Task<IReadOnlyList<ShippingTypeSummaryView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>یک تعریف یا <c>null</c> وقتی وجود ندارد.</summary>
    Task<ShippingTypeView?> GetAsync(Guid shippingTypeId, CancellationToken cancellationToken);
}
