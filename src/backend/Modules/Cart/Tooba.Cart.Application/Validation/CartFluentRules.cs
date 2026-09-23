using System.Linq.Expressions;
using FluentValidation;

namespace Tooba.Cart.Application.Validation;

/// <summary>
/// Reusable FluentValidation fragments for Cart transport input.
/// Only primitive/syntactic shape is checked here; business state (existence, ownership,
/// version conflict, offer/stock, merge eligibility) stays in Application/Domain.
/// </summary>
public static class CartFluentRules
{
    /// <summary>Requires a non-empty cart identifier.</summary>
    /// <typeparam name="T">Request type.</typeparam>
    /// <param name="validator">Target validator.</param>
    /// <param name="selector">Cart id selector.</param>
    public static void RequireCartId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(CartValidationCodes.CartIdRequired);

    /// <summary>Requires a non-empty offer identifier.</summary>
    /// <typeparam name="T">Request type.</typeparam>
    /// <param name="validator">Target validator.</param>
    /// <param name="selector">Offer id selector.</param>
    public static void RequireOfferId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(CartValidationCodes.OfferIdRequired);

    /// <summary>Requires a non-empty cart line identifier.</summary>
    /// <typeparam name="T">Request type.</typeparam>
    /// <param name="validator">Target validator.</param>
    /// <param name="selector">Line id selector.</param>
    public static void RequireLineId<T>(AbstractValidator<T> validator, Expression<Func<T, Guid>> selector)
        => validator.RuleFor(selector).NotEmpty().WithErrorCode(CartValidationCodes.LineIdRequired);

    /// <summary>Requires a non-negative expected version.</summary>
    /// <typeparam name="T">Request type.</typeparam>
    /// <param name="validator">Target validator.</param>
    /// <param name="selector">Expected version selector.</param>
    public static void RequireExpectedVersionMin<T>(AbstractValidator<T> validator, Expression<Func<T, int>> selector)
        => validator.RuleFor(selector).GreaterThanOrEqualTo(0).WithErrorCode(CartValidationCodes.ExpectedVersionMin);
}
