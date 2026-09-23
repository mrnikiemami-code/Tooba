using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RejectDeposit;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RejectDepositCommandValidator : AbstractValidator<RejectDepositCommand>
{
    public RejectDepositCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}