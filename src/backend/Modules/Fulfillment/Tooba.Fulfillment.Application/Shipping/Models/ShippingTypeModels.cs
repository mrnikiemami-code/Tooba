using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;

namespace Tooba.Fulfillment.Application.Shipping.Models;

/// <summary>
/// ورودی ترجمه در بدنهٔ نوشتن، با حفظ کامل حضور/تهی‌بودن مقادیر خام.
/// </summary>
/// <remarks>
/// هیچ فیلتری روی این ورودی انجام نمی‌شود: شناسهٔ خالی، نام تهی و فهرست حاوی عنصر <c>null</c> تا زمان
/// اعتبارسنجی دست‌نخورده می‌مانند تا کل درخواست رد شود، نه اینکه بخشی از آن‌ها بی‌صدا ذخیره شود.
/// </remarks>
/// <param name="LanguageId">شناسهٔ زبان محتوا؛ <c>null</c> یعنی کلید ارسال نشده است.</param>
/// <param name="Name">نام؛ <c>null</c> یعنی ارسال نشده یا صریحاً null.</param>
/// <param name="Description">توضیح؛ <c>null</c> یعنی پاک‌کردن همان توضیح.</param>
public sealed record ShippingTypeTranslationInput(Guid? LanguageId, string? Name, string? Description);

/// <summary>
/// مدل نوشتن نوع ارسال در مرز transport با حضور‌آگاهی کامل.
/// </summary>
/// <param name="Code">کد canonical؛ <c>null</c> یعنی ارسال نشده.</param>
/// <param name="SortOrder">ترتیب صریح؛ <c>null</c> یعنی ارسال نشده (نباید صفر شود).</param>
/// <param name="IsActive">وضعیت صریح؛ <c>null</c> یعنی ارسال نشده (نباید false شود).</param>
/// <param name="ExpectedRevision">بازبینی خوانده‌شده؛ <c>null</c> یعنی ارسال نشده.</param>
/// <param name="Translations">ترجمه‌ها؛ عنصر <c>null</c> حفظ می‌شود و کل درخواست را رد می‌کند.</param>
public sealed record ShippingTypeWriteModel(
    string? Code,
    int? SortOrder,
    bool? IsActive,
    long? ExpectedRevision,
    IReadOnlyList<ShippingTypeTranslationInput?>? Translations);

/// <summary>ترجمهٔ آماده/نرمال‌شده پس از پیش‌بررسی کامل.</summary>
/// <param name="LanguageId">شناسهٔ زبان غیرتهی.</param>
/// <param name="Name">نام نرمال‌شدهٔ غیرتهی.</param>
/// <param name="Description">توضیح نرمال‌شده یا <c>null</c>.</param>
public sealed record ShippingTypePreparedTranslation(Guid LanguageId, string Name, string? Description);

/// <summary>وضعیت زبان‌های حل‌شده و قابل‌اعتماد برای این capability.</summary>
/// <param name="ActiveDefaultLanguageId">تنها پیش‌فرض فعال.</param>
/// <param name="ActiveLanguageIds">مجموعهٔ زبان‌های فعال.</param>
public sealed record ShippingTypeLanguageResolution(
    Guid ActiveDefaultLanguageId,
    IReadOnlyList<Guid> ActiveLanguageIds)
{
    /// <summary>آیا شناسه جزو زبان‌های فعال است.</summary>
    /// <param name="languageId">شناسهٔ زبان.</param>
    public bool IsActive(Guid languageId) => ActiveLanguageIds.Contains(languageId);
}

/// <summary>یک ردیف زبان authoritative.</summary>
/// <param name="LanguageId">شناسهٔ زبان.</param>
/// <param name="IsActive">فعال‌بودن اعلام‌شدهٔ مالک.</param>
/// <param name="IsDefault">پیش‌فرض‌بودن اعلام‌شدهٔ مالک.</param>
/// <param name="SortOrder">ترتیب اعلام‌شدهٔ مالک.</param>
public sealed record ShippingTypeLanguageFact(Guid LanguageId, bool IsActive, bool IsDefault, int SortOrder);

/// <summary>
/// حقایق زبان authoritative که Application برای اعتبارسنجی متن نوع ارسال لازم دارد.
/// </summary>
/// <remarks>
/// این عکس فوری از رجیستری واقعی زبان ساخته می‌شود (شامل ردیف‌های غیرفعال) و هیچ زبان/پیش‌فرضی از
/// locale درخواست یا فهرست ثابت fa/en استنتاج نمی‌شود. حل این حقایق عمداً صریح و سخت‌گیرانه است: صفر یا
/// چند پیش‌فرض، شناسهٔ تکراری/تهی و مجموعهٔ فعال خالی قابل‌استفاده نیستند و fail-closed می‌شوند — هیچ
/// انتخاب «اولین» یا ترتیب بدلی انجام نمی‌شود.
/// </remarks>
/// <param name="Languages">همهٔ زبان‌های شناخته‌شده.</param>
public sealed record ShippingTypeLanguageFacts(IReadOnlyList<ShippingTypeLanguageFact> Languages)
{
    /// <summary>حقایق را حل می‌کند یا به‌صورت canonical رد می‌کند.</summary>
    /// <exception cref="SemanticException">رجیستری برای نوشتن متن قابل‌اعتماد نیست.</exception>
    public ShippingTypeLanguageResolution Resolve()
    {
        ArgumentNullException.ThrowIfNull(Languages);

        foreach (var language in Languages)
        {
            if (language is null || language.LanguageId == Guid.Empty)
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }
        }

        if (Languages.Select(x => x.LanguageId).Distinct().Count() != Languages.Count)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
        }

        var active = Languages.Where(x => x.IsActive).ToList();
        if (active.Count == 0)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
        }

        // پیش‌فرض باید دقیقاً یکی باشد: صفر یعنی پیکربندی ناقص، چند یعنی مبهم. هیچ‌کدام حدس‌زدنی نیست.
        var defaults = Languages.Where(x => x.IsDefault).ToList();
        if (defaults.Count > 1)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageAmbiguous);
        }

        if (defaults.Count == 0 || !defaults[0].IsActive)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
        }

        return new ShippingTypeLanguageResolution(
            defaults[0].LanguageId,
            active.Select(x => x.LanguageId).ToList());
    }

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}

/// <summary>درز خواندن حقایق زبان برای نوشتن/خواندن نوع ارسال.</summary>
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
/// <remarks>
/// ورودی این درز از قبل در Application پیش‌بررسی و نرمال شده است. پیاده‌سازی مسئول اعمال اتمیک است: هیچ
/// تغییر tracked پیش از اعتبارسنجی کامل کل وضعیت پیشنهادی انجام نمی‌شود.
/// </remarks>
public interface IShippingTypeStore
{
    /// <summary>ایجاد غیرفعال با بازبینی ۱؛ شناسهٔ سرورساخته برمی‌گردد.</summary>
    Task<Guid> CreateAsync(
        string code,
        int sortOrder,
        IReadOnlyList<ShippingTypePreparedTranslation> translations,
        CancellationToken cancellationToken);

    /// <summary>جایگزینی ترتیب/فعال‌بودن و patch ترجمه‌ها با predicate بازبینی.</summary>
    Task<ShippingTypeView> UpdateAsync(
        Guid shippingTypeId,
        long expectedRevision,
        string? submittedCode,
        int sortOrder,
        bool isActive,
        IReadOnlyList<ShippingTypePreparedTranslation> translations,
        Guid activeDefaultLanguageId,
        CancellationToken cancellationToken);

    /// <summary>فهرست کامل برای مدیریت (شامل غیرفعال‌ها).</summary>
    Task<IReadOnlyList<ShippingTypeSummaryView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>یک تعریف یا <c>null</c> وقتی وجود ندارد.</summary>
    Task<ShippingTypeView?> GetAsync(
        Guid shippingTypeId,
        Guid? activeDefaultLanguageId,
        CancellationToken cancellationToken);
}

/// <summary>قاعدهٔ مشترک آماده‌سازی/اعتبارسنجی canonical متن و سرصفحهٔ نوع ارسال.</summary>
/// <remarks>
/// این تنها موتور قاعده برای این capability است: هم handler و هم درز ذخیره پیش از هر تغییر tracked همین
/// متد را صدا می‌زنند. دامنه به Contracts وابسته نمی‌شود، بنابراین کدهای خطای مالک‌شده فقط در این‌جا هستند.
/// </remarks>
public static class ShippingTypePreparation
{
    /// <summary>
    /// همهٔ ترجمه‌ها را پیش از هر تغییر tracked نرمال/اعتبارسنجی می‌کند: زبان فعال و شناخته‌شده، بدون
    /// تکرار، نام غیرتهی و طول‌های مجاز. هیچ عنصر نامعتبر نادیده گرفته نمی‌شود.
    /// </summary>
    public static IReadOnlyList<ShippingTypePreparedTranslation> Prepare(
        IReadOnlyList<ShippingTypeTranslationInput?>? translations,
        ShippingTypeLanguageResolution languages)
    {
        ArgumentNullException.ThrowIfNull(languages);

        if (translations is null)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }

        var prepared = new List<ShippingTypePreparedTranslation>(translations.Count);
        var seen = new HashSet<Guid>();

        foreach (var entry in translations)
        {
            if (entry is null)
            {
                // یک عنصر null کل درخواست را رد می‌کند؛ هرگز بقیهٔ فهرست ذخیره نمی‌شود.
                throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
            }

            if (entry.LanguageId is not { } languageId
                || languageId == Guid.Empty
                || !languages.IsActive(languageId))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            if (!seen.Add(languageId))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            var (name, description) = NormalizeText(languageId, entry.Name, entry.Description);
            prepared.Add(new ShippingTypePreparedTranslation(languageId, name, description));
        }

        return prepared;
    }

    /// <summary>
    /// قاعدهٔ واحد نرمال‌سازی متن ترجمه: شناسهٔ غیرتهی، نام غیرتهی، طول‌های مجاز و <c>null</c> برای توضیح
    /// خالی. هر دو مقدار با هم برگردانده می‌شوند تا هیچ انتساب نیمه‌کاره ممکن نباشد.
    /// </summary>
    public static (string Name, string? Description) NormalizeText(
        Guid languageId,
        string? name,
        string? description)
    {
        if (languageId == Guid.Empty || string.IsNullOrWhiteSpace(name))
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > ShippingType.NameMaxLength)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }

        string? trimmedDescription = null;
        if (!string.IsNullOrWhiteSpace(description))
        {
            trimmedDescription = description.Trim();
            if (trimmedDescription.Length > ShippingType.DescriptionMaxLength)
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
            }
        }

        return (trimmedName, trimmedDescription);
    }

    /// <summary>کد canonical را الزامی اعتبارسنجی می‌کند (بدون trim/بازنویسی).</summary>
    public static string RequireCode(string? code) =>
        ShippingType.TryNormalizeCode(code) ?? throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);

    /// <summary>ترتیب را در بازهٔ مجاز الزامی می‌کند.</summary>
    public static int RequireSortOrder(int? sortOrder) =>
        sortOrder is { } value
        && value >= ShippingType.SortOrderMinValue
        && value <= ShippingType.SortOrderMaxValue
            ? value
            : throw Fault(FulfillmentErrorCodes.ShippingTypeSortOrderInvalid);

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}
