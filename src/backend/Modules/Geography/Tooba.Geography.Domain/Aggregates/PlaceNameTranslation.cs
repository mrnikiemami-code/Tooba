using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;
using Tooba.Geography.Domain.Enums;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// سطر نام محلی‌شدهٔ یک مکان. هویت مکان شناسهٔ پایدار است و این سطر فقط برچسب نمایشی را
/// با کلید <c>LanguageId</c> نگه‌داری می‌کند. تا زمانی که تصمیم D9 به‌طور کامل روشن شود،
/// مالکیت ترجمه در همین ماژول است و هیچ سرویس ترجمهٔ عمومی ساخته نمی‌شود.
/// </summary>
public sealed class PlaceNameTranslation
{
    /// <summary>حداکثر طول نام محلی‌شده.</summary>
    public const int NameMaxLength = 128;

    private PlaceNameTranslation()
    {
    }

    /// <summary>شناسهٔ پایدار سطر ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>نوع مکان مرجعی که این سطر نام آن را ترجمه می‌کند.</summary>
    public GeographyPlaceKind PlaceKind { get; private set; }

    /// <summary>شناسهٔ پایدار مکان مرجع.</summary>
    public Guid PlaceId { get; private set; }

    /// <summary>شناسهٔ زبان؛ مالکیت فهرست زبان‌ها بر عهدهٔ ماژول Localization است.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>نام محلی‌شدهٔ مکان.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>سطر ترجمهٔ جدید می‌سازد. یکتایی (PlaceKind، PlaceId، LanguageId) در لایهٔ پایدارسازی اعمال می‌شود.</summary>
    /// <param name="placeKind">نوع مکان مرجع.</param>
    /// <param name="placeId">شناسهٔ پایدار مکان مرجع.</param>
    /// <param name="languageId">شناسهٔ زبان.</param>
    /// <param name="name">نام محلی‌شده.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="translationId">شناسهٔ قطعی اختیاری.</param>
    public static PlaceNameTranslation Create(
        GeographyPlaceKind placeKind,
        Guid placeId,
        Guid languageId,
        string name,
        DateTimeOffset now,
        Guid? translationId = null)
        => new()
        {
            TranslationId = translationId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            PlaceKind = placeKind,
            PlaceId = RequirePlace(placeId),
            LanguageId = RequireLanguage(languageId),
            Name = NormalizeName(name),
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>نام محلی‌شده را با همان قواعد اعتبارسنجی جایگزین می‌کند.</summary>
    /// <param name="name">نام محلی‌شده.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Rename(string name, DateTimeOffset now)
    {
        Name = NormalizeName(name);
        UpdatedAt = now;
    }

    private static Guid RequirePlace(Guid placeId)
    {
        if (placeId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.TranslationPlaceIdRequired));
        }

        return placeId;
    }

    private static Guid RequireLanguage(Guid languageId)
    {
        if (languageId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.TranslationLanguageIdRequired));
        }

        return languageId;
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > NameMaxLength)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.TranslationNameRequired));
        }

        return trimmed;
    }
}
