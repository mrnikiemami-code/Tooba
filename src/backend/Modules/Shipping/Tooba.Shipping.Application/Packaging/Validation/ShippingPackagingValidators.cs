using FluentValidation;
using Tooba.Shipping.Application.Packaging.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Application.Packaging.Validation;

/// <summary>
/// اعتبارسنج شکلِ transport سیاست هزینهٔ بسته‌بندی. فقط شکل بررسی می‌شود؛ ناورداهای تجاری
/// (یکتایی دامنه، وجود خدمت، دامنهٔ هم‌بسته) در Domain و زیرساخت می‌مانند و اینجا تکرار نمی‌شوند.
/// </summary>
public sealed class ShippingPackagingPolicyWriteRequestValidator
    : AbstractValidator<ShippingPackagingPolicyWriteRequest>
{
    /// <summary>قواعد شکل درخواست بسته‌بندی را تعریف می‌کند.</summary>
    public ShippingPackagingPolicyWriteRequestValidator()
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

        // A missing scope is a declared validation failure, not an exception: nested rules only run
        // when the scope itself exists.
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

        RuleFor(x => x.FeeAmount)
            .GreaterThanOrEqualTo(0m).WithErrorCode(ShippingValidationCodes.PackagingFeeNegative)
            .LessThanOrEqualTo(ShippingPackagingPolicy.FeeAmountMaxValue)
            .WithErrorCode(ShippingValidationCodes.PackagingFeeOutOfRange);
        RuleFor(x => x.CurrencyCode)
            .Must(BeThreeLetters).WithErrorCode(ShippingValidationCodes.PackagingCurrencyInvalid);

        // The payer mode is a stable code contract: an out-of-contract value is a declared 400 rather
        // than a domain exception, while null keeps the documented Prepaid default.
        RuleFor(x => x.FreightPayerMode)
            .Must(BeKnownPayerMode).When(x => x.FreightPayerMode is not null)
            .WithErrorCode(ShippingValidationCodes.PackagingFreightPayerModeInvalid);
    }

    private static bool BeThreeLetters(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && raw.Trim().Length == 3
        && raw.Trim().All(char.IsAsciiLetter);

    private static bool BeKnownPayerMode(ShippingFreightPayerMode? mode)
        => mode is null
           or ShippingFreightPayerMode.Prepaid
           or ShippingFreightPayerMode.CollectOnDelivery;
}

/// <summary>اعتبارسنج درخواست ایجاد سیاست بسته‌بندی.</summary>
public sealed class CreateShippingPackagingPolicyCommandValidator
    : AbstractValidator<CreateShippingPackagingPolicyCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public CreateShippingPackagingPolicyCommandValidator()
    {
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull().SetValidator(new ShippingPackagingPolicyWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست ویرایش سیاست بسته‌بندی.</summary>
public sealed class UpdateShippingPackagingPolicyCommandValidator
    : AbstractValidator<UpdateShippingPackagingPolicyCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public UpdateShippingPackagingPolicyCommandValidator()
    {
        RuleFor(x => x.PackagingPolicyId).NotEmpty();
        ShippingCommandScopeRules.Apply(this, x => x.ServiceCode, x => x.Request?.ServiceCode);
        RuleFor(x => x.Request).NotNull().SetValidator(new ShippingPackagingPolicyWriteRequestValidator()!);
    }
}

/// <summary>اعتبارسنج درخواست تغییر وضعیت سیاست بسته‌بندی.</summary>
public sealed class SetShippingPackagingPolicyActiveCommandValidator
    : AbstractValidator<SetShippingPackagingPolicyActiveCommand>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public SetShippingPackagingPolicyActiveCommandValidator()
    {
        RuleFor(x => x.PackagingPolicyId).NotEmpty();
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);
    }
}

/// <summary>اعتبارسنج درخواست خواندن سیاست‌های بسته‌بندی.</summary>
public sealed class GetShippingPackagingPoliciesQueryValidator
    : AbstractValidator<GetShippingPackagingPoliciesQuery>
{
    /// <summary>قواعد را تعریف می‌کند.</summary>
    public GetShippingPackagingPoliciesQueryValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().WithErrorCode(ShippingValidationCodes.ServiceCodeRequired)
            .MaximumLength(ShippingRatePolicy.ServiceCodeMaxLength)
            .WithErrorCode(ShippingValidationCodes.ServiceCodeTooLong);
    }
}
