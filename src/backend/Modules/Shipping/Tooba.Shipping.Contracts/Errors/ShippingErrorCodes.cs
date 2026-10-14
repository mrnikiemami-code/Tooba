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
}
