using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.CancelPendingCheckout;

/// <summary>Transport Guid primitives for pending checkout cancel.</summary>
public sealed class CancelPendingCheckoutCommandValidator : AbstractValidator<CancelPendingCheckoutCommand>
{
    public CancelPendingCheckoutCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.CartId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CartIdRequired);
    }
}
