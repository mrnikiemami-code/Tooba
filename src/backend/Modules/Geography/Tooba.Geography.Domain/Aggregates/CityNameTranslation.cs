using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// نام محلی‌شدهٔ یک شهر. هویت مکان شناسهٔ پایدار شهر است و این موجودیت فقط برچسب نمایشی را
/// با کلید <c>LanguageId</c> نگه‌داری می‌کند؛ مالکیت فهرست زبان‌ها بر عهدهٔ Localization است.
/// </summary>
public sealed class CityNameTranslation
{
    /// <summary>حداکثر طول نام محلی‌شده.</summary>
    public const int NameMaxLength = 128;

    private CityNameTranslation()
    {
    }

    /// <summary>شناسهٔ پایدار سطر ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>شهر مالک؛ کلید خارجی واقعی به <c>cities</c>.</summary>
    public Guid CityId { get; private set; }

    /// <summary>شناسهٔ زبان متنی؛ یک هویت خارجی اسکالر، نه وابستگی به DbContext دیگر.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>نام محلی‌شدهٔ شهر.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>سطر ترجمهٔ جدید می‌سازد؛ یکتایی (شهر، زبان) در لایهٔ پایدارسازی اعمال می‌شود.</summary>
    /// <param name="cityId">شناسهٔ شهر مالک.</param>
    /// <param name="languageId">شناسهٔ زبان.</param>
    /// <param name="name">نام محلی‌شده.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="translationId">شناسهٔ قطعی اختیاری.</param>
    public static CityNameTranslation Create(
        Guid cityId,
        Guid languageId,
        string name,
        DateTimeOffset now,
        Guid? translationId = null)
        => new()
        {
            TranslationId = translationId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            CityId = RequireOwner(cityId),
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

    private static Guid RequireOwner(Guid cityId)
    {
        if (cityId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.TranslationPlaceIdRequired));
        }

        return cityId;
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
