using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.AssignTracking;

/// <summary>Syntactic envelope for admin order operation command.</summary>
public sealed class AssignTrackingCommandValidator : AbstractValidator<AssignTrackingCommand>
{
    public AssignTrackingCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}