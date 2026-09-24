using FluentValidation;
using Tooba.Cart.Application.Validation;

namespace Tooba.Cart.Application.Commands.AddCartLine;

/// <summary>
/// Transport validation for <see cref="AddCartLineCommand"/>.
/// Quantity/offer availability and cart/version semantics remain business validation.
/// </summary>
public sealed class AddCartLineCommandValidator : AbstractValidator<AddCartLineCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public AddCartLineCommandValidator()
    {
        CartFluentRules.RequireCartId(this, x => x.CartId);
        CartFluentRules.RequireOfferId(this, x => x.OfferId);
        CartFluentRules.RequireExpectedVersionMin(this, x => x.ExpectedVersion);
        CartFluentRules.OptionalCurrencyShape(this, x => x.Currency);
    }
}
