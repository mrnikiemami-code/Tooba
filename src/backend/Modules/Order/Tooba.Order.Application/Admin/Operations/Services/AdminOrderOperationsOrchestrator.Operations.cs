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
