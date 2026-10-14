using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// ناحیهٔ خدمت (زیرشهر/منطقهٔ عملیاتی) زیر یک شهر. هویت آن پایدار و مستقل از زبان است و
/// هیچ قاعدهٔ تجاری پوشش یا نرخ را در خود نگه نمی‌دارد.
/// </summary>
public sealed class ServiceArea
{
    /// <summary>حداکثر طول کد ناحیهٔ خدمت.</summary>
    public const int CodeMaxLength = 32;

    private ServiceArea()
    {
    }

    /// <summary>شناسهٔ پایدار ناحیهٔ خدمت.</summary>
    public Guid ServiceAreaId { get; init; }

    /// <summary>شهر مالک.</summary>
    public Guid CityId { get; private set; }

    /// <summary>کد پایدار مستقل از زبان.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>آیا ناحیهٔ خدمت فعال است.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ناحیهٔ خدمت جدید می‌سازد.</summary>
    /// <param name="cityId">شناسهٔ شهر مالک.</param>
    /// <param name="code">کد پایدار مستقل از زبان.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="serviceAreaId">شناسهٔ قطعی اختیاری.</param>
    public static ServiceArea Create(
        Guid cityId,
        string code,
        DateTimeOffset now,
        Guid? serviceAreaId = null)
        => new()
        {
            ServiceAreaId = serviceAreaId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            CityId = RequireCity(cityId),
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

    /// <summary>ناحیهٔ خدمت را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>ناحیهٔ خدمت را غیرفعال می‌کند؛ شناسهٔ پایدار حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private static Guid RequireCity(Guid cityId)
    {
        if (cityId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.CityIdRequired));
        }

        return cityId;
    }

    private static string NormalizeCode(string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > CodeMaxLength)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.ServiceAreaCodeInvalid));
        }

        return trimmed;
    }
}
