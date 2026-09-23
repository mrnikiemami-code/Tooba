using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.CancelConsolidatedPackage;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class CancelConsolidatedPackageCommandValidator : AbstractValidator<CancelConsolidatedPackageCommand>
{
    public CancelConsolidatedPackageCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}