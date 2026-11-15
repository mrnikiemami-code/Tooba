using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Queries.PreviewShipmentDispatchPlan;

/// <summary>Syntactic envelope for the TB-270-R2 dispatch-plan preview read.</summary>
public sealed class PreviewShipmentDispatchPlanQueryValidator : AbstractValidator<PreviewShipmentDispatchPlanQuery>
{
    public PreviewShipmentDispatchPlanQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireActorUserId(this, x => x.ActorUserId);
        RuleFor(x => x.ShipmentId)
            .NotEmpty()
            .WithErrorCode("order.shipment_plan.invalid")
            .WithMessage("مرسولهٔ هدف باید مشخص باشد.");
    }
}
