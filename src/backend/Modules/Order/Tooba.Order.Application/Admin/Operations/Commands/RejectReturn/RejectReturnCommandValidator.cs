using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RejectReturn;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class RejectReturnCommandValidator : AbstractValidator<RejectReturnCommand>
{
    public RejectReturnCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}