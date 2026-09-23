using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.CreateConsolidatedPackage;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class CreateConsolidatedPackageCommandValidator : AbstractValidator<CreateConsolidatedPackageCommand>
{
    public CreateConsolidatedPackageCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}