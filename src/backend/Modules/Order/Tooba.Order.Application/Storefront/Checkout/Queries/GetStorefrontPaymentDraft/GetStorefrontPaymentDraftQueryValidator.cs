using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontPaymentDraft;

public sealed class GetStorefrontPaymentDraftQueryValidator : AbstractValidator<GetStorefrontPaymentDraftQuery>
{
    public GetStorefrontPaymentDraftQueryValidator()
    {
        OrderFluentRules.RequireCartId(this, x => x.CartId);
    }
}
