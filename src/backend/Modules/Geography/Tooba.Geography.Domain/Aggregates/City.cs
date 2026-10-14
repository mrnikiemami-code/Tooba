using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// شهر با هویت پایدار. دو شهر هم‌نام در ناحیه‌های متفاوت دو هویت جدا هستند؛ برچسب محلی
/// هرگز جانشین شناسه نمی‌شود.
/// </summary>
public sealed class City
{
    /// <summary>حداکثر طول کد شهر.</summary>
    public const int CodeMaxLength = 32;

    private City()
    {
    }

    /// <summary>شناسهٔ پایدار شهر.</summary>
    public Guid CityId { get; init; }

    /// <summary>کشور مالک.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ اداری اختیاری مالک.</summary>
    public Guid? AdministrativeAreaId { get; private set; }

    /// <summary>کد پایدار مستقل از زبان.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>آیا شهر فعال است.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>شهر جدید می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور مالک.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری اختیاری.</param>
    /// <param name="code">کد پایدار مستقل از زبان.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="cityId">شناسهٔ قطعی اختیاری.</param>
    public static City Create(
        Guid countryId,
        Guid? administrativeAreaId,
        string code,
        DateTimeOffset now,
        Guid? cityId = null)
        => new()
        {
            CityId = cityId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            CountryId = RequireCountry(countryId),
            AdministrativeAreaId = NormalizeParent(administrativeAreaId),
            Code = NormalizeCode(code),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>کد پایدار را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="code">کد پایدار مستقل از زبان.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void UpdateCode(string code, DateTimeOffset now)
    {
        Code = NormalizeCode(code);
        UpdatedAt = now;
    }

    /// <summary>ناحیهٔ اداری مالک را تغییر می‌دهد.</summary>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی بدون ناحیه.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void MoveToAdministrativeArea(Guid? administrativeAreaId, DateTimeOffset now)
    {
        AdministrativeAreaId = NormalizeParent(administrativeAreaId);
        UpdatedAt = now;
    }

    /// <summary>شهر را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>شهر را غیرفعال می‌کند؛ ارجاع تاریخی به شناسه معتبر می‌ماند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private static Guid RequireCountry(Guid countryId)
    {
        if (countryId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.CountryIdRequired));
        }

        return countryId;
    }

    private static Guid? NormalizeParent(Guid? parentId)
        => parentId is { } id && id != Guid.Empty ? id : null;

    private static string NormalizeCode(string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > CodeMaxLength)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.CityCodeInvalid));
        }

        return trimmed;
    }
}
