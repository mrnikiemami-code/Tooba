using FluentValidation;
using Tooba.Cart.Application.Validation;

namespace Tooba.Cart.Application.Queries.GetCart;

/// <summary>Transport validation for <see cref="GetCartQuery"/>; ownership/guest-secret checks stay in the handler.</summary>
public sealed class GetCartQueryValidator : AbstractValidator<GetCartQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetCartQueryValidator()
    {
        CartFluentRules.RequireCartId(this, x => x.CartId);
    }
}
