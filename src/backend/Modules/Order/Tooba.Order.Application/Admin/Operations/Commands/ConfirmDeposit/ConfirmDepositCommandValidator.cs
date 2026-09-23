using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class ConfirmDepositCommandValidator : AbstractValidator<ConfirmDepositCommand>
{
    public ConfirmDepositCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}