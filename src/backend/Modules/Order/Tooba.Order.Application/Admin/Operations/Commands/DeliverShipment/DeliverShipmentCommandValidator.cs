using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.DeliverShipment;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class DeliverShipmentCommandValidator : AbstractValidator<DeliverShipmentCommand>
{
    public DeliverShipmentCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}