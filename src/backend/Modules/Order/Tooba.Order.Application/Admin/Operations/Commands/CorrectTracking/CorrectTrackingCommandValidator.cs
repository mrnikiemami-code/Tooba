using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.CorrectTracking;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class CorrectTrackingCommandValidator : AbstractValidator<CorrectTrackingCommand>
{
    public CorrectTrackingCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}