using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.HidePendingPaymentCard;

public sealed class HidePendingPaymentCardCommandValidator : AbstractValidator<HidePendingPaymentCardCommand>
{
    public HidePendingPaymentCardCommandValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireCartId(this, x => x.CartId);
    }
}