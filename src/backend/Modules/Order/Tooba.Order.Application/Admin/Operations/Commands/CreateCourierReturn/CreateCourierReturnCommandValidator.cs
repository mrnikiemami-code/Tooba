using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.CreateCourierReturn;

/// <summary>Syntactic envelope for the TB-265 courier-origin Returns creation command.</summary>
public sealed class CreateCourierReturnCommandValidator : AbstractValidator<CreateCourierReturnCommand>
{
    public CreateCourierReturnCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
