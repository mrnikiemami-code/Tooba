using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Commands.RecoverOrderInventoryReservation;

/// <summary>Id + optional reason length for inventory recovery command.</summary>
public sealed class RecoverOrderInventoryReservationCommandValidator
    : AbstractValidator<RecoverOrderInventoryReservationCommand>
{
    public const int MaxReasonLength = 500;

    public RecoverOrderInventoryReservationCommandValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.ActorUserId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.ActorUserIdRequired);
        RuleFor(x => x.Reason!)
            .MaximumLength(MaxReasonLength)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithErrorCode(OrderValidationCodes.ReasonTooLong);
    }
}
