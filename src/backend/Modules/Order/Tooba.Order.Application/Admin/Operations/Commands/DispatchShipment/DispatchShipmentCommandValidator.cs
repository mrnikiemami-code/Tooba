using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.DispatchShipment;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class DispatchShipmentCommandValidator : AbstractValidator<DispatchShipmentCommand>
{
    public DispatchShipmentCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}