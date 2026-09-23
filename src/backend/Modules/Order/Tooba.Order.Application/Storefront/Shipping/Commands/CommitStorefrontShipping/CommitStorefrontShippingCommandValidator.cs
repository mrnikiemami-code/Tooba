using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Shipping.Commands.CommitStorefrontShipping;

public sealed class CommitStorefrontShippingCommandValidator : AbstractValidator<CommitStorefrontShippingCommand>
{
    public CommitStorefrontShippingCommandValidator()
    {
        OrderFluentRules.RequireCartId(this, x => x.CartId);
        OrderFluentRules.RequireExpectedCartVersionMin(this, x => x.ExpectedCartVersion);
        OrderFluentRules.RequireIdempotencyKey(this, x => x.IdempotencyKey);
    }
}