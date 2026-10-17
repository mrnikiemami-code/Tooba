using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Domain.Aggregates;

/// <summary>
/// مجوز پوشش ارسال: ردیف صریحی که می‌گوید یک کد خدمت در یک دامنهٔ مقصد مجاز است.
/// </summary>
/// <remarks>
/// <para>
/// <b>Fail-closed by construction.</b> نبود ردیف یعنی «پوشش‌نشده». این عمداً با معنای تاریخی
/// <c>AllowedProvinces</c> خالی در مسیر فروشگاهی فعلی (که «بدون محدودیت» تفسیر می‌شود) یکی نیست:
/// آن معنا در همان مسیر دست‌نخورده باقی می‌ماند و تبدیل آن به پوشش صریح، کار یک Task جداگانه
/// است. هیچ‌جا در این ماژول از «فهرست خالی = مجاز» استفاده نمی‌شود.
/// </para>
/// <para>
/// دامنهٔ والد عمداً مجاز است: یک مجوز سطح کشور یک زیردرخت را پوشش می‌دهد، اما خودِ وجود این ردیف
/// یک تصمیم صریح و قابل بازبینی است، نه یک پیش‌فرض.
/// </para>
/// </remarks>
public sealed class ShippingCoverageGrant
{
    private ShippingCoverageGrant()
    {
    }

    /// <summary>شناسهٔ پایدار مجوز پوشش.</summary>
    public Guid CoverageGrantId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>
    /// گزینهٔ روش ارسال؛ تهی یعنی این مجوز در سطح خدمت است.
    /// </summary>
    /// <remarks>
    /// ردیف‌های موجود همچنان تهی می‌مانند و معنای قدیمی («پوشش سطح خدمت») را نگه می‌دارند. یک مجوز
    /// گزینه‌دار به روش دقیقِ خودش تعلق دارد و نه خدمت را پوشش می‌دهد و نه گزینه‌های هم‌خانواده را.
    /// </remarks>
    public string? OptionCode { get; private set; }

    /// <summary>کشور مالک دامنه.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ اداری دامنه؛ تهی یعنی دامنه در سطح کشور.</summary>
    public Guid? AdministrativeAreaId { get; private set; }

    /// <summary>شهر دامنه؛ تهی یعنی دامنه در سطح ناحیه/کشور.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>ناحیهٔ خدمت دامنه؛ تهی یعنی دامنه در سطح شهر یا بالاتر.</summary>
    public Guid? ServiceAreaId { get; private set; }

    /// <summary>آیا مجوز فعال است؛ رکورد هرگز فیزیکی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>دامنهٔ مقصد این مجوز به‌صورت یک مقدار واحد.</summary>
    public ShippingDestinationScope Scope =>
        new(CountryId, AdministrativeAreaId, CityId, ServiceAreaId);

    /// <summary>مجوز پوشش جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="scope">دامنهٔ مقصد مجوز.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="coverageGrantId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    /// <param name="optionCode">
    /// گزینهٔ روش ارسال؛ تهی یعنی مجوز در سطح خدمت. مقدار خالی/فاصله هم به تهی (خدمت‌سطح) نرمال می‌شود
    /// تا «گزینهٔ nameless» هرگز به‌عنوان یک هویت جدا ثبت نشود.
    /// </param>
    public static ShippingCoverageGrant Create(
        string serviceCode,
        ShippingDestinationScope scope,
        DateTimeOffset now,
        Guid? coverageGrantId = null,
        string? optionCode = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var grant = new ShippingCoverageGrant
        {
            CoverageGrantId = coverageGrantId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            CountryId = scope.CountryId == Guid.Empty
                ? throw new SemanticException(new SemanticError(ShippingErrorCodes.DestinationCountryIdRequired))
                : scope.CountryId,
            AdministrativeAreaId = Normalize(scope.AdministrativeAreaId),
            CityId = Normalize(scope.CityId),
            ServiceAreaId = Normalize(scope.ServiceAreaId),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        grant.EnsureScopeIsCoherent();
        return grant;
    }

    /// <summary>
    /// آیا این مجوز دقیقاً به روش داده‌شده تعلق دارد (کد خدمت و همان گزینه، بدون fallback).
    /// </summary>
    /// <param name="serviceCode">کد خدمت درخواست.</param>
    /// <param name="optionCode">گزینهٔ درخواست؛ تهی یعنی روش سطح خدمت.</param>
    public bool BelongsToMethod(string serviceCode, string? optionCode) =>
        string.Equals(ServiceCode, serviceCode?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            OptionCode ?? string.Empty,
            NormalizeOptionCode(optionCode) ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>مجوز را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>مجوز را غیرفعال می‌کند؛ شناسهٔ پایدار برای ارجاع تاریخی حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>آیا این مجوز دامنهٔ مقصد داده‌شده را پوشش می‌دهد.</summary>
    /// <param name="destination">دامنهٔ مقصد درخواست.</param>
    public bool Covers(ShippingDestinationScope destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (CountryId != destination.CountryId)
        {
            return false;
        }

        if (AdministrativeAreaId is { } area && area != destination.AdministrativeAreaId)
        {
            return false;
        }

        if (CityId is { } city && city != destination.CityId)
        {
            return false;
        }

        return ServiceAreaId is not { } serviceArea || serviceArea == destination.ServiceAreaId;
    }

    private void EnsureScopeIsCoherent()
    {
        // A service area without its owning city (or a city without its country) is a half-scope that
        // could silently match the wrong destination, so it is rejected instead of stored.
        if (ServiceAreaId is not null && CityId is null)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }
    }

    private static string NormalizeServiceCode(string? serviceCode)
    {
        var trimmed = serviceCode?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        return trimmed.Length > ShippingRatePolicy.ServiceCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid))
            : trimmed;
    }

    /// <summary>
    /// گزینه را به شکل متعارف می‌برد؛ تهی/فاصله یعنی مجوز در سطح خدمت است و یک مقدار غیرمجاز طولی
    /// هرگز ثبت نمی‌شود (قیاس با کد خدمت).
    /// </summary>
    private static string? NormalizeOptionCode(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > ShippingRatePolicy.OptionCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.OptionCodeInvalid))
            : trimmed;
    }

    private static Guid? Normalize(Guid? id) => id is { } value && value != Guid.Empty ? value : null;
}
