using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RestoreDeposit;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RestoreDepositCommandValidator : AbstractValidator<RestoreDepositCommand>
{
    public RestoreDepositCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}