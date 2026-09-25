using System.Linq.Expressions;
using FluentValidation;

namespace Tooba.Fulfillment.Application.Validators;

/// <summary>
/// Reusable FluentValidation fragments for Fulfillment transport/input shape.
/// Only primitive/syntactic shape is checked here. Authorization, ownership, existence,
/// state, mutation eligibility, grid policy semantics, bulk business rules and shipping
/// directory semantics stay in the Application/Domain layers.
/// </summary>
public static class FulfillmentFluentRules
{
    /// <summary>Requires a non-empty identifier.</summary>
    public static void RequireId<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> selector,
        string errorCode)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(errorCode);

    /// <summary>Optional identifier: absent is allowed; a supplied value must not be empty.</summary>
    public static void OptionalIdShape<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid?>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Value != Guid.Empty)
            .WithErrorCode(errorCode);

    /// <summary>Requires a non-null reference value.</summary>
    public static void RequireReference<T, TValue>(
        AbstractValidator<T> validator,
        Expression<Func<T, TValue>> selector,
        string errorCode)
        where TValue : class
        => validator.RuleFor(selector).NotNull().WithErrorCode(errorCode);

    /// <summary>Optional text: absent is allowed; a supplied value must not be whitespace-only.</summary>
    public static void OptionalNonBlankShape<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, string?>> selector,
        string errorCode)
        => validator.RuleFor(selector)
            .Must(value => value is null || value.Trim().Length > 0)
            .WithErrorCode(errorCode);
}
