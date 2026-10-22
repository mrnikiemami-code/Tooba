using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// یک ترجمهٔ مالک‌محور نام/توضیح یک پیش‌نویس برای <b>یک</b> زبان محتوای authoritative.
/// </summary>
/// <remarks>
/// این ردیف کاتالوگ Fulfillment نیست و هیچ ترجمهٔ مشترک/هویت روش را تکرار یا اصلاح نمی‌کند؛ فقط
/// ارائهٔ مالک همین پیش‌نویس است. کلید زبان شناسهٔ واقعی Localization است، نه کد نمایشی یا فرهنگ
/// مرورگر. متن ذخیره‌شده «خام» است: هیچ fallback، transliteration یا ترجمهٔ ساختگی اینجا نیست.
/// </remarks>
public sealed class ShippingAuthoringDraftTranslation
{
    private ShippingAuthoringDraftTranslation()
    {
    }

    /// <summary>شناسهٔ پیش‌نویس مالک این متن.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>شناسهٔ پایدار زبان محتوای مرجع.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>نام روش ارسال برای همین زبان.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>توضیح اختیاری همین زبان؛ تهی یعنی توضیحی برای این زبان ثبت نشده است.</summary>
    public string? Description { get; private set; }

    internal static ShippingAuthoringDraftTranslation Create(
        Guid draftId,
        Guid languageId,
        string name,
        string? description)
        => new()
        {
            DraftId = draftId,
            LanguageId = languageId,
            Name = name,
            Description = description,
        };

    /// <summary>نام/توضیح همین زبان را جایگزین می‌کند و زبان‌های دیگر را دست نمی‌زند.</summary>
    /// <param name="name">نام تأییدشده.</param>
    /// <param name="description">توضیح تأییدشده یا تهی برای پاک‌کردن.</param>
    internal void Apply(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}

/// <summary>
/// عنوان اختیاری یک گروه منطقه‌ای برای یک زبان محتوای authoritative. عنوان صرفاً ارائه است و در
/// قواعد، ارز، نرخ یا انتخاب جغرافیایی هیچ نقشی ندارد.
/// </summary>
public sealed class ShippingAuthoringRegionGroupTitle
{
    private ShippingAuthoringRegionGroupTitle()
    {
    }

    /// <summary>گروه مالک (همان هویت پایدار جغرافیا).</summary>
    public Guid GroupId { get; private set; }

    /// <summary>
    /// شناسهٔ پیش‌نویس denormalized؛ همان قاعدهٔ یکتایی/جداسازی که انتخاب‌ها دارند و هیچ FK دومی
    /// به drafts نمی‌سازد.
    /// </summary>
    public Guid DraftId { get; private set; }

    /// <summary>شناسهٔ پایدار زبان محتوای مرجع.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>عنوان ذخیره‌شدهٔ همین زبان.</summary>
    public string Title { get; private set; } = string.Empty;

    internal static ShippingAuthoringRegionGroupTitle Create(
        Guid draftId,
        Guid groupId,
        Guid languageId,
        string title)
        => new()
        {
            DraftId = draftId,
            GroupId = groupId,
            LanguageId = languageId,
            Title = title,
        };

    /// <summary>عنوان همین زبان را جایگزین می‌کند.</summary>
    /// <param name="title">عنوان تأییدشده.</param>
    internal void Apply(string title) => Title = title;
}

/// <summary>
/// قواعد شکل متن ارائهٔ نویسندگی. محدودیت‌ها <b>فنی payload</b> هستند (هم‌تراز با قراردادهای
/// طول کاتالوگ/ویرایشگر)، نه سقف تجاری یا سیاست قیمت/ارز.
/// </summary>
public static class ShippingAuthoringPresentationRules
{
    /// <summary>حداکثر طول نام روش در یک زبان.</summary>
    public const int NameMaxLength = 200;

    /// <summary>حداکثر طول توضیح در یک زبان.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>حداکثر طول عنوان یک گروه در یک زبان.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>نام یک زبان باید متن غیرخالی و در محدودهٔ طول باشد.</summary>
    /// <param name="name">نام خام.</param>
    public static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > NameMaxLength)
        {
            Reject();
        }

        return trimmed;
    }

    /// <summary>توضیح خالی/تهی یعنی «برای این زبان توضیحی نیست»، نه متن ساختگی.</summary>
    /// <param name="description">توضیح خام.</param>
    public static string? NormalizeDescription(string? description)
    {
        var trimmed = description?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > DescriptionMaxLength)
        {
            Reject();
        }

        return trimmed;
    }

    /// <summary>عنوان خالی/تهی یعنی پاک‌کردن عنوان همین زبان.</summary>
    /// <param name="title">عنوان خام.</param>
    public static string? NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > TitleMaxLength)
        {
            Reject();
        }

        return trimmed;
    }

    private static void Reject()
        => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
}
