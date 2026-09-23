using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.DeliverConsolidatedPackage;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class DeliverConsolidatedPackageCommandValidator : AbstractValidator<DeliverConsolidatedPackageCommand>
{
    public DeliverConsolidatedPackageCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}