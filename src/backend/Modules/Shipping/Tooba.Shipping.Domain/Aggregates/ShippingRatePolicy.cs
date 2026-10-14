using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Domain.Aggregates;

/// <summary>
/// سیاست تجاری نرخ ارسال: Shipping مالک «چقدر» و «با چه پوششی» است، نه هویت خدمت.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ownership.</b> این aggregate فقط سیاست تجاری را نگه می‌دارد. ردیف‌های کاتالوگ خدمت/گزینه
/// (هویت و نمایش) دارایی Fulfillment می‌مانند و اینجا تکرار نمی‌شوند: <see cref="ServiceCode"/>
/// تنها یک <em>کلید سازگاری</em> است، دقیقاً همان کدهایی که مصرف‌کننده‌های فعلی می‌شناسند.
/// </para>
/// <para>
/// <b>دو مفهوم زمانی جدا.</b> <see cref="PreparationDays"/> (آماده‌سازی فروشنده) و
/// <see cref="LeadDays"/> (حمل) عمداً دو فیلد جدا هستند؛ جمع‌کردن آن‌ها در یک عدد همان ابهامی
/// است که promise مشتری را غیرقابل‌حسابرسی می‌کند.
/// </para>
/// </remarks>
public sealed class ShippingRatePolicy
{
    /// <summary>حداکثر طول کد خدمت.</summary>
    public const int ServiceCodeMaxLength = 64;

    /// <summary>حداکثر مبلغ مجاز هزینه (برای جلوگیری از خطای داده‌ای).</summary>
    public const decimal ChargeAmountMaxValue = 1_000_000_000m;

    /// <summary>حداکثر تعداد روز آماده‌سازی/حمل.</summary>
    public const int DaysMaxValue = 365;

    private ShippingRatePolicy()
    {
    }

    /// <summary>شناسهٔ پایدار سیاست نرخ.</summary>
    public Guid RatePolicyId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری؛ بدون تکرار ردیف کاتالوگ).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>کشور مالک دامنه.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ اداری دامنه؛ تهی یعنی دامنه در سطح کشور.</summary>
    public Guid? AdministrativeAreaId { get; private set; }

    /// <summary>شهر دامنه؛ تهی یعنی دامنه در سطح ناحیه/کشور.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>ناحیهٔ خدمت دامنه؛ تهی یعنی دامنه در سطح شهر یا بالاتر.</summary>
    public Guid? ServiceAreaId { get; private set; }

    /// <summary>مبلغ ثابت هزینهٔ ارسال؛ هرگز منفی نیست.</summary>
    public decimal ChargeAmount { get; private set; }

    /// <summary>کد ارز سه‌حرفی.</summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>روزهای آماده‌سازی فروشنده.</summary>
    public int PreparationDays { get; private set; }

    /// <summary>روزهای حمل این خدمت.</summary>
    public int LeadDays { get; private set; }

    /// <summary>آستانهٔ ارسال رایگان؛ تهی یعنی بدون آستانه.</summary>
    public decimal? FreeAboveThreshold { get; private set; }

    /// <summary>آیا سیاست فعال است؛ رکورد هرگز فیزیکی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>دامنهٔ مقصد این سیاست به‌صورت یک مقدار واحد.</summary>
    public ShippingDestinationScope Scope =>
        new(CountryId, AdministrativeAreaId, CityId, ServiceAreaId);

    /// <summary>سیاست نرخ جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="scope">دامنهٔ مقصد سیاست.</param>
    /// <param name="chargeAmount">مبلغ هزینه؛ نباید منفی باشد.</param>
    /// <param name="currencyCode">کد ارز سه‌حرفی.</param>
    /// <param name="preparationDays">روزهای آماده‌سازی.</param>
    /// <param name="leadDays">روزهای حمل.</param>
    /// <param name="freeAboveThreshold">آستانهٔ ارسال رایگان اختیاری.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="ratePolicyId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static ShippingRatePolicy Create(
        string serviceCode,
        ShippingDestinationScope scope,
        decimal chargeAmount,
        string currencyCode,
        int preparationDays,
        int leadDays,
        decimal? freeAboveThreshold,
        DateTimeOffset now,
        Guid? ratePolicyId = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var policy = new ShippingRatePolicy
        {
            RatePolicyId = ratePolicyId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            CountryId = RequireCountry(scope.CountryId),
            AdministrativeAreaId = Normalize(scope.AdministrativeAreaId),
            CityId = Normalize(scope.CityId),
            ServiceAreaId = Normalize(scope.ServiceAreaId),
            ChargeAmount = NormalizeCharge(chargeAmount),
            CurrencyCode = NormalizeCurrency(currencyCode),
            PreparationDays = NormalizeDays(preparationDays, ShippingErrorCodes.PreparationDaysNegative),
            LeadDays = NormalizeDays(leadDays, ShippingErrorCodes.LeadDaysNegative),
            FreeAboveThreshold = NormalizeThreshold(freeAboveThreshold),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        policy.EnsureScopeIsCoherent();
        return policy;
    }

    /// <summary>مبلغ هزینه را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="chargeAmount">مبلغ جدید.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeCharge(decimal chargeAmount, DateTimeOffset now)
    {
        ChargeAmount = NormalizeCharge(chargeAmount);
        UpdatedAt = now;
    }

    /// <summary>آستانهٔ ارسال رایگان را تعیین یا حذف می‌کند.</summary>
    /// <param name="freeAboveThreshold">آستانهٔ جدید؛ تهی یعنی بدون آستانه.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void SetFreeAboveThreshold(decimal? freeAboveThreshold, DateTimeOffset now)
    {
        FreeAboveThreshold = NormalizeThreshold(freeAboveThreshold);
        UpdatedAt = now;
    }

    /// <summary>روزهای آماده‌سازی و حمل را جداگانه به‌روز می‌کند.</summary>
    /// <param name="preparationDays">روزهای آماده‌سازی.</param>
    /// <param name="leadDays">روزهای حمل.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeLeadTimes(int preparationDays, int leadDays, DateTimeOffset now)
    {
        PreparationDays = NormalizeDays(preparationDays, ShippingErrorCodes.PreparationDaysNegative);
        LeadDays = NormalizeDays(leadDays, ShippingErrorCodes.LeadDaysNegative);
        UpdatedAt = now;
    }

    /// <summary>سیاست را فعال می‌کند.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    /// <summary>سیاست را غیرفعال می‌کند؛ شناسهٔ پایدار برای ارجاع تاریخی حفظ می‌شود.</summary>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا این سیاست دامنهٔ مقصد داده‌شده را پوشش می‌دهد. هر سطحی که در سیاست مقدار دارد باید با
    /// مقصد برابر باشد؛ مقصد می‌تواند عمیق‌تر از سیاست باشد (سیاست یک زیردرخت را پوشش می‌دهد).
    /// </summary>
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
        if (ServiceAreaId is not null && CityId is null)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }

        if (CityId is null && AdministrativeAreaId is not null && ServiceAreaId is not null)
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

        if (trimmed.Length > ServiceCodeMaxLength)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid));
        }

        return trimmed;
    }

    private static Guid RequireCountry(Guid countryId)
        => countryId == Guid.Empty
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.DestinationCountryIdRequired))
            : countryId;

    private static Guid? Normalize(Guid? id) => id is { } value && value != Guid.Empty ? value : null;

    private static decimal NormalizeCharge(decimal chargeAmount)
    {
        if (chargeAmount < 0m)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ChargeAmountNegative));
        }

        return chargeAmount > ChargeAmountMaxValue
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ChargeAmountOutOfRange))
            : chargeAmount;
    }

    private static decimal? NormalizeThreshold(decimal? freeAboveThreshold)
    {
        if (freeAboveThreshold is not { } threshold)
        {
            return null;
        }

        return threshold < 0m
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.FreeAboveThresholdNegative))
            : threshold;
    }

    private static int NormalizeDays(int days, string errorCode)
        => days is < 0 or > DaysMaxValue
            ? throw new SemanticException(new SemanticError(errorCode))
            : days;

    private static string NormalizeCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length != 3 || !trimmed.All(char.IsAsciiLetter))
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.CurrencyCodeInvalid));
        }

        return trimmed;
    }
}
