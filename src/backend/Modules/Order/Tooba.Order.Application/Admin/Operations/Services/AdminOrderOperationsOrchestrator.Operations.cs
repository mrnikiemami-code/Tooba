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
    // --- Early-exit ops (projection may hide; domain remains authoritative) ---

    public Task<Result<object>> CancelOrderAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(async () =>
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
            EnsureCancelledDoesNotBlock(group, "cancel");

            if (!Has(effective, "order.cancel"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }

            try
            {
                return await CancelAsync(group, request, cancellationToken);
            }
            catch (AdminOrderOperationsException)
            {
                throw;
            }
            catch (ContractOperationException ex) when (
                ex.Code.StartsWith("order.cancel.forbidden", StringComparison.Ordinal)
                || ex.Code == "fulfillment.cancel.already_dispatched")
            {
                throw new AdminOrderOperationsException("order.cancel.forbidden");
            }
            catch (ContractOperationException ex) when (ex.Code == "settlement.cancel.payout_completed")
            {
                throw new AdminOrderOperationsException("order.cancel.payout_completed");
            }
        });

    public Task<Result<object>> RestoreDepositAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(async () =>
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
            EnsureCancelledDoesNotBlock(group, "restore_deposit");

            if (!Has(effective, "payment.reconcile"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }

            try
            {
                return await RestoreDepositForCheckoutAsync(checkoutId, cancellationToken);
            }
            catch (AdminOrderOperationsException)
            {
                throw;
            }
            catch (ContractOperationException ex)
            {
                throw MapPaymentRestoreFault(ex);
            }
        });

    public Task<Result<object>> UnconfirmDepositAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(async () =>
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
            EnsureCancelledDoesNotBlock(group, "unconfirm_deposit");

            if (!Has(effective, "payment.reconcile"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }

            try
            {
                return await UnconfirmDepositForCheckoutAsync(group, cancellationToken);
            }
            catch (AdminOrderOperationsException)
            {
                throw;
            }
            catch (ContractOperationException ex)
            {
                throw MapPaymentUnconfirmFault(ex);
            }
        });

    public Task<Result<object>> RestoreCancelledOrderAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(async () =>
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
            EnsureCancelledDoesNotBlock(group, "restore_cancelled_order");

            if (!HasAny(effective, "order.cancel", "order.handle"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }

            return await RestoreCancelledOrderCoreAsync(group, actorUserId, cancellationToken);
        });

    public Task<Result<object>> RecoverInventoryReservationAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(async () =>
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
            EnsureCancelledDoesNotBlock(group, "recover_inventory_reservation");

            if (!Has(effective, "payment.reconcile"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }

            return await RecoverInventoryReservationCoreAsync(checkoutId, actorUserId, request, cancellationToken);
        });

    // --- Projection-gated ops ---

    public Task<Result<object>> MarkProcessingAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "mark_processing",
            _ => MarkProcessingCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> MarkPackedAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "mark_packed",
            _ => MarkPackedCoreAsync(request, actorUserId, cancellationToken, requireSelections: false),
            cancellationToken));

    public Task<Result<object>> PackSelectedAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "pack_selected",
            _ => MarkPackedCoreAsync(request, actorUserId, cancellationToken, requireSelections: true),
            cancellationToken));

    public Task<Result<object>> UnprocessAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "unprocess",
            _ => UnprocessCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> UnpackAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "unpack",
            _ => UnpackCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> CreateShipmentAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "create_shipment",
            _ => CreateShipmentCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> CancelShipmentAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "cancel_shipment",
            _ => CancelShipmentCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> AssignTrackingAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "assign_tracking",
            _ => AssignTrackingCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> CorrectTrackingAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "correct_tracking",
            _ => CorrectTrackingCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> DispatchShipmentAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "dispatch_shipment",
            _ => DispatchCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> DeliverShipmentAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "deliver_shipment",
            _ => DeliverCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    // --- TB-258: post-dispatch non-delivery / physical return / quantity-safe resend ---

    public Task<Result<object>> RecordNonDeliveryAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunPostDispatchExceptionAsync(
            checkoutId,
            actorUserId,
            request,
            "record_non_delivery",
            _ => RecordNonDeliveryCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> StartReturnAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunPostDispatchExceptionAsync(
            checkoutId,
            actorUserId,
            request,
            "start_return",
            _ => StartReturnCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> RecordReturnReceiptAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunPostDispatchExceptionAsync(
            checkoutId,
            actorUserId,
            request,
            "record_return_receipt",
            _ => RecordReturnReceiptCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> ResendShipmentAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunPostDispatchExceptionAsync(
            checkoutId,
            actorUserId,
            request,
            "resend_shipment",
            _ => ResendShipmentCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> ResolveDamagedGoodsAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunDamagedResolutionAsync(
            checkoutId,
            actorUserId,
            request,
            cancellationToken));

    /// <summary>
    /// TB-265: creates the third lawful outcome of a courier-return receipt — a Returns case linked to the EXISTING
    /// Returns review/refund/disposition workflow, beside the healthy same-goods resend and the newly reserved healthy
    /// replacement. The received quantity is claimed in Fulfillment BEFORE the case exists, so the same units can never
    /// fund two branches.
    /// </summary>
    public Task<Result<object>> CreateCourierReturnAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunCourierReturnAsync(
            checkoutId,
            actorUserId,
            request,
            cancellationToken));

    public Task<Result<object>> CreateConsolidatedPackageAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "create_consolidated_package",
            _ => CreateConsolidatedPackageCoreAsync(checkoutId, request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> PrepareOperationalGroupAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunOperationalGroupAsync(checkoutId, actorUserId, request, cancellationToken));

    /// <summary>
    /// TB-259: exact recorded-occurrence reconciliation BEFORE the fresh readiness projection.
    /// </summary>
    /// <remarks>
    /// The workflow re-checks the current actor, the checkout and the exact group/envelope itself, so this
    /// seam can never be used as an authority bypass. A first attempt, a new occurrence or any changed
    /// request still passes the ordinary projected-action gate.
    /// </remarks>
    private async Task<object> RunOperationalGroupAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        EnsureCancelledDoesNotBlock(group, "prepare_operational_group");

        var reconciled = await _operationalGroupWorkflow.TryReconcileRecordedAsync(
            checkoutId,
            request,
            actorUserId,
            cancellationToken);
        if (reconciled is not null)
        {
            return reconciled;
        }

        return await RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "prepare_operational_group",
            _ => PrepareOperationalGroupCoreAsync(checkoutId, request, actorUserId, cancellationToken),
            cancellationToken);
    }

    public Task<Result<object>> CancelConsolidatedPackageAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "cancel_consolidated_package",
            _ => CancelConsolidatedPackageCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> AssignConsolidatedPackageTrackingAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "assign_consolidated_package_tracking",
            _ => AssignConsolidatedPackageTrackingCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> DispatchConsolidatedPackageAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "dispatch_consolidated_package",
            _ => DispatchConsolidatedPackageCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> DeliverConsolidatedPackageAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "deliver_consolidated_package",
            _ => DeliverConsolidatedPackageCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken));

    public Task<Result<object>> RequestReturnAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "request_return",
            group => RequestReturnCoreAsync(group, request, cancellationToken),
            cancellationToken,
            allowReturnLifecycleFallback: true));

    public Task<Result<object>> ApproveReturnAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "approve_return",
            _ => ApproveReturnCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken,
            allowReturnLifecycleFallback: true));

    public Task<Result<object>> RejectReturnAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "reject_return",
            _ => RejectReturnCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken,
            allowReturnLifecycleFallback: true));

    public Task<Result<object>> RetryRefundAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "retry_refund",
            _ => RetryRefundCoreAsync(request, actorUserId, cancellationToken),
            cancellationToken,
            allowReturnLifecycleFallback: true));

    public Task<Result<object>> ConfirmDepositAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "confirm_deposit",
            group => ConfirmDepositForCheckoutAsync(group, cancellationToken),
            cancellationToken));

    public Task<Result<object>> RejectDepositAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken) =>
        RunOperationAsync(() => RunProjectedCoreAsync(
            checkoutId,
            actorUserId,
            request,
            "reject_deposit",
            _ => RejectDepositForCheckoutAsync(checkoutId, cancellationToken),
            cancellationToken));
}
