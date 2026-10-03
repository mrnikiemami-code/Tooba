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
    private async Task<object> RecoverInventoryReservationCoreAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        await _inventoryRecovery.RecoverAsync(checkoutId, actorUserId, request.Reason, cancellationToken);

    private void ProjectWholeOrderCancel(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        OrderAdminEffectiveAccess effective,
        bool blockedBySellerPayout)
    {
        if (!Has(effective, "order.cancel")
            || IsCheckoutCancelled(group)
            || HasDispatchedOrDelivered(fulfillments)
            || blockedBySellerPayout)
        {
            return;
        }

        var any = group.SellerOrders.Any(order =>
            order.Status != SellerOrderStatus.Cancelled
            && CanCancel(order, fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId)));
        if (!any)
        {
            return;
        }

        actions.Add(Action(
            "cancel",
            "لغو سفارش",
            "Cancel order",
            null,
            null,
            null,
            null,
            "order.cancel",
            true,
            WholeOrderCancelConfirmFa));
    }

    private void ProjectRestoreCancelledOrder(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        OrderAdminEffectiveAccess effective,
        bool blockedBySellerPayout,
        string? paymentStatus)
    {
        if (!HasAny(effective, "order.cancel", "order.handle"))
        {
            return;
        }

        if (!CanRestoreCancelledOrder(group, fulfillments, returns, blockedBySellerPayout, paymentStatus))
        {
            return;
        }

        actions.Add(Action(
            "restore_cancelled_order",
            "بازگردانی سفارش لغوشده",
            "Restore cancelled order",
            null,
            null,
            null,
            null,
            Prefer(effective, "order.cancel", "order.handle"),
            true,
            "سفارش لغوشده بازگردانی شود؟ رزرو موجودی دوباره گرفته می‌شود."));
    }

    private async Task<object> RestoreCancelledOrderCoreAsync(
        AdminOrderOpsCheckoutSnapshot group,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        _ = actorUserId;
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (!CanRestoreCancelledOrder(group, fulfillments, returns, blockedBySellerPayout, payment?.Status))
        {
            throw new AdminOrderOperationsException(
                RestoreForbiddenCode(group, fulfillments, returns, blockedBySellerPayout, payment?.Status));
        }

        var paidSellerOrderIds = group.SellerOrders
            .Where(x => x.CancelledFromStatus == SellerOrderStatus.Paid)
            .Select(x => x.SellerOrderId)
            .ToList();
        try
        {
            await _payments.RestoreAfterOrderCancelRestoreAsync(group.CheckoutId, cancellationToken);
            if (payment is not null)
            {
                await _settlement.ReinstateAccrualAfterCancelRestoreAsync(
                    payment.PaymentId,
                    sellerOrderIds,
                    cancellationToken);
            }

            // ابتدا رزرو فعلی روی OrderLine ساخته می‌شود؛ سپس Fulfillment به همان مرجع فعال بازمی‌بندد.
            await _checkout.RestoreCancelledCheckoutAsync(
                group.CheckoutId,
                new OrderAccess(null, group.PlacedByUserId),
                cancellationToken);
            await _fulfillment.ReactivateAfterOrderRestoreAsync(group.CheckoutId, cancellationToken);
            if (paidSellerOrderIds.Count > 0)
            {
                await _fulfillment.EnsureCreatedForPaidCheckoutAsync(
                    group.CheckoutId,
                    paidSellerOrderIds,
                    cancellationToken);
            }

            return new { ok = true, code = "restore_cancelled_order", checkoutId = group.CheckoutId };
        }
        catch (ContractOperationException ex) when (ex.Code == "order.restore.inventory_failed")
        {
            throw new AdminOrderOperationsException("order.restore.inventory_failed");
        }
        catch (ContractOperationException ex) when (ex.Code == "fulfillment.restore.already_dispatched")
        {
            throw new AdminOrderOperationsException("order.restore.dispatched");
        }
        catch (ContractOperationException ex) when (ex.Code is "payment.restore.refund_completed"
            or "settlement.restore.payout_completed")
        {
            var code = ex.Code == "settlement.restore.payout_completed"
                ? "order.restore.seller_payout_completed"
                : "order.restore.refund_completed";
            throw new AdminOrderOperationsException(code);
        }
        catch (ContractOperationException ex) when (ex.Code.StartsWith("order.restore.", StringComparison.Ordinal))
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }

    private async Task<object> RestoreDepositForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("payment.missing");
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var sellerOrderIds = (await LoadCheckoutAsync(checkoutId, cancellationToken))?.SellerOrders.Select(x => x.SellerOrderId).ToList()
            ?? [];
        var returns = await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        if (HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            throw new AdminOrderOperationsException("payment.restore.invalid_state");
        }

        try
        {
            return await _payments.RestoreDepositAsync(payment.PaymentId, cancellationToken);
        }
        catch (ContractOperationException ex)
        {
            throw MapPaymentRestoreFault(ex);
        }
    }

    private async Task<object> UnconfirmDepositForCheckoutAsync(
        AdminOrderOpsCheckoutSnapshot group,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("payment.missing");
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        if (HasIrreversibleFinanceBlock(fulfillments, returns))
        {
            throw new AdminOrderOperationsException("payment.unconfirm.irreversible");
        }

        if (HasStartedFulfillment(fulfillments))
        {
            throw new AdminOrderOperationsException("fulfillment.unconfirm.already_started");
        }

        if (await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken))
        {
            throw new AdminOrderOperationsException("payment.unconfirm.payout_completed");
        }

        try
        {
            await _fulfillment.VoidUnstartedForCheckoutAsync(checkoutId, cancellationToken);
            await _orderPayments.RevertVerifiedSuccessAsync(checkoutId, sellerOrderIds, cancellationToken);
            await _settlement.VoidUnpaidAccrualForPaymentAsync(payment.PaymentId, sellerOrderIds, cancellationToken);
            return await _payments.UnconfirmDepositAsync(payment.PaymentId, cancellationToken);
        }
        catch (ContractOperationException ex)
        {
            throw MapPaymentUnconfirmFault(ex);
        }
    }

    private async Task<object> ConfirmDepositForCheckoutAsync(
        AdminOrderOpsCheckoutSnapshot group,
        CancellationToken cancellationToken)
    {
        var checkoutId = group.CheckoutId;
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("payment.missing");
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();

        var supply = await _orderSupply.EnsurePaidDurableAsync(
            checkoutId,
            "confirm_deposit",
            cancellationToken);
        if (supply.Unavailable)
        {
            throw new AdminOrderOperationsException("inventory.supply.unavailable");
        }

        try
        {
            var result = await _payments.ConfirmDepositAsync(payment.PaymentId, cancellationToken);
            await _orderPayments.ApplyVerifiedSuccessAsync(
                checkoutId,
                payment.PaymentId,
                sellerOrderIds,
                cancellationToken);
            await _fulfillment.EnsureCreatedForPaidCheckoutAsync(checkoutId, sellerOrderIds, cancellationToken);
            return result;
        }
        catch (ContractOperationException ex) when (ex.Code is "inventory.manual_review.unavailable"
            or "inventory.reservation.not_active")
        {
            throw new AdminOrderOperationsException("inventory.supply.unavailable");
        }
        catch (ContractOperationException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }

    private async Task<object> RejectDepositForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("payment.missing");
        try
        {
            var result = await _payments.RejectDepositAsync(payment.PaymentId, cancellationToken);
            await _orderPayments.ReleaseReservationsAfterManualRejectAsync(checkoutId, cancellationToken);
            return result;
        }
        catch (ContractOperationException ex)
        {
            throw new AdminOrderOperationsException(ex.Code);
        }
    }

    private async Task<object> RetryRefundCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var returnRequestId = RequireReturnRequestId(request);
        return await _returns.RetryRefundAsync(returnRequestId, actorUserId, cancellationToken);
    }
}
