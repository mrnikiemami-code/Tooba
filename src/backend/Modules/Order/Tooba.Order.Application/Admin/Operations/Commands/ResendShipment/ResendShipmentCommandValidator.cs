using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.ResendShipment;

/// <summary>Syntactic envelope for the TB-258 resend command.</summary>
public sealed class ResendShipmentCommandValidator : AbstractValidator<ResendShipmentCommand>
{
    public ResendShipmentCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
