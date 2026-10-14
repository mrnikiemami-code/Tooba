using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Errors;

namespace Tooba.Geography.Domain.Aggregates;

/// <summary>
/// ناحیهٔ اداری (استان/ایالت/منطقه) با سلسله‌مراتب داده‌محور و خودارجاع.
/// عمق و معنی سطوح (D10) در این بنیاد تصمیم‌گیری نشده است؛ هیچ تفکیک‌کنندهٔ سطحی مدل نمی‌شود.
/// </summary>
public sealed class AdministrativeArea
{
    /// <summary>حداکثر طول کد ناحیه.</summary>
    public const int CodeMaxLength = 32;

    private AdministrativeArea()
    {
    }

    /// <summary>شناسهٔ پایدار ناحیه.</summary>
    public Guid AdministrativeAreaId { get; init; }

    /// <summary>کشور مالک.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ والد در سلسله‌مراتب؛ برای ناحیهٔ ریشه تهی است.</summary>
    public Guid? ParentAdministrativeAreaId { get; private set; }

    /// <summary>کد پایدار مستقل از زبان.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>آیا ناحیه فعال است.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ناحیهٔ اداری جدید می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور مالک.</param>
    /// <param name="code">کد پایدار مستقل از زبان.</param>
    /// <param name="parentAdministrativeAreaId">شناسهٔ ناحیهٔ والد اختیاری.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="administrativeAreaId">شناسهٔ قطعی اختیاری.</param>
    public static AdministrativeArea Create(
        Guid countryId,
        string code,
        Guid? parentAdministrativeAreaId,
        DateTimeOffset now,
        Guid? administrativeAreaId = null)
    {
        var areaId = administrativeAreaId is { } id && id != Guid.Empty ? id : UuidV7.New();
        var area = new AdministrativeArea
        {
            AdministrativeAreaId = areaId,
            CountryId = RequireCountry(countryId),
            Code = NormalizeCode(code),
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true,
        };
        area.SetParent(parentAdministrativeAreaId);
        return area;
    }

    /// <summary>جابه‌جایی در سلسله‌مراتب یا ریشه‌شدن را اعمال می‌کند.</summary>
    /// <param name="parentAdministrativeAreaId">شناسهٔ ناحیهٔ والد؛ تهی یعنی ریشه.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Reparent(Guid? parentAdministrativeAreaId, DateTimeOffset now)
    {
        SetParent(parentAdministrativeAreaId);
        UpdatedAt = now;
    }

    /// <summary>کد پایدار را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="code">کد پایدار مستقل از زبان.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void UpdateCode(string code, DateTimeOffset now)
    {
        Code = NormalizeCode(code);
        UpdatedAt = now;
    }

    /// <summary>ناحیه را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>ناحیه را غیرفعال می‌کند؛ شناسهٔ پایدار برای ارجاع تاریخی حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private void SetParent(Guid? parentAdministrativeAreaId)
    {
        if (parentAdministrativeAreaId is not { } parent || parent == Guid.Empty)
        {
            ParentAdministrativeAreaId = null;
            return;
        }

        if (parent == AdministrativeAreaId)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.AdministrativeAreaParentSelf));
        }

        ParentAdministrativeAreaId = parent;
    }

    private static Guid RequireCountry(Guid countryId)
    {
        if (countryId == Guid.Empty)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.CountryIdRequired));
        }

        return countryId;
    }

    private static string NormalizeCode(string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > CodeMaxLength)
        {
            throw new SemanticException(new SemanticError(GeographyErrorCodes.AdministrativeAreaCodeInvalid));
        }

        return trimmed;
    }
}
