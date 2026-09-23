using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.PackFulfillmentSelected;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class PackFulfillmentSelectedCommandValidator : AbstractValidator<PackFulfillmentSelectedCommand>
{
    public PackFulfillmentSelectedCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}