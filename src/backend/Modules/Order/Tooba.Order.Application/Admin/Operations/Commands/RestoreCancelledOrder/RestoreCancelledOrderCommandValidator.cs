using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RestoreCancelledOrder;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RestoreCancelledOrderCommandValidator : AbstractValidator<RestoreCancelledOrderCommand>
{
    public RestoreCancelledOrderCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}