using Tooba.Geography.Contracts.Places;

namespace Tooba.Shipping.Contracts.Rates;

/// <summary>
/// سطح دامنهٔ مقصد یک سیاست نرخ یا یک مجوز پوشش. سطح یک واقعیت داده‌ای است، نه یک درخت ثابت:
/// دامنهٔ ریشه «کشور» است و هر سطح پایین‌تر باید والد خود را نیز داشته باشد.
/// </summary>
public enum ShippingDestinationLevel
{
    /// <summary>دامنه در سطح کشور.</summary>
    Country = 0,

    /// <summary>دامنه در سطح ناحیهٔ اداری (استان).</summary>
    AdministrativeArea = 1,

    /// <summary>دامنه در سطح شهر.</summary>
    City = 2,

    /// <summary>دامنه در سطح ناحیهٔ خدمت.</summary>
    ServiceArea = 3,
}

/// <summary>
/// شکست یک سیاست نرخ یا مجوز پوشش روی شناسه‌های یک مقصد. مقصد و پوشش هر دو با شناسهٔ پایدار
/// بیان می‌شوند؛ هیچ برچسب محلی‌شده‌ای در این قرارداد حمل نمی‌شود.
/// </summary>
/// <remarks>
/// یک شیء دامنه‌دار باید همیشه همهٔ والدها را داشته باشد (شهر بدون ناحیهٔ اداری تنها وقتی مجاز
/// است که واقعاً بدون ناحیه باشد)، وگرنه «نیمه‌دامنه»ای ساخته می‌شود که می‌تواند بی‌سروصدا به
/// مقصد اشتباه متصل شود.
/// </remarks>
public sealed record ShippingDestinationScope(
    Guid CountryId,
    Guid? AdministrativeAreaId = null,
    Guid? CityId = null,
    Guid? ServiceAreaId = null)
{
    /// <summary>دامنهٔ سطح کشور می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    public static ShippingDestinationScope ForCountry(Guid countryId) => new(countryId);

    /// <summary>دامنهٔ سطح ناحیهٔ اداری می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری.</param>
    public static ShippingDestinationScope ForAdministrativeArea(Guid countryId, Guid administrativeAreaId)
        => new(countryId, administrativeAreaId);

    /// <summary>دامنهٔ سطح شهر می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی شهر بدون ناحیه.</param>
    /// <param name="cityId">شناسهٔ شهر.</param>
    public static ShippingDestinationScope ForCity(Guid countryId, Guid? administrativeAreaId, Guid cityId)
        => new(countryId, administrativeAreaId, cityId);

    /// <summary>دامنهٔ سطح ناحیهٔ خدمت می‌سازد.</summary>
    /// <param name="countryId">شناسهٔ کشور.</param>
    /// <param name="administrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی شهر بدون ناحیه.</param>
    /// <param name="cityId">شناسهٔ شهر مالک ناحیهٔ خدمت.</param>
    /// <param name="serviceAreaId">شناسهٔ ناحیهٔ خدمت.</param>
    public static ShippingDestinationScope ForServiceArea(
        Guid countryId,
        Guid? administrativeAreaId,
        Guid cityId,
        Guid serviceAreaId)
        => new(countryId, administrativeAreaId, cityId, serviceAreaId);

    /// <summary>سطح دامنه بر پایهٔ عمیق‌ترین شناسهٔ موجود.</summary>
    public ShippingDestinationLevel Level => ServiceAreaId is not null
        ? ShippingDestinationLevel.ServiceArea
        : CityId is not null
            ? ShippingDestinationLevel.City
            : AdministrativeAreaId is not null
                ? ShippingDestinationLevel.AdministrativeArea
                : ShippingDestinationLevel.Country;
}

/// <summary>وضعیت خدمت‌رسانی برای یک درخواست نرخ.</summary>
public enum ShippingServiceabilityOutcome
{
    /// <summary>یک سیاست فعال با پوشش معتبر پیدا شد؛ نرخ قابل اعلام است.</summary>
    Quoted = 0,

    /// <summary>هیچ مجوز پوششی برای این مقصد وجود ندارد. عدم مجوز یعنی «پوشش‌نشده»، نه «بدون محدودیت».</summary>
    NotCovered = 1,

    /// <summary>کد خدمت هیچ سیاست فعالی ندارد.</summary>
    UnknownService = 2,

    /// <summary>بیش از یک سیاست هم‌سطح و ناسازگار با دامنهٔ درخواست سازگار است؛ انتخاب خودکار انجام نمی‌شود.</summary>
    ScopeConflict = 3,

    /// <summary>سیاست نرخ و ارز آن با ارز درخواست سازگار نیست.</summary>
    CurrencyMismatch = 4,
}

/// <summary>کدهای دلیلِ پایدار برای نتیجهٔ خدمت‌رسانی؛ مستقل از زبان و مناسب برای لاگ/تلمتری.</summary>
public static class ShippingReasonCodes
{
    /// <summary>نرخ از یک سیاست فعال و پوشش‌شده محاسبه شد.</summary>
    public const string Quoted = "shipping.quote.quoted";

    /// <summary>نرخ از سیاست فعال محاسبه شد و آستانهٔ ارسال رایگان اعمال شد.</summary>
    public const string QuotedFreeAboveThreshold = "shipping.quote.free_above_threshold";

    /// <summary>برای این کد خدمت و مقصد هیچ مجوز پوششی فعالی وجود ندارد.</summary>
    public const string CoverageMissing = "shipping.coverage.missing";

    /// <summary>کد خدمت هیچ سیاست فعالی ندارد.</summary>
    public const string ServiceNotPriced = "shipping.service.not_priced";

    /// <summary>چند سیاست هم‌سطح با دامنهٔ درخواست سازگارند.</summary>
    public const string ScopeAmbiguous = "shipping.scope.ambiguous";

    /// <summary>ارز درخواست با ارز سیاست یکسان نیست.</summary>
    public const string CurrencyMismatch = "shipping.currency.mismatch";
}

/// <summary>
/// درخواست نرخ ارسال. مقصد با شناسه‌های پایدار Geography بیان می‌شود و مبلغ سبد جدا از هزینهٔ
/// ارسال است تا آستانهٔ ارسال رایگان بدون بازتفسیر قابل اعمال باشد.
/// </summary>
/// <param name="ServiceCode">کد خدمت ارسال؛ همان کدهای موجود Fulfillment، دست‌نخورده و فقط به‌عنوان کلید.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد.</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال.</param>
/// <param name="CurrencyCode">ارز درخواست؛ تهی یعنی بررسی ارز انجام نشود.</param>
public sealed record ShippingQuoteRequest(
    string ServiceCode,
    DestinationIdentifiers Destination,
    decimal CartSubtotalExclusive,
    string? CurrencyCode = null);

/// <summary>
/// نتیجهٔ قطعی و محدود خدمت‌رسانی همراه با جزئیات آمادهٔ promise مشتری. نتیجه هرگز «سرویس‌پذیر
/// به‌طور پیش‌فرض» نیست: نبود مجوز پوشش صریحاً <see cref="ShippingServiceabilityOutcome.NotCovered"/> است.
/// </summary>
/// <param name="Outcome">وضعیت خدمت‌رسانی.</param>
/// <param name="ServiceCode">کد خدمت درخواست‌شده.</param>
/// <param name="ChargeAmount">هزینهٔ ارسال؛ تنها در حالت <see cref="ShippingServiceabilityOutcome.Quoted"/> معنا دارد.</param>
/// <param name="CurrencyCode">ارز هزینه؛ تنها در حالت نرخ‌دار معنا دارد.</param>
/// <param name="PreparationDays">روزهای آماده‌سازی فروشنده؛ از روزهای حمل جدا است.</param>
/// <param name="LeadDays">روزهای حمل این خدمت.</param>
/// <param name="FreeAboveThreshold">آستانهٔ ارسال رایگان؛ در صورت وجود، برای شفافیت بازگردانده می‌شود.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
public sealed record ShippingQuote(
    ShippingServiceabilityOutcome Outcome,
    string ServiceCode,
    decimal ChargeAmount,
    string? CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold,
    string ReasonCode)
{
    /// <summary>آیا نرخ قابل اعلام به مشتری است.</summary>
    public bool IsQuoted => Outcome == ShippingServiceabilityOutcome.Quoted;
}

/// <summary>
/// تنها درز خواندنِ بین‌ماژولی برای نرخ و پوشش ارسال. فقط-خواندنی است: هیچ درز نوشتنی برای
/// ماژول‌های دیگر باز نمی‌شود و Shipping هرگز «حکم خدمت‌رسانی» را از ماژول دیگری نمی‌گیرد.
/// </summary>
/// <remarks>
/// این درز عمداً سیاست را <em>بازنمی‌گرداند</em>؛ فقط نتیجهٔ قطعی نرخ را می‌دهد تا مصرف‌کننده
/// (Order) نتواند قواعد تجاری را بازپیاده‌سازی کند.
/// </remarks>
public interface IShippingRateQuoteLookup
{
    /// <summary>نرخ و خدمت‌رسانی یک درخواست را به‌صورت قطعی و fail-closed محاسبه می‌کند.</summary>
    /// <param name="request">درخواست نرخ.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نتیجهٔ نرخ؛ هرگز null.</returns>
    Task<ShippingQuote> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken);
}
