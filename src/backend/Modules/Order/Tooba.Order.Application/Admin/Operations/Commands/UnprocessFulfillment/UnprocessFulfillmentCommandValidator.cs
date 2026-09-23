using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.UnprocessFulfillment;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class UnprocessFulfillmentCommandValidator : AbstractValidator<UnprocessFulfillmentCommand>
{
    public UnprocessFulfillmentCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}