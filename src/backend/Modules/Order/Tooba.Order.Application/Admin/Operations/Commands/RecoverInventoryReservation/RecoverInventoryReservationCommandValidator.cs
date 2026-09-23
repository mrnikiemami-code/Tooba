using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecoverInventoryReservation;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RecoverInventoryReservationCommandValidator : AbstractValidator<RecoverInventoryReservationCommand>
{
    public RecoverInventoryReservationCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}