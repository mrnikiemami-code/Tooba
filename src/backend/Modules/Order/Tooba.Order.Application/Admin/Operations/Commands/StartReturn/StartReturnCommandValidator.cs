using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.StartReturn;

/// <summary>Syntactic envelope for the TB-258 start-return command.</summary>
public sealed class StartReturnCommandValidator : AbstractValidator<StartReturnCommand>
{
    public StartReturnCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
