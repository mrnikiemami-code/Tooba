namespace Tooba.Shipping.Contracts.Errors;

/// <summary>
/// کدهای خطای پایدار Shipping برای سیاست نرخ و پوشش. کدها مستقل از زبان‌اند و هیچ متن
/// محلی‌سازی‌شده‌ای در این لایه نگه‌داری نمی‌شود.
/// </summary>
public static class ShippingErrorCodes
{
    /// <summary>کد خدمت ارسال الزامی است.</summary>
    public const string ServiceCodeRequired = "shipping.service_code.required";

    /// <summary>کد خدمت ارسال خارج از محدودهٔ مجاز طول است.</summary>
    public const string ServiceCodeInvalid = "shipping.service_code.invalid";

    /// <summary>شناسهٔ کشور مقصد الزامی است.</summary>
    public const string DestinationCountryIdRequired = "shipping.destination.country_id.required";

    /// <summary>مبلغ هزینه نباید منفی باشد.</summary>
    public const string ChargeAmountNegative = "shipping.charge.amount.negative";

    /// <summary>مبلغ هزینه خارج از محدودهٔ مجاز است.</summary>
    public const string ChargeAmountOutOfRange = "shipping.charge.amount.out_of_range";

    /// <summary>کد ارز الزامی است و باید کد سه‌حرفی باشد.</summary>
    public const string CurrencyCodeInvalid = "shipping.charge.currency.invalid";

    /// <summary>روزهای آماده‌سازی نباید منفی باشد.</summary>
    public const string PreparationDaysNegative = "shipping.preparation_days.negative";

    /// <summary>روزهای حمل نباید منفی باشد.</summary>
    public const string LeadDaysNegative = "shipping.lead_days.negative";

    /// <summary>آستانهٔ ارسال رایگان نباید منفی باشد.</summary>
    public const string FreeAboveThresholdNegative = "shipping.free_above_threshold.negative";

    /// <summary>سطح دامنهٔ پوشش نامعتبر است (ترکیب شناسه‌های مقصد ناسازگار).</summary>
    public const string CoverageScopeInvalid = "shipping.coverage.scope.invalid";

    /// <summary>سیاست نرخ با این شناسه یافت نشد.</summary>
    public const string RatePolicyNotFound = "shipping.rate_policy.not_found";

    /// <summary>مجوز پوشش با این شناسه یافت نشد.</summary>
    public const string CoverageGrantNotFound = "shipping.coverage_grant.not_found";

    /// <summary>برای همین کد خدمت و همین دامنهٔ مقصد از قبل یک سیاست نرخ وجود دارد.</summary>
    public const string RatePolicyScopeDuplicate = "shipping.rate_policy.scope_duplicate";

    /// <summary>برای همین کد خدمت و همین دامنهٔ مقصد از قبل یک مجوز پوشش وجود دارد.</summary>
    public const string CoverageGrantScopeDuplicate = "shipping.coverage_grant.scope_duplicate";

    /// <summary>کد خدمت در هیچ ردیف کاتالوگ خدمت Fulfillment وجود ندارد.</summary>
    public const string ServiceCodeUnknown = "shipping.service_code.unknown";

    /// <summary>ردیف درخواستی به کد خدمت دیگری تعلق دارد؛ تغییر میان‌خدمتی مجاز نیست.</summary>
    public const string ServiceScopeMismatch = "shipping.service_scope.mismatch";

    /// <summary>دامنهٔ مقصد در فهرست مرجع Geography ناشناخته، غیرفعال یا ناهم‌بسته است.</summary>
    public const string DestinationScopeInvalid = "shipping.destination_scope.invalid";

    /// <summary>کد گزینهٔ روش ارسال خارج از محدودهٔ مجاز طول است.</summary>
    public const string OptionCodeInvalid = "shipping.option_code.invalid";

    /// <summary>کد گزینهٔ ارسال در کاتالوگ خدمت Fulfillment برای این خدمت وجود ندارد.</summary>
    public const string ServiceOptionUnknown = "shipping.service_option.unknown";

    /// <summary>هویت روش (خدمت + گزینه) یک ردیف با مسیر/بدنهٔ درخواست یکسان نیست.</summary>
    public const string RatePolicyOptionMismatch = "shipping.rate_policy.option_mismatch";

    /// <summary>شناسهٔ منطقهٔ زمانی برنامهٔ عملیاتی الزامی است.</summary>
    public const string ScheduleTimeZoneRequired = "shipping.schedule.timezone.required";

    /// <summary>شناسهٔ منطقهٔ زمانی روی این runtime قابل حل نیست (ترجیحاً IANA).</summary>
    public const string ScheduleTimeZoneInvalid = "shipping.schedule.timezone.invalid";

    /// <summary>شمارهٔ روز هفته خارج از بازهٔ ISO 1..7 است.</summary>
    public const string ScheduleWeekdayInvalid = "shipping.schedule.weekday.invalid";

    /// <summary>
    /// بازهٔ پنجرهٔ عملیاتی نامعتبر است: <c>end &lt;= start</c>. این حالت شامل پنجرهٔ معکوس،
    /// پنجرهٔ با طول صفر و پنجرهٔ شبانه (عبور از نیمه‌شب) است که در این نسخه پشتیبانی نمی‌شود.
    /// </summary>
    public const string ScheduleWindowRangeInvalid = "shipping.schedule.window.range_invalid";

    /// <summary>دو پنجرهٔ عملیاتی روی یک روز هفته هم‌پوشانی دارند.</summary>
    public const string ScheduleWindowOverlap = "shipping.schedule.window.overlap";

    /// <summary>ظرفیت اعلام‌شدهٔ پنجرهٔ عملیاتی باید مثبت باشد.</summary>
    public const string ScheduleWindowCapacityInvalid = "shipping.schedule.window.capacity.invalid";

    /// <summary>تعداد پنجره‌های عملیاتی از سقف مجاز بیشتر است.</summary>
    public const string ScheduleWindowsTooMany = "shipping.schedule.windows.too_many";

    /// <summary>برنامهٔ عملیاتی با این شناسه یافت نشد.</summary>
    public const string ScheduleNotFound = "shipping.schedule.not_found";

    /// <summary>برای همین خدمت و همین گزینه از قبل یک برنامهٔ عملیاتی وجود دارد.</summary>
    public const string ScheduleDuplicate = "shipping.schedule.duplicate";

    /// <summary>هویت روش برنامهٔ عملیاتی با مسیر/بدنهٔ درخواست یکسان نیست.</summary>
    public const string ScheduleOptionMismatch = "shipping.schedule.option_mismatch";
}
