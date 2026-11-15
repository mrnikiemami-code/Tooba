using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.PlanShipmentDispatch;

/// <summary>
/// Syntactic envelope for the TB-270-R2 plan command. The reviewed key, the exact parcel and the chosen customer slot
/// are all required by the server; nothing is inferred from the browser.
/// </summary>
public sealed class PlanShipmentDispatchCommandValidator : AbstractValidator<PlanShipmentDispatchCommand>
{
    public PlanShipmentDispatchCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);

        RuleFor(x => x.Request.ShipmentId)
            .NotEmpty()
            .WithErrorCode("order.shipment_plan.invalid")
            .WithMessage("مرسولهٔ هدف باید مشخص باشد.");

        RuleFor(x => x.Request.IdempotencyKey)
            .NotEmpty()
            .WithErrorCode("order.operation.invalid")
            .WithMessage("کلید بازبینی عملیات الزامی است.");

        RuleFor(x => x.Request.DispatchPlanReceivingDate)
            .NotEmpty()
            .WithErrorCode("order.shipment_plan.selection_required")
            .WithMessage("تاریخ تحویل مشتری باید انتخاب شود.");

        RuleFor(x => x.Request.DispatchPlanReceivingOpensAt)
            .NotEmpty()
            .WithErrorCode("order.shipment_plan.selection_required")
            .WithMessage("شروع نوبت تحویل باید انتخاب شود.");

        RuleFor(x => x.Request.DispatchPlanReceivingClosesAt)
            .NotEmpty()
            .WithErrorCode("order.shipment_plan.selection_required")
            .WithMessage("پایان نوبت تحویل باید انتخاب شود.");
    }
}
