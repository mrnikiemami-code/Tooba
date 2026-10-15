using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Admin;

/// <summary>
/// شکست مکتوب دامنهٔ مقصد برای نویسندگی ادمین. همان دامنهٔ نرخ/پوشش است، اما با شناسه‌های
/// صریح به‌جای یک شیء مقدار، تا کلاینت بتواند هر سطح را جدا بخواند و بنویسد.
/// </summary>
/// <param name="CountryId">شناسهٔ کشور؛ الزامی.</param>
/// <param name="AdministrativeAreaId">شناسهٔ ناحیهٔ اداری؛ تهی یعنی سطح کشور.</param>
/// <param name="CityId">شناسهٔ شهر؛ تهی یعنی سطح بالاتر.</param>
/// <param name="ServiceAreaId">شناسهٔ ناحیهٔ خدمت؛ تهی یعنی سطح بالاتر.</param>
public sealed record ShippingDestinationScopeWrite(
    Guid CountryId,
    Guid? AdministrativeAreaId = null,
    Guid? CityId = null,
    Guid? ServiceAreaId = null)
{
    /// <summary>دامنهٔ مقصد را به شیء مقدار قرارداد تبدیل می‌کند.</summary>
    public ShippingDestinationScope ToScope() => new(CountryId, AdministrativeAreaId, CityId, ServiceAreaId);
}

/// <summary>
/// بازهٔ نیم‌باز <c>[Min, Max)</c> در قرارداد ادمین برای یک پلهٔ اندازه. کران بالا تهی یعنی باز.
/// </summary>
/// <param name="Min">کران پایین شامل.</param>
/// <param name="Max">کران بالا غیرشامل؛ تهی یعنی بی‌کران بالا.</param>
public sealed record ShippingNumericTierWrite(long Min, long? Max = null)
{
    /// <summary>بازهٔ قرارداد را به مقدار دامنه تبدیل می‌کند.</summary>
    public ShippingNumericTier ToTier() => ShippingNumericTier.Between(Min, Max);
}

/// <summary>
/// شرط اندازهٔ یک سیاست نرخ در قرارداد ادمین: بازهٔ وزن (گرم) و بازهٔ تعداد اقلام، هر دو اختیاری.
/// تهی یعنی «روی این محور بی‌شرط».
/// </summary>
/// <param name="WeightGrams">بازهٔ وزن گرم؛ تهی یعنی بی‌شرط روی وزن.</param>
/// <param name="ItemCount">بازهٔ تعداد اقلام؛ تهی یعنی بی‌شرط روی تعداد.</param>
public sealed record ShippingRateConditionWrite(
    ShippingNumericTierWrite? WeightGrams = null,
    ShippingNumericTierWrite? ItemCount = null)
{
    /// <summary>شرط قرارداد را به مقدار دامنه تبدیل می‌کند؛ تهی یعنی بی‌شرط.</summary>
    public ShippingRateCondition? ToCondition()
        => WeightGrams is null && ItemCount is null
            ? null
            : new ShippingRateCondition(WeightGrams?.ToTier(), ItemCount?.ToTier());
}

/// <summary>بدنهٔ ایجاد/ویرایش یک سیاست نرخ ارسال برای یک کد خدمت.</summary>
/// <param name="ServiceCode">کد خدمت Fulfillment؛ فقط کلید سازگاری، بدون تکرار ردیف کاتالوگ.</param>
/// <param name="Scope">دامنهٔ مقصد سیاست.</param>
/// <param name="ChargeAmount">مبلغ ثابت هزینه؛ نباید منفی باشد.</param>
/// <param name="CurrencyCode">کد ارز سه‌حرفی.</param>
/// <param name="PreparationDays">روزهای آماده‌سازی فروشنده.</param>
/// <param name="LeadDays">روزهای حمل این خدمت.</param>
/// <param name="FreeAboveThreshold">آستانهٔ ارسال رایگان اختیاری.</param>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی سیاست در سطح خود خدمت است.
/// این مقدار <em>هویت گزینه</em> است، نه کد خدمت: «tipax:express» خدمت tipax با گزینهٔ express است.
/// </param>
/// <param name="Condition">
/// شرط اندازهٔ این سیاست (وزن/تعداد). تهی یعنی قاعدهٔ بی‌شرط، دقیقاً مانند ردیف‌های قدیمی که
/// رفتارشان تغییر نمی‌کند.
/// </param>
public sealed record ShippingRatePolicyWriteRequest(
    string ServiceCode,
    ShippingDestinationScopeWrite Scope,
    decimal ChargeAmount,
    string CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold = null,
    string? OptionCode = null,
    ShippingRateConditionWrite? Condition = null);

/// <summary>بدنهٔ تغییر وضعیت فعال/غیرفعال یک سیاست نرخ.</summary>
/// <param name="IsActive">وضعیت هدف.</param>
public sealed record ShippingRatePolicyActiveRequest(bool IsActive);

/// <summary>بدنهٔ ایجاد یک مجوز پوشش صریح برای یک کد خدمت.</summary>
/// <param name="ServiceCode">کد خدمت Fulfillment.</param>
/// <param name="Scope">دامنهٔ مقصد مجوز.</param>
public sealed record ShippingCoverageGrantWriteRequest(
    string ServiceCode,
    ShippingDestinationScopeWrite Scope);

/// <summary>بدنهٔ تغییر وضعیت فعال/غیرفعال یک مجوز پوشش.</summary>
/// <param name="IsActive">وضعیت هدف.</param>
public sealed record ShippingCoverageGrantActiveRequest(bool IsActive);

/// <summary>یک سیاست نرخ همان‌طور که ادمین می‌بیند.</summary>
/// <param name="RatePolicyId">شناسهٔ پایدار سیاست.</param>
/// <param name="ServiceCode">کد خدمت Fulfillment.</param>
/// <param name="CountryId">شناسهٔ کشور دامنه.</param>
/// <param name="AdministrativeAreaId">ناحیهٔ اداری دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="CityId">شهر دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="ServiceAreaId">ناحیهٔ خدمت دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="Level">عمیق‌ترین سطح دامنه.</param>
/// <param name="ChargeAmount">مبلغ ثابت هزینه.</param>
/// <param name="CurrencyCode">کد ارز سه‌حرفی.</param>
/// <param name="PreparationDays">روزهای آماده‌سازی فروشنده.</param>
/// <param name="LeadDays">روزهای حمل.</param>
/// <param name="FreeAboveThreshold">آستانهٔ ارسال رایگان؛ تهی یعنی بدون آستانه.</param>
/// <param name="IsActive">آیا سیاست فعال است.</param>
/// <param name="CreatedAt">زمان ایجاد UTC.</param>
/// <param name="UpdatedAt">زمان آخرین ویرایش UTC.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی سیاست سطح‌خدمت.</param>
/// <param name="Condition">
/// شرط اندازهٔ این سیاست؛ تهی یعنی بی‌شرط. یک سیاست شرط‌دار فقط داخل بازهٔ نیم‌باز خودش قیمت
/// می‌دهد و اندازهٔ اعلام‌نشده هرگز با آن تطبیق نمی‌یابد.
/// </param>
public sealed record ShippingRatePolicyAdminItem(
    Guid RatePolicyId,
    string ServiceCode,
    Guid CountryId,
    Guid? AdministrativeAreaId,
    Guid? CityId,
    Guid? ServiceAreaId,
    ShippingDestinationLevel Level,
    decimal ChargeAmount,
    string CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? OptionCode = null,
    ShippingRateConditionWrite? Condition = null);

/// <summary>یک مجوز پوشش همان‌طور که ادمین می‌بیند.</summary>
public sealed record ShippingCoverageGrantAdminItem(
    Guid CoverageGrantId,
    string ServiceCode,
    Guid CountryId,
    Guid? AdministrativeAreaId,
    Guid? CityId,
    Guid? ServiceAreaId,
    ShippingDestinationLevel Level,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// نمای کامل نویسندگی یک کد خدمت. مجموعه‌های خالی عمداً صریح‌اند: نبود مجوز یعنی «بدون پوشش»،
/// نه «بدون محدودیت».
/// </summary>
/// <param name="ServiceCode">کد خدمت این نما.</param>
/// <param name="RatePolicies">سیاست‌های نرخ این خدمت.</param>
/// <param name="CoverageGrants">مجوزهای پوشش این خدمت.</param>
public sealed record ShippingRatesAndCoverageView(
    string ServiceCode,
    IReadOnlyList<ShippingRatePolicyAdminItem> RatePolicies,
    IReadOnlyList<ShippingCoverageGrantAdminItem> CoverageGrants);
