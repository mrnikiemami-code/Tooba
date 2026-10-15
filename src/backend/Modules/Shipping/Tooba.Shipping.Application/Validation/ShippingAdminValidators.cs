using FluentValidation;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Application.Validation;

/// <summary>کدهای اعتبارسنجی شکلِ transport برای نویسندگی نرخ/پوشش Shipping.</summary>
public static class ShippingValidationCodes
{
    /// <summary>کد خدمت الزامی است.</summary>
    public const string ServiceCodeRequired = "shipping.validation.service_code_required";

    /// <summary>کد خدمت از طول مجاز بیشتر است.</summary>
    public const string ServiceCodeTooLong = "shipping.validation.service_code_too_long";

    /// <summary>
    /// دامنهٔ مقصد الزامی است. کد اعلام‌شدهٔ پایدار برای شکست اعتبارسنجی دامنهٔ غایب؛ بدون آن،
    /// خواندن خصیصه‌های تودرتوی دامنه استثنای مرجع‌نشده می‌داد و به‌جای ۴xx به خطای بستر می‌رسید.
    /// </summary>
    public const string ScopeRequired = "shipping.validation.scope_required";

    /// <summary>کد کشور الزامی است.</summary>
    public const string CountryRequired = "shipping.validation.country_required";

    /// <summary>شناسهٔ ناحیهٔ خدمت بدون شهر مالک نامعتبر است.</summary>
    public const string ServiceAreaRequiresCity = "shipping.validation.service_area_requires_city";

    /// <summary>مبلغ هزینه نباید منفی باشد.</summary>
    public const string ChargeNegative = "shipping.validation.charge_negative";

    /// <summary>مبلغ هزینه خارج از محدودهٔ مجاز است.</summary>
    public const string ChargeOutOfRange = "shipping.validation.charge_out_of_range";

    /// <summary>کد ارز باید سه حرف باشد.</summary>
    public const string CurrencyInvalid = "shipping.validation.currency_invalid";

    /// <summary>روزهای آماده‌سازی خارج از بازهٔ مجاز است.</summary>
    public const string PreparationDaysOutOfRange = "shipping.validation.preparation_days_out_of_range";

    /// <summary>روزهای حمل خارج از بازهٔ مجاز است.</summary>
    public const string LeadDaysOutOfRange = "shipping.validation.lead_days_out_of_range";

    /// <summary>آستانهٔ ارسال رایگان نباید منفی باشد.</summary>
    public const string FreeAboveThresholdNegative = "shipping.validation.free_above_threshold_negative";

    /// <summary>کد خدمت بدنه با کد خدمت مسیر یکسان نیست.</summary>
    public const string BodyServiceCodeMismatch = "shipping.validation.body_service_code_mismatch";

    /// <summary>کد گزینهٔ روش ارسال از طول مجاز بیشتر است.</summary>
    public const string OptionCodeTooLong = "shipping.validation.option_code_too_long";

    /// <summary>
    /// بدنهٔ درخواست خوانا نیست: JSON نامعتبر، contentType غیر-JSON، شناسهٔ نامعتبر/تهی یا بدنهٔ
    /// کاملاً غایب. ورودی سطح کلاینت است و باید ۴۰۰ اعلام‌شده بدهد، نه خطای بستر.
    /// </summary>
    public const string BodyInvalid = "shipping.validation.body_invalid";
}

/// <summary>
/// قاعدهٔ مشترک «کد خدمت مسیر مرجع است»: بدنه اگر کد خدمت بدهد باید همان باشد، وگرنه درخواست رد
/// می‌شود تا هیچ کلاینتی نتواند با مقدار بدنه، مسیر را دور بزند.
/// </summary>
internal static class ShippingCommandScopeRules
{
    internal static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string> routeServiceCode,
        Func<T, string?> bodyServiceCode)
    {
        validator.RuleFor(x => x)
            .Must(x => SameService(routeServiceCode(x), bodyServiceCode(x)))
            .WithErrorCode(ShippingValidationCodes.BodyServiceCodeMismatch)
            .WithName("serviceCode");
    }

    private static bool SameService(string? routeCode, string? bodyCode) =>
        string.IsNullOrWhiteSpace(bodyCode)
        || string.Equals(routeCode?.Trim(), bodyCode.Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// اعتبارسنج شکلِ transport سیاست نرخ. فقط شکل بررسی می‌شود؛ ناورداهای تجاری (یکتایی دامنه،
/// وجود خدمت، دامنهٔ هم‌بسته) در Domain و زیرساخت می‌مانند و اینجا تکرار نمی‌شوند.
/// </summary>
public sealed class ShippingRatePolicyWriteRequestValidator : AbstractValidator<ShippingRatePolicyWriteRequest>
{
    /// <summary>قواعد شکل درخواست نرخ را تعریف می‌کند.</summary>
    public ShippingRatePolicyWriteRequestValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);

        // The option code is an identity token, not prose: shape only (length), and the real catalog
        // lookup stays in the handler through the Fulfillment Contracts seam.
        RuleFor(x => x.OptionCode)
            .MaximumLength(ShippingRatePolicy.OptionCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.OptionCodeTooLong);

        // دامنهٔ غایب یک شکست اعتبارسنجی اعلام‌شده است، نه استثنا: قواعد خصیصه‌های تودرتو فقط
        // وقتی ارزیابی می‌شوند که خود دامنه وجود داشته باشد، وگرنه دسترسی تودرتو استثنای
        // مرجع‌نشده می‌داد و درخواست به‌جای ۴xx به خطای بستر می‌رسید.
        RuleFor(x => x.Scope)
            .NotNull().WithErrorCode(ShippingValidationCodes.ScopeRequired);
        When(x => x.Scope is not null, () =>
        {
            RuleFor(x => x.Scope.CountryId)
                .NotEmpty().WithErrorCode(ShippingValidationCodes.CountryRequired);
            RuleFor(x => x.Scope.ServiceAreaId)
                .Null().When(x => x.Scope.CityId is null)
                .WithErrorCode(ShippingValidationCodes.ServiceAreaRequiresCity);
        });

        RuleFor(x => x.ChargeAmount)
            .GreaterThanOrEqualTo(0m).WithErrorCode(ShippingValidationCodes.ChargeNegative)
            .LessThanOrEqualTo(ShippingRatePolicy.ChargeAmountMaxValue)
            .WithErrorCode(ShippingValidationCodes.ChargeOutOfRange);
        RuleFor(x => x.CurrencyCode)
            .Must(BeThreeLetters).WithErrorCode(ShippingValidationCodes.CurrencyInvalid);
        RuleFor(x => x.PreparationDays)
            .InclusiveBetween(0, ShippingRatePolicy.DaysMaxValue)
            .WithErrorCode(ShippingValidationCodes.PreparationDaysOutOfRange);
        RuleFor(x => x.LeadDays)
            .InclusiveBetween(0, ShippingRatePolicy.DaysMaxValue)
            .WithErrorCode(ShippingValidationCodes.LeadDaysOutOfRange);
        RuleFor(x => x.FreeAboveThreshold)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.FreeAboveThreshold is not null)
            .WithErrorCode(ShippingValidationCodes.FreeAboveThresholdNegative);
    }

    private static bool BeThreeLetters(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && raw.Trim().Length == 3
        && raw.Trim().All(char.IsAsciiLetter);
}

/// <summary>اعتبارسنج شکلِ transport مجوز پوشش.</summary>
public sealed class ShippingCoverageGrantWriteRequestValidator : AbstractValidator<ShippingCoverageGrantWriteRequest>
{
    /// <summary>قواعد شکل درخواست پوشش را تعریف می‌کند.</summary>
    public ShippingCoverageGrantWriteRequestValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);

        // همان مرز دامنه در درخواست پوشش: نبود دامنه باید ۴xx اعلام‌شده بدهد، نه استثنای مرجع‌نشده.
        RuleFor(x => x.Scope)
            .NotNull().WithErrorCode(ShippingValidationCodes.ScopeRequired);
        When(x => x.Scope is not null, () =>
        {
            RuleFor(x => x.Scope.CountryId)
                .NotEmpty().WithErrorCode(ShippingValidationCodes.CountryRequired);
            RuleFor(x => x.Scope.ServiceAreaId)
                .Null().When(x => x.Scope.CityId is null)
                .WithErrorCode(ShippingValidationCodes.ServiceAreaRequiresCity);
        });
    }
}

/// <summary>اعتبارسنج درخواست ایجاد سیاست نرخ.</summary>
public sealed class CreateShippingRatePolicyCommandValidator : AbstractValidator<CreateShippingRatePolicyCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public CreateShippingRatePolicyCommandValidator()
    {
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull().SetValidator(new ShippingRatePolicyWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست ویرایش سیاست نرخ.</summary>
public sealed class UpdateShippingRatePolicyCommandValidator : AbstractValidator<UpdateShippingRatePolicyCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public UpdateShippingRatePolicyCommandValidator()
    {
        RuleFor(x => x.RatePolicyId).NotEmpty();
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull().SetValidator(new ShippingRatePolicyWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست ایجاد مجوز پوشش.</summary>
public sealed class CreateShippingCoverageGrantCommandValidator : AbstractValidator<CreateShippingCoverageGrantCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public CreateShippingCoverageGrantCommandValidator()
    {
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull().SetValidator(new ShippingCoverageGrantWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست تغییر وضعیت سیاست نرخ.</summary>
public sealed class SetShippingRatePolicyActiveCommandValidator : AbstractValidator<SetShippingRatePolicyActiveCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public SetShippingRatePolicyActiveCommandValidator()
    {
        RuleFor(x => x.RatePolicyId).NotEmpty();
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);
    }
}

/// <summary>اعتبارسنج درخواست تغییر وضعیت مجوز پوشش.</summary>
public sealed class SetShippingCoverageGrantActiveCommandValidator : AbstractValidator<SetShippingCoverageGrantActiveCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public SetShippingCoverageGrantActiveCommandValidator()
    {
        RuleFor(x => x.CoverageGrantId).NotEmpty();
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);
    }
}

/// <summary>اعتبارسنج درخواست خواندن نرخ و پوشش.</summary>
public sealed class GetShippingRatesAndCoverageQueryValidator : AbstractValidator<GetShippingRatesAndCoverageQuery>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public GetShippingRatesAndCoverageQueryValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);
    }
}
