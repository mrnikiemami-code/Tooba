using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;

/// <summary>Syntactic rules for cancel-order transport body (business state stays in orchestrator).</summary>
public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(OrderValidationCodes.OperationRequestRequired);
        RuleFor(x => x.Request.Code)
            .NotEmpty()
            .When(x => x.Request is not null)
            .WithErrorCode(OrderValidationCodes.OperationCodeRequired);
    }
}
