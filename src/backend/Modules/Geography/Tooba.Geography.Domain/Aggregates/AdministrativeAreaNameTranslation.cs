using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// نام محلی‌شدهٔ یک ناحیهٔ اداری. هویت مکان شناسهٔ پایدار ناحیه است و این موجودیت فقط برچسب
/// نمایشی را با کلید <c>LanguageId</c> نگه‌داری می‌کند؛ مالکیت فهرست زبان‌ها بر عهدهٔ Localization است.
/// </summary>
public sealed class AdministrativeAreaNameTranslation
{
    /// <summary>حداکثر طول نام محلی‌شده.</summary>
    public const int NameMaxLength = 128;

    private AdministrativeAreaNameTranslation()
    {
    }

    /// <summary>شناسهٔ پایدار سطر ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>ناحیهٔ اداری مالک؛ کلید خارجی واقعی به <c>administrative_areas</c>.</summary>
    public Guid AdministrativeAreaId { get; private set; }

    /// <summary>شناسهٔ زبان متنی؛ یک هویت خارجی اسکالر، نه وابستگی به DbContext دیگر.</summary>
    public Guid LanguageId { get; private set; }

    /// <summary>نام محلی‌شدهٔ ناحیه.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>سطر ترجمهٔ جدید می‌سازد؛ یکتایی (ناحیه، زبان) در لایهٔ پایدارسازی اعمال می‌شود.</summary>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری مالک.</param>
    /// <param name="languageId">شناسهٔ زبان.</param>
    /// <param name="name">نام محلی‌شده.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="translationId">شناسهٔ قطعی اختیاری.</param>
    public static AdministrativeAreaNameTranslation Create(
        Guid administrativeAreaId,
        Guid languageId,
        string name,
        DateTimeOffset now,
        Guid? translationId = null)
        => new()
        {
            TranslationId = translationId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            AdministrativeAreaId = RequireOwner(administrativeAreaId),
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

    private static Guid RequireOwner(Guid administrativeAreaId)
    {
        if (administrativeAreaId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.TranslationPlaceIdRequired));
        }

        return administrativeAreaId;
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
