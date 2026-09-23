using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.UnconfirmDeposit;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class UnconfirmDepositCommandValidator : AbstractValidator<UnconfirmDepositCommand>
{
    public UnconfirmDepositCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}