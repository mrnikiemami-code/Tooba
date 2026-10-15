using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Domain.Aggregates;

/// <summary>
/// سیاست تجاری <em>هزینهٔ بسته‌بندی</em>: Shipping مالک «چقدر بسته‌بندی» است، جدا از «چقدر حمل».
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا جدا از <see cref="ShippingRatePolicy"/>.</b> حمل و بسته‌بندی دو تصمیم تجاری متفاوت‌اند:
/// حمل یک خدمت با آستانهٔ ارسال رایگان و پلهٔ وزن است، بسته‌بندی یک هزینهٔ ثابت عملیاتی بدون پله
/// و بدون آستانه. جمع‌کردنشان در یک عدد باعث می‌شد «ارسال رایگان» بی‌سروصدا بسته‌بندی را هم صفر
/// کند یا برعکس، و هیچ‌کدام قابل حسابرسی نباشد.
/// </para>
/// <para>
/// <b>پیش‌فرض صفر.</b> نبود ردیف یعنی «هزینهٔ بسته‌بندی صفر»، نه یک هزینهٔ پنهان. این aggregate
/// هیچ مقداری را به مشتری تحمیل نمی‌کند؛ فقط یک واقعیت نرخ را نگه می‌دارد.
/// </para>
/// <para>
/// <b>طرف پرداخت، نه تراکنش.</b> <see cref="FreightPayerMode"/> می‌گوید حمل پیش‌پرداخت است یا
/// هنگام تحویل وصول می‌شود. این یک واقعیت نرخ است و هیچ تراکنش پرداختی در این ماژول ساخته نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingPackagingPolicy
{
    private ShippingPackagingPolicy()
    {
    }

    /// <summary>حداکثر مبلغ مجاز هزینهٔ بسته‌بندی (هم‌مرز با سقف نرخ، برای جلوگیری از خطای داده‌ای).</summary>
    public const decimal FeeAmountMaxValue = 1_000_000_000m;

    /// <summary>شناسهٔ پایدار سیاست بسته‌بندی.</summary>
    public Guid PackagingPolicyId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری؛ بدون تکرار ردیف کاتالوگ).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>گزینهٔ روش ارسال؛ تهی یعنی سیاست در سطح خود خدمت است.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>کشور مالک دامنه.</summary>
    public Guid CountryId { get; private set; }

    /// <summary>ناحیهٔ اداری دامنه؛ تهی یعنی دامنه در سطح کشور.</summary>
    public Guid? AdministrativeAreaId { get; private set; }

    /// <summary>شهر دامنه؛ تهی یعنی دامنه در سطح ناحیه/کشور.</summary>
    public Guid? CityId { get; private set; }

    /// <summary>ناحیهٔ خدمت دامنه؛ تهی یعنی دامنه در سطح شهر یا بالاتر.</summary>
    public Guid? ServiceAreaId { get; private set; }

    /// <summary>هزینهٔ ثابت بسته‌بندی؛ هرگز منفی نیست.</summary>
    public decimal FeeAmount { get; private set; }

    /// <summary>کد ارز سه‌حرفی.</summary>
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>طرف پرداخت‌کنندهٔ حمل؛ پیش‌فرض <see cref="ShippingFreightPayerMode.Prepaid"/>.</summary>
    public ShippingFreightPayerMode FreightPayerMode { get; private set; }

    /// <summary>آیا سیاست فعال است؛ رکورد هرگز فیزیکی حذف نمی‌شود.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>دامنهٔ مقصد این سیاست به‌صورت یک مقدار واحد.</summary>
    public ShippingDestinationScope Scope =>
        new(CountryId, AdministrativeAreaId, CityId, ServiceAreaId);

    /// <summary>آیا این سیاست یک گزینهٔ مشخص را قیمت می‌دهد (در مقابل قاعدهٔ سطح‌خدمت).</summary>
    public bool IsOptionSpecific => OptionCode is not null;

    /// <summary>
    /// آیا این سیاست برای گزینهٔ درخواست‌شده معتبر است. سیاست سطح‌خدمت برای هر درخواست معتبر است؛
    /// سیاست گزینه‌دار فقط برای گزینهٔ خودش.
    /// </summary>
    /// <param name="optionCode">گزینهٔ درخواست؛ تهی یعنی درخواست فقط در سطح خدمت است.</param>
    public bool MatchesOption(string? optionCode)
        => OptionCode is null
           || string.Equals(OptionCode, optionCode?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>سیاست هزینهٔ بسته‌بندی جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="scope">دامنهٔ مقصد سیاست.</param>
    /// <param name="feeAmount">هزینهٔ ثابت؛ نباید منفی باشد.</param>
    /// <param name="currencyCode">کد ارز سه‌حرفی.</param>
    /// <param name="freightPayerMode">طرف پرداخت‌کنندهٔ حمل؛ تهی یعنی پیش‌فرض Prepaid.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="packagingPolicyId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    /// <param name="optionCode">گزینهٔ روش ارسال اختیاری؛ تهی یعنی سیاست در سطح خدمت است.</param>
    public static ShippingPackagingPolicy Create(
        string serviceCode,
        ShippingDestinationScope scope,
        decimal feeAmount,
        string currencyCode,
        ShippingFreightPayerMode? freightPayerMode,
        DateTimeOffset now,
        Guid? packagingPolicyId = null,
        string? optionCode = null)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var policy = new ShippingPackagingPolicy
        {
            PackagingPolicyId = packagingPolicyId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            CountryId = RequireCountry(scope.CountryId),
            AdministrativeAreaId = Normalize(scope.AdministrativeAreaId),
            CityId = Normalize(scope.CityId),
            ServiceAreaId = Normalize(scope.ServiceAreaId),
            FeeAmount = NormalizeFee(feeAmount),
            CurrencyCode = NormalizeCurrency(currencyCode),
            FreightPayerMode = NormalizePayerMode(freightPayerMode),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        policy.EnsureScopeIsCoherent();
        return policy;
    }

    /// <summary>هزینهٔ بسته‌بندی را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="feeAmount">مبلغ جدید.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeFee(decimal feeAmount, DateTimeOffset now)
    {
        FeeAmount = NormalizeFee(feeAmount);
        UpdatedAt = now;
    }

    /// <summary>ارز هزینه را با همان قواعد اعتبارسنجی به‌روز می‌کند.</summary>
    /// <param name="currencyCode">کد ارز جدید.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeCurrency(string currencyCode, DateTimeOffset now)
    {
        CurrencyCode = NormalizeCurrency(currencyCode);
        UpdatedAt = now;
    }

    /// <summary>طرف پرداخت‌کنندهٔ حمل را تعیین می‌کند.</summary>
    /// <param name="freightPayerMode">حالت جدید؛ تهی یعنی بازگشت به پیش‌فرض Prepaid.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeFreightPayerMode(ShippingFreightPayerMode? freightPayerMode, DateTimeOffset now)
    {
        FreightPayerMode = NormalizePayerMode(freightPayerMode);
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
    /// مقصد برابر باشد؛ مقصد می‌تواند عمیق‌تر از سیاست باشد.
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
        // A service area whose owning city is absent is a half-scope that could match the wrong
        // destination; it is refused instead of stored, exactly like the rate policy.
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

        return trimmed.Length > ShippingRatePolicy.ServiceCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid))
            : trimmed;
    }

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

    private static Guid RequireCountry(Guid countryId)
        => countryId == Guid.Empty
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.DestinationCountryIdRequired))
            : countryId;

    private static Guid? Normalize(Guid? id) => id is { } value && value != Guid.Empty ? value : null;

    private static decimal NormalizeFee(decimal feeAmount)
    {
        if (feeAmount < 0m)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.PackagingFeeNegative));
        }

        return feeAmount > FeeAmountMaxValue
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.PackagingFeeOutOfRange))
            : feeAmount;
    }

    private static string NormalizeCurrency(string? currencyCode)
    {
        var trimmed = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length != 3 || !trimmed.All(char.IsAsciiLetter))
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.PackagingCurrencyInvalid));
        }

        return trimmed;
    }

    /// <summary>
    /// طرف پرداخت‌کنندهٔ حمل را به شکل متعارف می‌برد. مقدار تهی به پیش‌فرض مستند
    /// <see cref="ShippingFreightPayerMode.Prepaid"/> نگاشت می‌شود تا رفتار قدیمی checkout
    /// دست‌نخورده بماند؛ هر مقدار خارج از قرارداد صریحاً رد می‌شود.
    /// </summary>
    private static ShippingFreightPayerMode NormalizePayerMode(ShippingFreightPayerMode? mode)
        => mode is null
            ? ShippingFreightPayerMode.Prepaid
            : mode is ShippingFreightPayerMode.Prepaid or ShippingFreightPayerMode.CollectOnDelivery
                ? mode.Value
                : throw new SemanticException(new SemanticError(ShippingErrorCodes.PackagingFreightPayerModeInvalid));
}
