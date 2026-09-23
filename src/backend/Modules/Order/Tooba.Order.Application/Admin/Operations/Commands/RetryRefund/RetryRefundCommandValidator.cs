using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RetryRefund;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RetryRefundCommandValidator : AbstractValidator<RetryRefundCommand>
{
    public RetryRefundCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}