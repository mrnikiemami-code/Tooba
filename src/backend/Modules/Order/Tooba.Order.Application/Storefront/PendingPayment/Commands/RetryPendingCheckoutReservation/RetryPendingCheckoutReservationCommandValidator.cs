using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.PendingPayment.Commands.RetryPendingCheckoutReservation;

/// <summary>
/// Transport Guid primitives for pending checkout reservation retry.
/// مالکیت واقعی (actor یا proof سبد) در سرویس اعمال می‌شود.
/// </summary>
public sealed class RetryPendingCheckoutReservationCommandValidator
    : AbstractValidator<RetryPendingCheckoutReservationCommand>
{
    public RetryPendingCheckoutReservationCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.CartId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CartIdRequired);
    }
}
