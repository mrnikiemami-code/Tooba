using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.DispatchConsolidatedPackage;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class DispatchConsolidatedPackageCommandValidator : AbstractValidator<DispatchConsolidatedPackageCommand>
{
    public DispatchConsolidatedPackageCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}