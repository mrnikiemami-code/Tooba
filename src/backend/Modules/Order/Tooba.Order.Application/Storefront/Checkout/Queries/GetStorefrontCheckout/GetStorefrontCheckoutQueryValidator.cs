using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontCheckout;

public sealed class GetStorefrontCheckoutQueryValidator : AbstractValidator<GetStorefrontCheckoutQuery>
{
    public GetStorefrontCheckoutQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireCartId(this, x => x.CartId);
    }
}