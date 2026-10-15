using FluentValidation;
using Tooba.Shipping.Application.Operations.Queries;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Application.Validation;

/// <summary>کدهای اعتبارسنجی شکلِ transport برای ارزیابی عملیاتی Shipping.</summary>
public static class ShippingOperationalValidationCodes
{
    /// <summary>کد خدمت الزامی است.</summary>
    public const string ServiceCodeRequired = "shipping.validation.service_code_required";

    /// <summary>کد خدمت از طول مجاز بیشتر است.</summary>
    public const string ServiceCodeTooLong = "shipping.validation.service_code_too_long";

    /// <summary>دامنهٔ مقصد الزامی است.</summary>
    public const string ScopeRequired = "shipping.validation.scope_required";

    /// <summary>کد کشور الزامی است.</summary>
    public const string CountryRequired = "shipping.validation.country_required";

    /// <summary>شناسهٔ ناحیهٔ خدمت بدون شهر مالک نامعتبر است.</summary>
    public const string ServiceAreaRequiresCity = "shipping.validation.service_area_requires_city";

    /// <summary>کد گزینهٔ روش ارسال از طول مجاز بیشتر است.</summary>
    public const string OptionCodeTooLong = "shipping.validation.option_code_too_long";

    /// <summary>جمع سبد نباید منفی باشد.</summary>
    public const string CartSubtotalNegative = "shipping.validation.cart_subtotal_negative";

    /// <summary>ارز درخواست باید سه حرف باشد.</summary>
    public const string CurrencyInvalid = "shipping.validation.currency_invalid";

    /// <summary>وزن اندازه‌گیری‌شده نباید منفی باشد.</summary>
    public const string WeightNegative = "shipping.validation.operational_weight_negative";

    /// <summary>وزن اندازه‌گیری‌شده خارج از سقف متعارف است.</summary>
    public const string WeightOutOfRange = "shipping.validation.operational_weight_out_of_range";

    /// <summary>تعداد اقلام باید مثبت باشد؛ صفر یک مرسولهٔ خالی است، نه یک اندازه.</summary>
    public const string ItemCountOutOfRange = "shipping.validation.operational_item_count_out_of_range";
}

/// <summary>
/// اعتبارسنج شکلِ transport ورودی ارزیابی عملیاتی. فقط شکل بررسی می‌شود؛ وجود خدمت/گزینه و
/// هم‌بستگی دامنه با فهرست مرجع Geography در handler و seamها می‌ماند و اینجا تکرار نمی‌شود.
/// </summary>
public sealed class ShippingOperationalQuoteEvaluationRequestValidator
    : AbstractValidator<ShippingOperationalQuoteEvaluationRequest>
{
    /// <summary>قواعد شکل درخواست ارزیابی را تعریف می‌کند.</summary>
    public ShippingOperationalQuoteEvaluationRequestValidator()
    {
        // A destination is required to price anything at all; a null scope must be a declared 400 rather
        // than a null-reference exception from nested property rules.
        RuleFor(x => x.Scope)
            .NotNull().WithErrorCode(ShippingOperationalValidationCodes.ScopeRequired);
        When(x => x.Scope is not null, () =>
        {
            RuleFor(x => x.Scope.CountryId)
                .NotEmpty().WithErrorCode(ShippingOperationalValidationCodes.CountryRequired);
            RuleFor(x => x.Scope.ServiceAreaId)
                .Null().When(x => x.Scope.CityId is null)
                .WithErrorCode(ShippingOperationalValidationCodes.ServiceAreaRequiresCity);
        });

        RuleFor(x => x.OptionCode)
            .MaximumLength(ShippingRatePolicy.OptionCodeMaxLength)
            .WithErrorCode(ShippingOperationalValidationCodes.OptionCodeTooLong);

        RuleFor(x => x.CartSubtotalExclusive)
            .GreaterThanOrEqualTo(0m)
            .WithErrorCode(ShippingOperationalValidationCodes.CartSubtotalNegative);

        RuleFor(x => x.CurrencyCode)
            .Must(BeThreeLetters)
            .When(x => x.CurrencyCode is not null)
            .WithErrorCode(ShippingOperationalValidationCodes.CurrencyInvalid);

        RuleFor(x => x.WeightGrams)
            .GreaterThanOrEqualTo(0L)
            .When(x => x.WeightGrams is not null)
            .WithErrorCode(ShippingOperationalValidationCodes.WeightNegative);

        RuleFor(x => x.WeightGrams)
            .LessThanOrEqualTo(ShippingRatePolicy.WeightGramsMaxValue)
            .When(x => x.WeightGrams is not null)
            .WithErrorCode(ShippingOperationalValidationCodes.WeightOutOfRange);

        // Item count is a positive physical quantity: zero is an empty shipment, not a measurement, so it
        // is refused instead of being silently treated as "no measurement".
        RuleFor(x => x.ItemCount)
            .InclusiveBetween(1, ShippingRatePolicy.ItemCountMaxValue)
            .When(x => x.ItemCount is not null)
            .WithErrorCode(ShippingOperationalValidationCodes.ItemCountOutOfRange);
    }

    private static bool BeThreeLetters(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && raw.Trim().Length == 3
        && raw.Trim().All(char.IsAsciiLetter);
}

/// <summary>اعتبارسنج درخواست ارزیابی عملیاتی.</summary>
public sealed class EvaluateShippingOperationalQuoteQueryValidator
    : AbstractValidator<EvaluateShippingOperationalQuoteQuery>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public EvaluateShippingOperationalQuoteQueryValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingOperationalValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingOperationalValidationCodes.ServiceCodeTooLong);

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new ShippingOperationalQuoteEvaluationRequestValidator()!);
    }
}
