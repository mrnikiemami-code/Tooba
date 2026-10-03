using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// Admin order lifecycle orchestration — Host-free; Contracts + Order ports only.
/// </summary>
public sealed partial class AdminOrderOperationsOrchestrator
{
    private async Task<object> RequestReturnCoreAsync(
        AdminOrderOpsCheckoutSnapshot group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var sellerOrderId = request.SellerOrderId
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var eligibility = await _returns.EvaluateEligibilityAsync(sellerOrderId, cancellationToken);
        if (!eligibility.Eligible)
        {
            throw new AdminOrderOperationsException(ReturnEligibilityReasons.ToErrorCode(eligibility.ReasonCode));
        }

        var items = request.ReturnItems;
        if (items is null || items.Count == 0)
        {
            items = request.Selections?
                .Where(x => x.Quantity > 0)
                .Select(x => new ReturnLineCommand(x.OrderLineId, x.Quantity))
                .ToArray();
        }

        if (items is null || items.Count == 0)
        {
            items = eligibility.Lines
                .Where(x => x.RemainingReturnableQuantity > 0)
                .Select(x => new ReturnLineCommand(x.OrderLineId, x.RemainingReturnableQuantity))
                .ToArray();
        }

        if (items.Count == 0)
        {
            throw new AdminOrderOperationsException("return.quantity_exceeded");
        }

        var idempotency = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"admin-return-{sellerOrderId:N}-{Guid.NewGuid():N}"
            : request.IdempotencyKey.Trim();
        return await _returns.CreateAdminInitiatedAsync(
            new CreateAdminReturnCommand(
                sellerOrderId,
                group.PlacedByUserId,
                idempotency,
                request.Reason,
                items),
            cancellationToken);
    }

    private async Task<object> ApproveReturnCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returns.ApproveAsync(returnRequestId, actorUserId, cancellationToken);
    }

    private async Task<object> RejectReturnCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returns.RejectAsync(returnRequestId, actorUserId, request.Reason, cancellationToken);
    }

    private void ProjectPaymentActions(
        List<AdminOrderOperationAction> actions,
        PaymentAdminOperationalSnapshot? payment,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        OrderAdminEffectiveAccess effective,
        bool blockedBySellerPayout,
        string supplyStatus)
    {
        if (payment is null || !Has(effective, "payment.reconcile"))
        {
            return;
        }

        if (payment.ConfirmDepositEligible)
        {
            var confirmMessage = supplyStatus switch
            {
                "AvailableForReacquire" =>
                    "موجودی قابل تأمین است و هنگام تأیید واریز به‌صورت خودکار رزرو می‌شود.",
                "Unavailable" or "PartiallyUnavailable" =>
                    "این سفارش در حال حاضر قابل تأمین نیست.",
                _ => "آیا واریز کارت‌به‌کارت این سفارش را تأیید می‌کنید؟",
            };
            actions.Add(Action(
                "confirm_deposit",
                "تأیید واریز",
                "Confirm deposit",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                confirmMessage));
        }

        if (payment.RejectDepositEligible)
        {
            actions.Add(Action(
                "reject_deposit",
                "رد واریز",
                "Reject deposit",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "آیا از رد واریز مطمئن هستید؟"));
        }

        if (payment.RestoreDepositEligible && !HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            actions.Add(Action(
                "restore_deposit",
                "برگشت از رد واریز",
                "Undo deposit rejection",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "واریز ردشده به انتظار تأیید واریز بازگردد؟"));
        }

        if (payment.UnconfirmDepositEligible
            && !HasIrreversibleFinanceBlock(fulfillments, returns)
            && !HasStartedFulfillment(fulfillments)
            && !blockedBySellerPayout)
        {
            actions.Add(Action(
                "unconfirm_deposit",
                "برگشت از واریز",
                "Undo deposit confirmation",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "تأیید واریز این سفارش به حالت انتظار برگردد؟"));
        }
    }

    private void ProjectInventoryRecovery(
        List<AdminOrderOperationAction> actions,
        AdminOrderInventoryRecoveryAssessment recovery,
        OrderAdminEffectiveAccess effective,
        string supplyStatus,
        bool canConfirmDeposit)
    {
        if (!Has(effective, "payment.reconcile"))
        {
            return;
        }

        if (canConfirmDeposit && supplyStatus is "Reserved" or "AvailableForReacquire")
        {
            return;
        }

        if (recovery.NeedsRecovery && recovery.ClassCode is "A" or "B")
        {
            actions.Add(Action(
                "recover_inventory_reservation",
                "بازیابی رزرو موجودی",
                "Recover inventory reservation",
                null,
                null,
                null,
                null,
                "payment.reconcile",
                true,
                "رزرو موجودی این سفارش از چرخه قبلی معتبر نیست. بازیابی از موجودی فعلی انجام شود؟"));
        }
    }
}
