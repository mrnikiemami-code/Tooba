using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.PreviewStorefrontCheckout;

public sealed class PreviewStorefrontCheckoutQueryValidator : AbstractValidator<PreviewStorefrontCheckoutQuery>
{
    public PreviewStorefrontCheckoutQueryValidator()
    {
        OrderFluentRules.RequireCartId(this, x => x.CartId);
    }
}