using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// کشور به‌عنوان برگهٔ ریشهٔ هویت مکانی. کد کشور پایدار و مستقل از زبان است و نام محلی
/// در سطرهای ترجمه نگه‌داری می‌شود.
/// </summary>
public sealed class Country
{
    /// <summary>حداقل طول کد کشور (ISO 3166-1 alpha-2).</summary>
    public const int CountryCodeMinLength = 2;

    /// <summary>حداکثر طول کد کشور (ISO 3166-1 alpha-3).</summary>
    public const int CountryCodeMaxLength = 3;

    private Country()
    {
    }

    /// <summary>شناسهٔ پایدار کشور.</summary>
    public Guid CountryId { get; init; }

    /// <summary>کد کشور مستقل از زبان.</summary>
    public string CountryCode { get; private set; } = string.Empty;

    /// <summary>آیا کشور در فهرست فعال است؛ هیچ رکوردی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>کشور فعال جدید می‌سازد؛ شناسه در صورت نبود تولید می‌شود.</summary>
    /// <param name="countryCode">کد کشور مستقل از زبان.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="countryId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static Country Create(string countryCode, DateTimeOffset now, Guid? countryId = null)
        => new()
        {
            CountryId = countryId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            CountryCode = NormalizeCode(countryCode),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>کد کشور را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="countryCode">کد کشور مستقل از زبان.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void UpdateCode(string countryCode, DateTimeOffset now)
    {
        CountryCode = NormalizeCode(countryCode);
        UpdatedAt = now;
    }

    /// <summary>کشور را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>کشور را غیرفعال می‌کند؛ رکورد و شناسهٔ پایدار حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private static string NormalizeCode(string? countryCode)
    {
        var trimmed = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length < CountryCodeMinLength || trimmed.Length > CountryCodeMaxLength)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.CountryCodeInvalid));
        }

        return trimmed;
    }
}
