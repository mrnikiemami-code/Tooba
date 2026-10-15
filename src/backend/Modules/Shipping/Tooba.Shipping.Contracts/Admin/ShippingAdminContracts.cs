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
public sealed record ShippingRatePolicyWriteRequest(
    string ServiceCode,
    ShippingDestinationScopeWrite Scope,
    decimal ChargeAmount,
    string CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold = null,
    string? OptionCode = null);

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
    string? OptionCode = null);

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
