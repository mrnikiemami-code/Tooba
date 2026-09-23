using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.ApproveReturn;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class ApproveReturnCommandValidator : AbstractValidator<ApproveReturnCommand>
{
    public ApproveReturnCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}