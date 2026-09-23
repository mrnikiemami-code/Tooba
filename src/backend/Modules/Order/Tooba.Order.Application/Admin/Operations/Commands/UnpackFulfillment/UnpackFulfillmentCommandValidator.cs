using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.UnpackFulfillment;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class UnpackFulfillmentCommandValidator : AbstractValidator<UnpackFulfillmentCommand>
{
    public UnpackFulfillmentCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}