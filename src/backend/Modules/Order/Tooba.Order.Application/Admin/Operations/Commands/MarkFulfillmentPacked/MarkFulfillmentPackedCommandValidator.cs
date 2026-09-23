using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.MarkFulfillmentPacked;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class MarkFulfillmentPackedCommandValidator : AbstractValidator<MarkFulfillmentPackedCommand>
{
    public MarkFulfillmentPackedCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}