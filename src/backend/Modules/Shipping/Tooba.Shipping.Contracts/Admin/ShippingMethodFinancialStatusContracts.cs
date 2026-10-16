using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Admin;

/// <summary>
/// وضعیت مالی اثبات‌شدهٔ یک روش ارسال canonical، همان‌طور که مالک داده (Shipping) می‌تواند آن را
/// از ردیف‌های واقعی نرخ/پوشش استنتاج کند. این شمارش‌گر عمداً فقط حالت‌هایی را دارد که اسکیمای
/// فعلی می‌تواند اثبات کند؛ «رایگان»، «پرداخت در محل» و «استعلام ارائه‌دهنده» هیچ ستون/قاعدهٔ
/// پشتیبان ندارند و بنابراین حالت قابل‌اثبات محسوب نمی‌شوند.
/// </summary>
public enum ShippingMethodFinancialState
{
    /// <summary>هیچ قاعدهٔ نرخ فعالِ متعلق به همین روش وجود ندارد؛ روش «بدون تنظیم مالی» است.</summary>
    NeedsConfiguration = 0,

    /// <summary>حداقل یک قاعدهٔ نرخ فعالِ متعلق به همین روش با مبلغ و ارز مشخص وجود دارد.</summary>
    FinanciallyConfigured = 1,

    /// <summary>
    /// ردیف نرخ فعال فقط در سطح سرویس (<c>optionCode</c> تهی) وجود دارد و این روش دارای گزینهٔ
    /// اختصاصی است. این ردیف مالکیت روش را اثبات نمی‌کند؛ نه مبلغی به این روش نسبت داده می‌شود و
    /// نه «رایگان» فرض می‌شود.
    /// </summary>
    LegacySharedUnresolved = 2,

    /// <summary>
    /// داده‌های فعالِ همین روش یک مبلغ/دامنهٔ یگانه را اثبات نمی‌کنند (ارز متناقض یا دو قاعدهٔ
    /// هم‌دامنه/هم‌شرط). انتخاب خودکار یک تصمیم تجاری پنهان است و اینجا انجام نمی‌شود.
    /// </summary>
    Conflicting = 3,
}

/// <summary>
/// حالت پولی قابل‌اثبات از اسکیمای فعلی. تنها حالت اثبات‌پذیر «مبلغ ثابت اعلام‌شده» است؛ بقیهٔ
/// واژگان قفل‌شده (رایگان/قاعده‌محور/پرداخت در محل/استعلام) فیلد یا موتور پشتیبان ندارند و در
/// این read model ساخته نمی‌شوند.
/// </summary>
public enum ShippingMethodFinancialMode
{
    /// <summary>حالت پولی اثبات‌شده‌ای وجود ندارد.</summary>
    None = 0,

    /// <summary>مبلغ ثابت اعلام‌شده در ردیف نرخ؛ ممکن است به دامنهٔ مقصد یا پلهٔ اندازه محدود باشد.</summary>
    FixedAmount = 1,
}

/// <summary>یک قاعدهٔ نرخ فعالِ متعلق به همین روش، فقط برای شفافیت و حسابرسی ادمین.</summary>
/// <param name="RatePolicyId">شناسهٔ پایدار ردیف نرخ.</param>
/// <param name="ScopeLevel">عمیق‌ترین سطح دامنهٔ مقصد این قاعده.</param>
/// <param name="ChargeAmount">مبلغ ثابت اعلام‌شدهٔ همین قاعده.</param>
/// <param name="CurrencyCode">ارز اعلام‌شدهٔ همین قاعده.</param>
/// <param name="PreparationDays">روزهای آماده‌سازی فروشنده.</param>
/// <param name="LeadDays">روزهای حمل.</param>
/// <param name="FreeAboveThreshold">آستانهٔ ارسال رایگان؛ تهی یعنی بدون آستانه.</param>
/// <param name="IsConditioned">آیا این قاعده به پلهٔ وزن/تعداد محدود است.</param>
public sealed record ShippingMethodFinancialRule(
    Guid RatePolicyId,
    ShippingDestinationLevel ScopeLevel,
    decimal ChargeAmount,
    string CurrencyCode,
    int PreparationDays,
    int LeadDays,
    decimal? FreeAboveThreshold,
    bool IsConditioned);

/// <summary>
/// وضعیت مالی یک روش canonical با هویت کامل <c>(serviceCode, optionCode)</c>. این نما هرگز مبلغ
/// سطح سرویس را به روش گزینه‌دار نسبت نمی‌دهد و نبود داده را «صفر/رایگان» نمی‌خواند.
/// </summary>
/// <param name="ServiceCode">کد سرویس authoritative.</param>
/// <param name="OptionCode">گزینهٔ authoritative؛ تهی یعنی روش در سطح سرویس است.</param>
/// <param name="State">وضعیت مالی اثبات‌شده.</param>
/// <param name="Mode">حالت پولی اثبات‌شده؛ فقط <c>FixedAmount</c> از اسکیمای فعلی قابل اثبات است.</param>
/// <param name="CurrencyCode">ارز یگانهٔ اثبات‌شده؛ تهی وقتی ارز یگانه‌ای اثبات نشود.</param>
/// <param name="RuleCount">تعداد قاعده‌های فعالِ متعلق به همین روش.</param>
/// <param name="ConditionedRuleCount">تعداد قاعده‌های وابسته به پلهٔ وزن/تعداد.</param>
/// <param name="HasZeroChargeRule">
/// آیا مبلغ صفر در دادهٔ همین روش ذخیره شده است. این یک «واقعیت داده» است، نه حالت «رایگان»:
/// اسکیمای فعلی هیچ حالت رایگانِ صریحی ندارد، پس مبلغ صفر نباید رایگان تفسیر شود.
/// </param>
/// <param name="ActiveServiceLevelRuleCount">
/// تعداد ردیف‌های فعال در سطح سرویس (<c>optionCode</c> تهی) برای همین کد سرویس. برای روش گزینه‌دار
/// این ردیف‌ها مالکیت روش را اثبات نمی‌کنند و فقط خوانا/قابل‌حسابرسی می‌مانند.
/// </param>
/// <param name="ActiveSiblingOptionRuleCount">
/// تعداد ردیف‌های فعال متعلق به گزینه‌های دیگر همان سرویس. فقط شمارش است تا مرز جداسازی روش‌ها
/// قابل‌مشاهده باشد؛ هیچ نرخ هم‌خانواده‌ای به این روش نسبت داده نمی‌شود.
/// </param>
/// <param name="ActiveCoverageGrantCount">
/// تعداد مجوزهای پوشش فعال همین سرویس. پوشش در دامنهٔ مقصد اعطا می‌شود، بنابراین این عدد
/// «آمادهٔ استعلام برای همه‌جا» نیست و صرفاً وجود حداقل یک مقصد مجاز را نشان می‌دهد.
/// </param>
/// <param name="Rules">قاعده‌های فعالِ همین روش؛ در حالت <c>LegacySharedUnresolved</c> خالی می‌ماند.</param>
/// <param name="ReasonCode">کد دلیل پایدار و مستقل از زبان.</param>
public sealed record ShippingMethodFinancialStatusView(
    string ServiceCode,
    string? OptionCode,
    ShippingMethodFinancialState State,
    ShippingMethodFinancialMode Mode,
    string? CurrencyCode,
    int RuleCount,
    int ConditionedRuleCount,
    bool HasZeroChargeRule,
    int ActiveServiceLevelRuleCount,
    int ActiveSiblingOptionRuleCount,
    int ActiveCoverageGrantCount,
    IReadOnlyList<ShippingMethodFinancialRule> Rules,
    string ReasonCode);

/// <summary>کدهای دلیلِ پایدار وضعیت مالی روش؛ مستقل از زبان و مناسب برای لاگ/تلمتری/رابط ادمین.</summary>
public static class ShippingMethodFinancialReasonCodes
{
    /// <summary>قاعده‌های نرخ فعالِ متعلق به همین روش با ارز یگانه یافت شد.</summary>
    public const string OwnedRulesPresent = "shipping.financial.owned_rules_present";

    /// <summary>هیچ قاعدهٔ نرخ فعالی برای این هویت یافت نشد؛ روش بدون تنظیم مالی است.</summary>
    public const string NoOwnedRules = "shipping.financial.no_owned_rules";

    /// <summary>تنها ردیف‌های سطح سرویس فعال‌اند و این روش گزینهٔ اختصاصی دارد.</summary>
    public const string LegacyServiceLevelShared = "shipping.financial.legacy_service_level_shared";

    /// <summary>دو قاعدهٔ فعالِ همین روش دامنه/شرط یکسان دارند و انتخاب یکی ابهام تجاری است.</summary>
    public const string AmbiguousOwnedScope = "shipping.financial.ambiguous_owned_scope";

    /// <summary>قاعده‌های فعالِ همین روش بیش از یک ارز اعلام می‌کنند.</summary>
    public const string MixedCurrency = "shipping.financial.mixed_currency";
}
