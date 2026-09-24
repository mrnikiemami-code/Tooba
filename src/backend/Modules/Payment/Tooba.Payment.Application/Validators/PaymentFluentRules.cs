using System.Linq.Expressions;
using FluentValidation;

namespace Tooba.Payment.Application.Validators;

/// <summary>
/// Reusable FluentValidation fragments for Payment storefront transport input.
/// Only primitive/syntactic shape is checked here; ownership, authorization, DB existence,
/// gateway availability, payment state, eligibility and payable amount stay in Application/Domain.
/// </summary>
public static class PaymentFluentRules
{
    /// <summary>Requires a non-empty identifier.</summary>
    public static void RequireId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector, string errorCode)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(errorCode);

    /// <summary>Optional identifier: absent is allowed; a supplied value must not be empty.</summary>
    public static void OptionalIdShape<T>(AbstractValidator<T> validator, Expression<Func<T, Guid?>> selector, string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Value != Guid.Empty)
            .WithErrorCode(errorCode);

    /// <summary>Requires non-blank text after trimming.</summary>
    public static void RequireNonBlank<T>(AbstractValidator<T> validator, Expression<Func<T, string>> selector, string errorCode)
        => validator.RuleFor(selector)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithErrorCode(errorCode);

    /// <summary>Optional text: absent is allowed; a supplied value must not be whitespace-only.</summary>
    public static void OptionalNonBlankShape<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Trim().Length > 0)
            .WithErrorCode(errorCode);
}
