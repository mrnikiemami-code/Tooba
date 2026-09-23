using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Supply.Commands.EnsureOrderSupply;

/// <summary>Id + reason length for ensure-supply command.</summary>
public sealed class EnsureOrderSupplyCommandValidator : AbstractValidator<EnsureOrderSupplyCommand>
{
    public const int MaxReasonLength = 500;

    public EnsureOrderSupplyCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ReasonRequired);
        RuleFor(x => x.Reason)
            .MaximumLength(MaxReasonLength)
            .When(x => x.Reason is not null)
            .WithErrorCode(OrderValidationCodes.ReasonTooLong);
    }
}
