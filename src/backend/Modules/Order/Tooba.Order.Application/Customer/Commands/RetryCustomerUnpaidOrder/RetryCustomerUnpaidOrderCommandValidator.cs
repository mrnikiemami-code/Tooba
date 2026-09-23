using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Customer.Commands.RetryCustomerUnpaidOrder;

/// <summary>Transport Guid primitives for unpaid retry (ownership/state stay in handler).</summary>
public sealed class RetryCustomerUnpaidOrderCommandValidator : AbstractValidator<RetryCustomerUnpaidOrderCommand>
{
    public RetryCustomerUnpaidOrderCommandValidator()
    {
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
    }
}
