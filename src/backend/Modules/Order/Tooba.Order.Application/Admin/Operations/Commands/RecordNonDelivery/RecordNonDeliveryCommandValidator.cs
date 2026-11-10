using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.RecordNonDelivery;

/// <summary>Syntactic envelope for the TB-258 non-delivery command.</summary>
public sealed class RecordNonDeliveryCommandValidator : AbstractValidator<RecordNonDeliveryCommand>
{
    public RecordNonDeliveryCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
