using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Operations.Services;

/// <summary>
/// Admin order lifecycle orchestration — Host-free; Contracts + Order ports only.
/// </summary>
public sealed class AdminOrderOperationsOrchestrator
{

    private static readonly HashSet<string> OpsFamilyPrefixes =
    [
        "order.",
        "return.",
        "fulfillment.",
        "refund.",
        "payment.",
    ];

    public static readonly HashSet<string> CancelledBlockedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "confirm_deposit",
        "reject_deposit",
        "restore_deposit",
        "unconfirm_deposit",
        "mark_processing",
        "mark_packed",
        "pack_selected",
        "unprocess",
        "unpack",
        "create_shipment",
        "cancel_shipment",
        "assign_tracking",
        "correct_tracking",
        "dispatch_shipment",
        "deliver_shipment",
        "create_consolidated_package",
        "cancel_consolidated_package",
        "assign_consolidated_package_tracking",
        "dispatch_consolidated_package",
        "deliver_consolidated_package",
    };

    public const string WholeOrderCancelBlockedAfterDispatchFa =
        "پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.";

    public const string WholeOrderCancelConfirmFa =
        "با لغو کامل سفارش، مرسوله‌های پیش از ارسال ابطال می‌شوند، موجودی آزاد می‌شود و در صورت پرداخت موفق بازگشت وجه آغاز می‌شود. آیا مطمئن هستید؟";

    private readonly IAdminOrderOperationsCheckoutReader _checkouts;
    
    private readonly IFulfillmentAdminOperations _fulfillment;
    private readonly IReturnAdminOperations _returns;
    
    private readonly ICheckoutDirectory _checkout;
    private readonly IOrderAdminEffectiveAccessReader _access;
    
    private readonly IPaymentAdminGateway _payments;
    private readonly IOrderPaymentProjectionPort _orderPayments;
    private readonly ISettlementOrderAccrualPort _settlement;
    private readonly IAdminOrderOperationsInventoryRecoveryPort _inventoryRecovery;
    private readonly IAdminOrderOperationsSupplyPort _orderSupply;
    

    /// <summary>ترکیب‌گر عملیات را به ماژول‌های موجود وصل می‌کند.</summary>
    public AdminOrderOperationsOrchestrator(
        IAdminOrderOperationsCheckoutReader checkouts,
        IFulfillmentAdminOperations fulfillment,
        IReturnAdminOperations returns,
        
        ICheckoutDirectory checkout,
        IOrderAdminEffectiveAccessReader access,
        
        IPaymentAdminGateway payments,
        IOrderPaymentProjectionPort orderPayments,
        ISettlementOrderAccrualPort settlement,
        IAdminOrderOperationsInventoryRecoveryPort inventoryRecovery,
        IAdminOrderOperationsSupplyPort orderSupply)
    {
        _checkouts = checkouts;
        _fulfillment = fulfillment;
        _returns = returns;
        
        _checkout = checkout;
        _access = access;
        
        _payments = payments;
        _orderPayments = orderPayments;
        _settlement = settlement;
        _inventoryRecovery = inventoryRecovery;
        _orderSupply = orderSupply;
        
    }

    /// <summary>اقدامات مجاز و eligibility مرجوعی یک checkout را برمی‌گرداند.</summary>
    public async Task<Result<AdminOrderOperationsPage>> ListAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.Success(await ListCoreAsync(checkoutId, actorUserId, cancellationToken));
        }
        catch (AdminOrderOperationsException ex)
        {
            return Result.Failure<AdminOrderOperationsPage>(new SemanticError(ex.Code));
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<AdminOrderOperationsPage>(new SemanticError(ex.Code));
        }
    }

    private async Task<AdminOrderOperationsPage> ListCoreAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");

        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        var fulfillments = await _fulfillment.ListForCheckoutAsync(checkoutId, cancellationToken);
        var packages = await _fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
        var allShipmentIds = fulfillments.SelectMany(f => f.Shipments.Select(s => s.ShipmentId)).Distinct().ToArray();
        var memberships = await _fulfillment.GetActiveMembershipByShipmentIdsAsync(allShipmentIds, cancellationToken);
        var membershipByShipment = memberships.ToDictionary(x => x.ShipmentId);
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = (await _returns.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken))
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToList();

        var eligibility = new List<ReturnEligibilityResult>();
        var actions = new List<AdminOrderOperationAction>();
        var payment = await _payments.GetLatestOperationalForCheckoutAsync(checkoutId, cancellationToken);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        var supply = await _orderSupply.GetStatusAsync(checkoutId, cancellationToken);
        if (!IsCheckoutCancelled(group))
        {
            ProjectPaymentActions(actions, payment, fulfillments, returns, effective, blockedBySellerPayout, supply.Status);
        }
        foreach (var order in group.SellerOrders)
        {
            var elig = await _returns.EvaluateEligibilityAsync(order.SellerOrderId, cancellationToken);
            eligibility.Add(elig);
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            ProjectActions(actions, order, fulfillment, returns, elig, effective, membershipByShipment);
        }

        ProjectWholeOrderCancel(actions, group, fulfillments, effective, blockedBySellerPayout);
        ProjectRestoreCancelledOrder(actions, group, fulfillments, returns, effective, blockedBySellerPayout, payment?.Status);
        ProjectConsolidatedPackageActions(actions, group, fulfillments, packages, membershipByShipment, effective);
        var recovery = await _inventoryRecovery.AssessAsync(checkoutId, cancellationToken);
        var canConfirmDeposit = payment is { ConfirmDepositEligible: true } && Has(effective, "payment.reconcile");
        ProjectInventoryRecovery(actions, recovery, effective, supply.Status, canConfirmDeposit);
        var collapsed = AdminOrderWholeOrderActions.Collapse(actions);
        var lineCaps = new List<AdminOrderLineCapability>();
        var sellerCaps = new List<AdminSellerCapability>();
        foreach (var order in group.SellerOrders)
        {
            var fulfillment = fulfillments.FirstOrDefault(x => x.SellerOrderId == order.SellerOrderId);
            var projected = AdminFulfillmentCapabilityProjector.Project(order, fulfillment, collapsed);
            if (recovery.NeedsRecovery || recovery.ClassCode is "C")
            {
                sellerCaps.Add(projected.Seller with
                {
                    SelectionAllowed = false,
                    PaymentLocked = true,
                    InfoMessageFa = "رزرو موجودی این سفارش از چرخه قبلی معتبر نیست و نیاز به بازیابی موجودی دارد.",
                    ShipmentCreationPossible = false,
                });
                foreach (var line in projected.Lines)
                {
                    lineCaps.Add(line with
                    {
                        Selectable = false,
                        LockedReasonCode = "inventory.recovery.required",
                        LockedReasonFa = "رزرو موجودی این سفارش از چرخه قبلی معتبر نیست و نیاز به بازیابی موجودی دارد.",
                    });
                }
            }
            else
            {
                lineCaps.AddRange(projected.Lines);
                sellerCaps.Add(projected.Seller);
            }
        }

        var warning = recovery.NeedsRecovery || recovery.ClassCode is "C"
            ? "رزرو موجودی این سفارش از چرخه قبلی معتبر نیست و نیاز به بازیابی موجودی دارد."
            : null;
        var canRecover = collapsed.Any(a => a.Code == "recover_inventory_reservation");
        var shortage = supply.Status is "Unavailable" or "PartiallyUnavailable"
            ? supply.Lines
                .Where(x => x.Shortage > 0 || x.LineStatus is "Unavailable" or "PartiallyUnavailable")
                .Select(x => new AdminSupplyLineShortage(x.ItemTitle, x.UnitCode, x.Required, x.Available, x.Shortage))
                .ToList()
            : [];
        return new AdminOrderOperationsPage(
            checkoutId,
            collapsed,
            eligibility,
            lineCaps,
            sellerCaps,
            warning,
            recovery.ClassCode is "A" or "B" or "C" ? recovery.ClassCode : null,
            supply.Status,
            supply.MessageFa,
            canConfirmDeposit,
            canRecover,
            shortage);
    }

    /// <summary>eligibility همهٔ سفارش‌های فروشندهٔ یک checkout.</summary>
    public async Task<Result<IReadOnlyList<ReturnEligibilityResult>>> ListReturnEligibilityAsync(
        Guid checkoutId,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
                ?? throw new AdminOrderOperationsException("order.operation.invalid");
            var results = new List<ReturnEligibilityResult>();
            foreach (var order in group.SellerOrders)
            {
                results.Add(await _returns.EvaluateEligibilityAsync(order.SellerOrderId, cancellationToken));
            }

            return Result.Success<IReadOnlyList<ReturnEligibilityResult>>(results);
        }
        catch (AdminOrderOperationsException ex)
        {
            return Result.Failure<IReadOnlyList<ReturnEligibilityResult>>(new SemanticError(ex.Code));
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<IReadOnlyList<ReturnEligibilityResult>>(new SemanticError(ex.Code));
        }
    }

    /// <summary>یک عملیات را پس از بررسی مجوز اجرا می‌کند.</summary>
    private static async Task<Result<object>> RunOperationAsync(Func<Task<object>> execute)
    {
        try
        {
            return Result.Success(await execute());
        }
        catch (AdminOrderOperationsException ex)
        {
            return Result.Failure<object>(new SemanticError(ex.Code));
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<object>(new SemanticError(ex.Code));
        }
    }

    private void EnsureCancelledDoesNotBlock(
        AdminOrderOpsCheckoutSnapshot group,
        string expectedCode)
    {
        if (IsCheckoutCancelled(group) && CancelledBlockedCodes.Contains(expectedCode))
        {
            throw new AdminOrderOperationsException("order.cancelled.blocks_action");
        }
    }

    private async Task EnsureProjectedActionAllowedAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        string expectedCode,
        OrderAdminEffectiveAccess effective,
        bool allowReturnLifecycleFallback,
        CancellationToken cancellationToken)
    {
        var page = await ListCoreAsync(checkoutId, actorUserId, cancellationToken);
        var projected = page.Actions.FirstOrDefault(a =>
            string.Equals(a.Code, expectedCode, StringComparison.OrdinalIgnoreCase)
            && MatchesIds(a, request));
        if (projected is null)
        {
            if (!allowReturnLifecycleFallback)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            if (!Has(effective, "return.manage"))
            {
                throw new AdminOrderOperationsException("order.operation.denied");
            }
        }
        else if (!Has(effective, projected.RequiredPermission))
        {
            throw new AdminOrderOperationsException("order.operation.denied");
        }
    }

    private async Task<object> RunProjectedCoreAsync(
        Guid checkoutId,
        Guid actorUserId,
        AdminOrderOperationRequest request,
        string expectedCode,
        Func<AdminOrderOpsCheckoutSnapshot, Task<object>> execute,
        CancellationToken cancellationToken,
        bool allowReturnLifecycleFallback = false)
    {
        var group = await LoadCheckoutAsync(checkoutId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var effective = await LoadEffectiveAsync(actorUserId, cancellationToken);
        EnsureCancelledDoesNotBlock(group, expectedCode);

        await EnsureProjectedActionAllowedAsync(
            checkoutId,
            actorUserId,
            request,
            expectedCode,
            effective,
            allowReturnLifecycleFallback,
            cancellationToken);

        try
        {
            return await execute(group);
        }
        catch (AdminOrderOperationsException)
        {
            throw;
        }
        catch (ContractOperationException ex)
        {
            var code = ex.Code == "fulfillment.cancel.already_dispatched"
                ? "fulfillment.dispatch.already_dispatched"
                : ex.Code;
            throw new AdminOrderOperationsException(code);
        }
    }

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

    private void ProjectActions(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsSellerOrderSnapshot order,
        FulfillmentSnapshot? fulfillment,
        IReadOnlyList<ReturnSnapshot> returns,
        ReturnEligibilityResult eligibility,
        OrderAdminEffectiveAccess effective,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment)
    {
        if (order.Status != SellerOrderStatus.Cancelled && fulfillment is not null)
        {
            if (HasProcessableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_processing",
                    "شروع پردازش",
                    "Mark processing",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "شروع پردازش این سفارش؟"));
                foreach (var item in fulfillment.Items.Where(x => x.QuantityOrdered > x.QuantityProcessing))
                {
                    actions.Add(Action(
                        "mark_processing",
                        "شروع پردازش این قلم",
                        "Start processing this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "پردازش این قلم شروع شود؟",
                        item.OrderLineId));
                }
            }

            if (HasPackableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "mark_packed",
                    "بسته‌بندی همه اقلام آماده",
                    "Pack all eligible",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "همه اقلام آماده این فروشنده بسته‌بندی شود؟"));
                actions.Add(Action(
                    "pack_selected",
                    "بسته‌بندی انتخاب‌شده‌ها",
                    "Pack selected",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "اقلام انتخاب‌شده بسته‌بندی شود؟"));
                foreach (var item in fulfillment.Items.Where(x => x.QuantityProcessing > x.QuantityPacked))
                {
                    actions.Add(Action(
                        "pack_selected",
                        "بسته‌بندی این قلم",
                        "Pack this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "این قلم بسته‌بندی شود؟",
                        item.OrderLineId));
                    actions.Add(Action(
                        "unprocess",
                        "برگشت از پردازش این قلم",
                        "Unprocess this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "برگشت از پردازش این قلم؟",
                        item.OrderLineId));
                }

                actions.Add(Action(
                    "unprocess",
                    "برگشت از پردازش",
                    "Unprocess",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "برگشت از پردازش برای اقلام بسته‌بندی‌نشده؟"));
            }

            if (HasUnpackableQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "unpack",
                    "بازگشت از بسته‌بندی",
                    "Unpack",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "بازگشت از بسته‌بندی برای اقلام تخصیص‌نشده؟"));
                foreach (var item in fulfillment.Items)
                {
                    var blocking = OpenAllocated(fulfillment, item.OrderLineId) + item.QuantityShipped;
                    if (item.QuantityPacked <= blocking)
                    {
                        continue;
                    }

                    actions.Add(Action(
                        "unpack",
                        "بازگشت از بسته‌بندی",
                        "Unpack this line",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        null,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "بازگشت از بسته‌بندی این قلم؟",
                        item.OrderLineId));
                }
            }

            if (fulfillment.Status is not (FulfillmentOperationStatus.Cancelled or FulfillmentOperationStatus.Failed or FulfillmentOperationStatus.Delivered)
                && HasUnallocatedShipmentQuantity(fulfillment)
                && HasAny(effective, "order.handle", "fulfillment.manage"))
            {
                actions.Add(Action(
                    "create_shipment",
                    "ایجاد مرسوله",
                    "Create shipment",
                    order.SellerOrderId,
                    fulfillment.FulfillmentId,
                    null,
                    null,
                    Prefer(effective, "order.handle", "fulfillment.manage"),
                    true,
                    "مرسوله برای این سفارش ایجاد شود؟"));
            }

            foreach (var shipment in fulfillment.Shipments.Where(s => s.Status != ShipmentOperationStatus.Cancelled))
            {
                var lockedByPackage = membershipByShipment.TryGetValue(shipment.ShipmentId, out var membership)
                    && membership.PackageStatus is ConsolidatedPackageOperationStatus.Created or ConsolidatedPackageOperationStatus.Dispatched;

                if (shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "cancel_shipment",
                        "ابطال مرسوله",
                        "Cancel shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "مرسوله ابطال شود؟ تخصیص آزاد می‌شود و کد رهگیری در تاریخچه می‌ماند."));
                }

                if (string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "assign_tracking",
                        "ثبت کد رهگیری",
                        "Assign tracking",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "کد رهگیری برای مرسوله ثبت شود؟"));
                }

                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.DispatchedAt is null
                    && shipment.Status == ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "correct_tracking",
                        "اصلاح کد رهگیری",
                        "Correct tracking",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "کد رهگیری مرسوله اصلاح شود؟ مقدار قبلی در تاریخچه می‌ماند."));
                }

                if (!string.IsNullOrWhiteSpace(shipment.TrackingReference)
                    && shipment.DispatchedAt is null
                    && shipment.Status is ShipmentOperationStatus.Created
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                    "dispatch_shipment",
                    "ارسال مرسوله",
                    "Dispatch shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "ارسال مرسوله ثبت شود؟"));
                }

                if (shipment.Status is ShipmentOperationStatus.Dispatched or ShipmentOperationStatus.InTransit
                    && shipment.DeliveredAt is null
                    && !lockedByPackage
                    && HasAny(effective, "order.handle", "fulfillment.manage"))
                {
                    actions.Add(Action(
                        "deliver_shipment",
                        "ثبت تحویل",
                        "Deliver shipment",
                        order.SellerOrderId,
                        fulfillment.FulfillmentId,
                        shipment.ShipmentId,
                        null,
                        Prefer(effective, "order.handle", "fulfillment.manage"),
                        true,
                        "تحویل مرسوله ثبت شود؟"));
                }
            }
        }

        if (eligibility.Eligible && Has(effective, "return.manage"))
        {
            actions.Add(Action(
                "request_return",
                "درخواست مرجوعی",
                "Request return",
                order.SellerOrderId,
                fulfillment?.FulfillmentId,
                null,
                null,
                "return.manage",
                true,
                "درخواست مرجوعی برای این سفارش ثبت شود؟"));
        }

        foreach (var ret in returns.Where(x => x.SellerOrderId == order.SellerOrderId))
        {
            if (ret.Status == ReturnRequestOperationStatus.Requested && Has(effective, "return.manage"))
            {
                actions.Add(Action(
                    "approve_return",
                    "تأیید مرجوعی",
                    "Approve return",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    "return.manage",
                    true,
                    "مرجوعی تأیید و بازگشت وجه آغاز شود؟"));
                actions.Add(Action(
                    "reject_return",
                    "رد مرجوعی",
                    "Reject return",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    "return.manage",
                    true,
                    "مرجوعی رد شود؟"));
            }

            if (ret.Status == ReturnRequestOperationStatus.RefundFailed
                && HasAny(effective, "order.refund", "return.manage"))
            {
                actions.Add(Action(
                    "retry_refund",
                    "تلاش مجدد برای بازگشت وجه",
                    "Retry refund",
                    order.SellerOrderId,
                    null,
                    null,
                    ret.ReturnRequestId,
                    Prefer(effective, "order.refund", "return.manage"),
                    true,
                    "بازگشت وجه دوباره تلاش شود؟"));
            }
        }
    }

    private void ProjectConsolidatedPackageActions(
        List<AdminOrderOperationAction> actions,
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ConsolidatedPackageSnapshot> packages,
        IReadOnlyDictionary<Guid, ActivePackageMembershipSnapshot> membershipByShipment,
        OrderAdminEffectiveAccess effective)
    {
        if (IsCheckoutCancelled(group))
        {
            return;
        }

        var sellerCount = group.SellerOrders.Select(x => x.SellerPartyId).Distinct().Count();
        if (sellerCount < 2 || !HasAny(effective, "order.handle", "fulfillment.manage"))
        {
            return;
        }

        var permission = Prefer(effective, "order.handle", "fulfillment.manage");
        var eligibleByMethod = fulfillments
            .SelectMany(f => f.Shipments
                .Where(s => s.Status == ShipmentOperationStatus.Created
                    && s.DispatchedAt is null
                    && !string.IsNullOrWhiteSpace(s.ShippingMethodCode)
                    && !membershipByShipment.ContainsKey(s.ShipmentId))
                .Select(s => (f.SellerPartyId, Method: s.ShippingMethodCode.Trim())))
            .GroupBy(x => x.Method, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Select(x => x.SellerPartyId).Distinct().Count() >= 2);
        if (eligibleByMethod)
        {
            actions.Add(Action(
                "create_consolidated_package",
                "ایجاد بسته تجمیعی",
                "Create consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                "بسته تجمیعی برای مرسوله‌های انتخاب‌شده ایجاد شود؟"));
        }

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageOperationStatus.Created))
        {
            actions.Add(Action(
                "cancel_consolidated_package",
                "ابطال بسته تجمیعی",
                "Cancel consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"بسته {package.PackageNumber} ابطال شود؟ عضویت مرسوله‌ها آزاد می‌شود.",
                consolidatedPackageId: package.ConsolidatedPackageId));
            if (string.IsNullOrWhiteSpace(package.TrackingReference))
            {
                actions.Add(Action(
                    "assign_consolidated_package_tracking",
                    "ثبت کد رهگیری بسته",
                    "Assign consolidated package tracking",
                    null,
                    null,
                    null,
                    null,
                    permission,
                    true,
                    $"کد رهگیری مرکزی برای بسته {package.PackageNumber} ثبت شود؟",
                    consolidatedPackageId: package.ConsolidatedPackageId));
            }

            actions.Add(Action(
                "dispatch_consolidated_package",
                "ارسال بسته تجمیعی",
                "Dispatch consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"ارسال مرکزی بسته {package.PackageNumber} ثبت شود؟",
                consolidatedPackageId: package.ConsolidatedPackageId));
        }

        foreach (var package in packages.Where(p => p.Status == ConsolidatedPackageOperationStatus.Dispatched))
        {
            actions.Add(Action(
                "deliver_consolidated_package",
                "تحویل بسته تجمیعی",
                "Deliver consolidated package",
                null,
                null,
                null,
                null,
                permission,
                true,
                $"تحویل مرکزی بسته {package.PackageNumber} ثبت شود؟",
                consolidatedPackageId: package.ConsolidatedPackageId));
        }
    }

    private async Task<object> CancelAsync(
        AdminOrderOpsCheckoutSnapshot group,
        AdminOrderOperationRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (HasDispatchedOrDelivered(fulfillments))
        {
            throw new AdminOrderOperationsException("order.cancel.forbidden");
        }

        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var alreadyCancelled = IsCheckoutCancelled(group);
        var blockedBySellerPayout = await HasSellerPayoutRestoreBlockAsync(sellerOrderIds, cancellationToken);
        if (!alreadyCancelled && blockedBySellerPayout)
        {
            throw new AdminOrderOperationsException("order.cancel.payout_completed");
        }

        var access = new OrderAccess(null, group.PlacedByUserId);
        await _fulfillment.AbortForCheckoutCancelAsync(group.CheckoutId, cancellationToken);

        var cancelled = new List<Guid>();
        foreach (var order in group.SellerOrders)
        {
            if (order.Status == SellerOrderStatus.Cancelled)
            {
                continue;
            }

            await _checkout.CancelSellerOrderAsync(order.SellerOrderId, access, cancellationToken);
            cancelled.Add(order.SellerOrderId);
        }

        if (!alreadyCancelled && cancelled.Count == 0)
        {
            throw new AdminOrderOperationsException("order.cancel.forbidden");
        }

        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (payment is not null && !blockedBySellerPayout)
        {
            await _settlement.NeutralizeUnpaidAccrualForCancelAsync(
                payment.PaymentId,
                sellerOrderIds,
                cancellationToken);
        }

        await _payments.CloseOrStartRefundForOrderCancelAsync(group.CheckoutId, cancellationToken);
        return new
        {
            ok = true,
            code = "cancel",
            checkoutId = group.CheckoutId,
            sellerOrderIds = alreadyCancelled ? sellerOrderIds : cancelled,
        };
    }

    private async Task<object> MarkProcessingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        if (request.Selections is not { Count: > 0 })
        {
            return await _fulfillment.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (!SelectionsAreHomogeneousProcessable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveProcessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.ProcessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnprocessCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnprocessable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnprocessSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.UnprocessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> MarkPackedCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken,
        bool requireSelections)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (snapshot.Status == FulfillmentOperationStatus.ReadyToFulfill)
        {
            throw new AdminOrderOperationsException("fulfillment.pack.requires_processing");
        }

        if (requireSelections && (request.Selections is null || request.Selections.Count == 0))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        if (requireSelections && !SelectionsAreHomogeneousPackable(snapshot, request.Selections!))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolvePackSelections(snapshot, requireSelections ? request.Selections : request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> UnpackCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        if (request.Selections is { Count: > 0 } && !SelectionsAreHomogeneousUnpackable(snapshot, request.Selections))
        {
            throw new AdminOrderOperationsException("fulfillment.bulk.incompatible");
        }

        var selections = ResolveUnpackSelections(snapshot, request.Selections);
        if (selections.Count == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.UnpackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    }

    private async Task<object> CreateShipmentCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var methodCode = request.ShippingMethodCode?.Trim();
        var carrier = request.CarrierDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(carrier))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var enabled = _fulfillment.ListEnabledShippingMethods().Any(x =>
                string.Equals(x.Code, methodCode, StringComparison.OrdinalIgnoreCase));
            if (!enabled)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            carrier = _fulfillment.ResolveShippingMethodLabel(methodCode, carrier);
        }

        var snapshot = await _fulfillment.GetAsync(fulfillmentId, cancellationToken)
            ?? throw new AdminOrderOperationsException("order.operation.invalid");
        var lines = ResolveShipmentSelections(snapshot, request.Selections);
        if (lines.Length == 0)
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.CreateShipmentAsync(
            fulfillmentId,
            actorUserId,
            carrier!,
            lines,
            methodCode,
            request.ProviderMetadataJson,
            cancellationToken);
    }

    private async Task<object> CancelShipmentCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.AssignTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task<object> CorrectTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("order.operation.invalid");
        }

        return await _fulfillment.CorrectTrackingAsync(
            fulfillmentId,
            shipmentId,
            actorUserId,
            request.TrackingReference.Trim(),
            cancellationToken);
    }

    private async Task<object> DispatchCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var fulfillmentId = RequireFulfillmentId(request);
        var shipmentId = RequireShipmentId(request);
        return await _fulfillment.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    }

    private async Task<object> CreateConsolidatedPackageCoreAsync(
        Guid checkoutId,
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var shipmentIds = request.ShipmentIds;
        if (shipmentIds is null || shipmentIds.Count == 0)
        {
            throw new AdminOrderOperationsException("fulfillment.package.requires_multi_seller");
        }

        var methodCode = request.ShippingMethodCode?.Trim();

        return await _fulfillment.CreateConsolidatedPackageAsync(
            checkoutId,
            shipmentIds,
            string.IsNullOrWhiteSpace(methodCode) ? null : methodCode,
            request.TrackingReference,
            request.Reason,
            actorUserId,
            cancellationToken);
    }

    private async Task<object> CancelConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.CancelConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> AssignConsolidatedPackageTrackingCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        if (string.IsNullOrWhiteSpace(request.TrackingReference))
        {
            throw new AdminOrderOperationsException("fulfillment.package.tracking_required");
        }

        return await _fulfillment.AssignConsolidatedPackageTrackingAsync(
            packageId,
            request.TrackingReference.Trim(),
            actorUserId,
            cancellationToken);
    }

    private async Task<object> DispatchConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DispatchConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

    private async Task<object> DeliverConsolidatedPackageCoreAsync(
        AdminOrderOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var packageId = RequireConsolidatedPackageId(request);
        return await _fulfillment.DeliverConsolidatedPackageAsync(packageId, actorUserId, cancellationToken);
    }

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

    private Task<AdminOrderOpsCheckoutSnapshot?> LoadCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        _checkouts.GetAsync(checkoutId, cancellationToken);

    private Task<OrderAdminEffectiveAccess> LoadEffectiveAsync(Guid actorUserId, CancellationToken cancellationToken) =>
        _access.GetAsync(actorUserId, cancellationToken);

    /// <summary>
    /// اگر کاربر مجوز granular از خانواده‌های order/return/fulfillment دارد، همان را الزام می‌کند؛
    /// در غیر این صورت admin قدیمی (فقط tenant#view) همهٔ ops را می‌بیند.
    /// </summary>
    public static bool Has(OrderAdminEffectiveAccess effective, string permissionId)
    {
        var grants = effective.Permissions.Where(p => !p.DeniedByCeiling).ToList();
        var hasOpsFamily = grants.Any(p => OpsFamilyPrefixes.Any(prefix =>
            p.PermissionId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        if (!hasOpsFamily)
        {
            return true;
        }

        return grants.Any(p => string.Equals(p.PermissionId, permissionId, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAny(OrderAdminEffectiveAccess effective, params string[] permissionIds) =>
        permissionIds.Any(p => Has(effective, p));

    public static string Prefer(OrderAdminEffectiveAccess effective, params string[] permissionIds)
    {
        foreach (var permissionId in permissionIds)
        {
            if (Has(effective, permissionId))
            {
                return permissionId;
            }
        }

        return permissionIds[0];
    }

    public static bool CanCancel(AdminOrderOpsSellerOrderSnapshot order, FulfillmentSnapshot? fulfillment)
    {
        SellerOrderCancelFulfillmentSnapshot? gate = fulfillment is null
            ? null
            : new SellerOrderCancelFulfillmentSnapshot(
                fulfillment.Status.ToString(),
                fulfillment.Shipments.Count,
                HasDispatchedQuantity(fulfillment));
        return SellerOrderCancellationPolicy.CanCancel(order.Status, gate);
    }

    public static bool IsCheckoutCancelled(AdminOrderOpsCheckoutSnapshot group) =>
        group.SellerOrders.Count > 0
        && group.SellerOrders.All(x => x.Status == SellerOrderStatus.Cancelled);

    public static bool HasDispatchedQuantity(FulfillmentSnapshot? fulfillment)
    {
        if (fulfillment is null)
        {
            return false;
        }

        if (fulfillment.Status is FulfillmentOperationStatus.Dispatched
            or FulfillmentOperationStatus.InTransit
            or FulfillmentOperationStatus.Delivered)
        {
            return true;
        }

        if (fulfillment.Items.Any(item => item.QuantityShipped > 0))
        {
            return true;
        }

        return fulfillment.Shipments.Any(shipment =>
            shipment.Status != ShipmentOperationStatus.Cancelled
            && (shipment.DispatchedAt is not null
                || shipment.DeliveredAt is not null
                || shipment.Status is ShipmentOperationStatus.Dispatched
                    or ShipmentOperationStatus.InTransit
                    or ShipmentOperationStatus.Delivered));
    }

    public static bool HasDispatchedOrDelivered(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(HasDispatchedQuantity);

    public static bool HasCompletedRefund(
        IReadOnlyList<ReturnSnapshot> returns,
        string? paymentStatus = null) =>
        returns.Any(x => x.Status == ReturnRequestOperationStatus.Completed)
        || paymentStatus == "Refunded";

    public static bool HasIrreversibleFinanceBlock(
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        string? paymentStatus = null) =>
        HasDispatchedOrDelivered(fulfillments) || HasCompletedRefund(returns, paymentStatus);

    public static bool HasStartedFulfillment(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        fulfillments.Any(f =>
            f.Status != FulfillmentOperationStatus.ReadyToFulfill
            || f.Items.Any(i => i.QuantityPacked > 0 || i.QuantityProcessing > 0)
            || f.Shipments.Any(s => s.Status != ShipmentOperationStatus.Cancelled));

    public static bool CanRestoreCancelledOrder(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null)
    {
        if (group.SellerOrders.Count == 0
            || group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled)
            || group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            return false;
        }

        return !HasIrreversibleFinanceBlock(fulfillments, returns, paymentStatus) && !blockedBySellerPayout;
    }

    public static string RestoreForbiddenCode(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null)
    {
        if (group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled))
        {
            return "order.restore.not_cancelled";
        }

        if (group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            return "order.restore.missing_snapshot";
        }

        if (HasCompletedRefund(returns, paymentStatus))
        {
            return "order.restore.refund_completed";
        }

        if (fulfillments.Any(f =>
            f.Status == FulfillmentOperationStatus.Delivered
            || f.Shipments.Any(s => s.DeliveredAt is not null || s.Status == ShipmentOperationStatus.Delivered)))
        {
            return "order.restore.delivered";
        }

        if (HasDispatchedOrDelivered(fulfillments))
        {
            return "order.restore.dispatched";
        }

        if (blockedBySellerPayout)
        {
            return "order.restore.seller_payout_completed";
        }

        return "order.restore.invalid_state";
    }

    public static string RestoreForbiddenMessage(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null) =>
        RestoreCodeToFa(RestoreForbiddenCode(group, fulfillments, returns, blockedBySellerPayout, paymentStatus));

    public static string RestoreCodeToFa(string code) => code switch
    {
        "order.restore.not_cancelled" => "فقط سفارش لغوشده را می‌توان بازگرداند.",
        "order.restore.missing_snapshot" => "وضعیت قبل از لغو برای بازگردانی موجود نیست.",
        "order.restore.refund_completed" => "بازگردانی پس از بازگشت وجه تکمیل‌شده مجاز نیست.",
        "order.restore.delivered" => "بازگردانی پس از تحویل مجاز نیست؛ از مرجوعی استفاده کنید.",
        "order.restore.dispatched" => "بازگردانی پس از ارسال مرسوله مجاز نیست.",
        "order.restore.seller_payout_completed" => "این سفارش به‌دلیل انجام تسویه/واریز سهم فروشنده قابل بازگردانی نیست.",
        "order.restore.inventory_failed" => "بازگردانی ممکن نیست؛ موجودی برای رزرو دوباره کافی نیست. سفارش لغوشده باقی ماند.",
        "order.restore.invalid_state" => "بازگردانی سفارش در این وضعیت مجاز نیست.",
        _ => "بازگردانی سفارش در این وضعیت مجاز نیست.",
    };

    private async Task<bool> HasSellerPayoutRestoreBlockAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        var gates = await _settlement.GetRestoreGatesAsync(sellerOrderIds, cancellationToken);
        return gates.Values.Any(x => x.HasCompletedPayoutEffect);
    }

    private static AdminOrderOperationsException MapPaymentRestoreFault(ContractOperationException ex) =>
        ex.Code switch
        {
            "payment.restore.not_manual" => new AdminOrderOperationsException(ex.Code),
            "payment.restore.already_succeeded" => new AdminOrderOperationsException(ex.Code),
            "payment.restore.invalid_state" => new AdminOrderOperationsException(ex.Code),
            _ => throw ex,
        };

    private static AdminOrderOperationsException MapPaymentUnconfirmFault(ContractOperationException ex) =>
        ex.Code switch
        {
            "payment.unconfirm.not_manual" => new AdminOrderOperationsException(ex.Code),
            "payment.unconfirm.invalid_state" => new AdminOrderOperationsException(ex.Code),
            "fulfillment.unconfirm.already_started" => new AdminOrderOperationsException(ex.Code),
            "order.payment.unconfirm.invalid_state" => new AdminOrderOperationsException(ex.Code),
            "settlement.unconfirm.payout_completed" => new AdminOrderOperationsException(ex.Code),
            _ => throw ex,
        };

    private static bool MatchesIds(AdminOrderOperationAction action, AdminOrderOperationRequest request) =>
        (request.SellerOrderId is null || action.SellerOrderId == request.SellerOrderId)
        && (request.FulfillmentId is null || action.FulfillmentId == request.FulfillmentId)
        && (request.ShipmentId is null || action.ShipmentId == request.ShipmentId)
        && (request.ReturnRequestId is null || action.ReturnRequestId == request.ReturnRequestId)
        && (request.ConsolidatedPackageId is null || action.ConsolidatedPackageId == request.ConsolidatedPackageId);

    private static Guid RequireFulfillmentId(AdminOrderOperationRequest request) =>
        request.FulfillmentId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireShipmentId(AdminOrderOperationRequest request) =>
        request.ShipmentId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireConsolidatedPackageId(AdminOrderOperationRequest request) =>
        request.ConsolidatedPackageId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    private static Guid RequireReturnRequestId(AdminOrderOperationRequest request) =>
        request.ReturnRequestId
        ?? throw new AdminOrderOperationsException("order.operation.invalid");

    /// <summary>
    /// آیا هنوز تعدادی برای ایجاد محمولهٔ جدید باقی مانده (با احتساب محمولهٔ Created باز و packed).
    /// </summary>
    private static bool HasUnallocatedShipmentQuantity(FulfillmentSnapshot fulfillment)
    {
        foreach (var item in fulfillment.Items)
        {
            var openAllocated = OpenAllocated(fulfillment, item.OrderLineId);
            if (item.QuantityPacked > item.QuantityShipped + openAllocated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasProcessableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(x => x.QuantityOrdered > x.QuantityProcessing);

    private static bool HasPackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(x => x.QuantityProcessing > x.QuantityPacked);

    private static bool HasUnpackableQuantity(FulfillmentSnapshot fulfillment) =>
        fulfillment.Items.Any(item =>
        {
            var blocking = OpenAllocated(fulfillment, item.OrderLineId) + item.QuantityShipped;
            return item.QuantityPacked > blocking;
        });

    private static decimal OpenAllocated(FulfillmentSnapshot fulfillment, Guid orderLineId) =>
        fulfillment.Shipments
            .Where(s => s.Status == ShipmentOperationStatus.Created)
            .SelectMany(s => s.Items)
            .Where(line => line.OrderLineId == orderLineId)
            .Sum(line => line.Quantity);

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolvePackSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Where(x => x.QuantityProcessing > x.QuantityPacked)
                .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityProcessing - x.QuantityPacked))
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var packable = item.QuantityProcessing - item.QuantityPacked;
            if (qty > packable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveProcessSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Where(x => x.QuantityOrdered > x.QuantityProcessing)
                .Select(x => new FulfillmentSelectionCommand(x.OrderLineId, x.QuantityOrdered - x.QuantityProcessing))
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var processable = item.QuantityOrdered - item.QuantityProcessing;
            if (qty > processable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveUnprocessSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var unprocessable = item.QuantityProcessing - item.QuantityPacked;
                    return unprocessable > 0
                        ? new FulfillmentSelectionCommand(item.OrderLineId, unprocessable)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<FulfillmentSelectionCommand>()
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var unprocessable = item.QuantityProcessing - item.QuantityPacked;
            if (qty > unprocessable)
            {
                throw new AdminOrderOperationsException("fulfillment.selection.qty_exceeded");
            }
        });
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> ResolveUnpackSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
                    var unpackable = item.QuantityPacked - blocking;
                    return unpackable > 0
                        ? new FulfillmentSelectionCommand(item.OrderLineId, unpackable)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<FulfillmentSelectionCommand>()
                .ToArray();
        }

        return NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
            var unpackable = item.QuantityPacked - blocking;
            if (qty > unpackable)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }
        });
    }

    private static ShipmentLineCommand[] ResolveShipmentSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection>? selections)
    {
        if (selections is null || selections.Count == 0)
        {
            return snapshot.Items
                .Select(item =>
                {
                    var open = OpenAllocated(snapshot, item.OrderLineId);
                    var remaining = item.QuantityPacked - item.QuantityShipped - open;
                    return remaining > 0
                        ? new ShipmentLineCommand(item.OrderLineId, remaining)
                        : null;
                })
                .Where(x => x is not null)
                .Cast<ShipmentLineCommand>()
                .ToArray();
        }

        var normalized = NormalizeAndValidateSelections(snapshot, selections, (item, qty) =>
        {
            var open = OpenAllocated(snapshot, item.OrderLineId);
            var remaining = item.QuantityPacked - item.QuantityShipped - open;
            if (qty > remaining)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }
        });
        return normalized.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray();
    }

    private static IReadOnlyList<FulfillmentSelectionCommand> NormalizeAndValidateSelections(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections,
        Action<FulfillmentItemSnapshot, decimal> validateQuantity)
    {
        var itemsByLine = snapshot.Items.ToDictionary(x => x.OrderLineId);
        var map = new Dictionary<Guid, decimal>();
        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            if (!itemsByLine.ContainsKey(selection.OrderLineId))
            {
                throw new AdminOrderOperationsException("order.operation.invalid");
            }

            map[selection.OrderLineId] = map.TryGetValue(selection.OrderLineId, out var existing)
                ? existing + selection.Quantity
                : selection.Quantity;
        }

        foreach (var pair in map)
        {
            validateQuantity(itemsByLine[pair.Key], pair.Value);
        }

        return map.Select(x => new FulfillmentSelectionCommand(x.Key, x.Value)).ToArray();
    }

    public static bool SelectionsAreHomogeneousUnpackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item))
            {
                return false;
            }

            var blocking = OpenAllocated(snapshot, item.OrderLineId) + item.QuantityShipped;
            if (item.QuantityPacked <= blocking)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousPackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityProcessing <= item.QuantityPacked)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousProcessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityOrdered <= item.QuantityProcessing)
            {
                return false;
            }
        }

        return true;
    }

    public static bool SelectionsAreHomogeneousUnprocessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections)
    {
        if (selections.Count == 0)
        {
            return false;
        }

        var items = snapshot.Items.ToDictionary(x => x.OrderLineId);
        foreach (var selection in selections)
        {
            if (!items.TryGetValue(selection.OrderLineId, out var item)
                || item.QuantityProcessing <= item.QuantityPacked)
            {
                return false;
            }
        }

        return true;
    }

    public static string FulfillmentOpToFa(string code) => code switch
    {
        "fulfillment.pack.requires_processing" => "ابتدا پردازش را شروع کنید.",
        "fulfillment.pack.not_processing" => "این قلم هنوز در مرحله پردازش نیست.",
        "fulfillment.ship.not_packed" => "این قلم هنوز بسته‌بندی نشده است.",
        "fulfillment.selection.qty_exceeded" => "تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.",
        "fulfillment.bulk.incompatible" => "ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.",
        "fulfillment.bulk.cross_seller" => "عملیات گروهی روی فروشندگان متفاوت مجاز نیست.",
        "order.cancelled.blocks_action" => "سفارش لغوشده است؛ این عملیات مجاز نیست.",
        "order.cancel.forbidden" => WholeOrderCancelBlockedAfterDispatchFa,
        "fulfillment.dispatch.invalid_state" => "ارسال در وضعیت فعلی مرسوله مجاز نیست.",
        "fulfillment.dispatch.tracking_required" => "بدون کد رهگیری نمی‌توان ارسال کرد.",
        "fulfillment.dispatch.already_dispatched" => "این مرسوله قبلاً ارسال شده است.",
        "fulfillment.tracking.duplicate" => "این کد رهگیری قبلاً ثبت شده است.",
        "fulfillment.pack.after_delivered" => "پس از تحویل کامل نمی‌توان بسته‌بندی کرد.",
        "fulfillment.process.after_delivered" => "پس از تحویل کامل نمی‌توان پردازش را ادامه داد.",
        "fulfillment.shipment.void_after_dispatch" => "پس از ارسال نمی‌توان مرسوله را ابطال کرد.",
        "fulfillment.shipment.void_invalid_state" => "ابطال مرسوله در این وضعیت مجاز نیست.",
        "fulfillment.allocation.conflict" => "تعداد از باقیماندهٔ قابل تخصیص به مرسوله بیشتر است.",
        "fulfillment.work_queue.row_mismatch" => "ردیف انتخاب‌شده با دادهٔ سرور هم‌خوان نیست.",
        "inventory.reservation.not_active" => "رزرو موجودی این سفارش دیگر فعال نیست. اطلاعات سفارش را تازه‌سازی کنید یا وضعیت رزرو را بررسی کنید.",
        "inventory.manual_review.unavailable" => "موجودی این سفارش در زمان بررسی پرداخت دیگر در دسترس نیست. لطفاً وضعیت سفارش و بازگشت وجه را بررسی کنید.",
        "inventory.reservation.not_found" => "رزرو موجودی این سفارش پیدا نشد. اطلاعات سفارش را تازه‌سازی کنید.",
        "inventory.reservation.stock_mismatch" => "مصرف رزرو با موجودی هم‌خوان نبود.",
        "fulfillment.shipment.locked_by_consolidated_package" =>
            "این مرسوله عضو بسته تجمیعی است و عملیات ارسال باید از طریق همان بسته انجام شود.",
        "fulfillment.package.requires_multi_seller" => "بسته تجمیعی حداقل به دو فروشندهٔ متمایز نیاز دارد.",
        "fulfillment.package.shipment_not_eligible" => "این مرسوله برای بسته تجمیعی واجد شرایط نیست.",
        "fulfillment.package.shipment_already_member" => "این مرسوله هم‌اکنون عضو یک بسته تجمیعی فعال است.",
        "fulfillment.package.mixed_checkout" => "فقط مرسوله‌های همین سفارش را می‌توان در یک بسته تجمیعی قرار داد.",
        "fulfillment.package.duplicate_shipment" => "مرسوله تکراری در بسته تجمیعی مجاز نیست.",
        "fulfillment.package.cancel_after_dispatch" => "پس از ارسال بسته تجمیعی، ابطال مجاز نیست.",
        "fulfillment.package.dispatch_invalid_state" => "ارسال بسته تجمیعی در وضعیت فعلی مجاز نیست.",
        "fulfillment.package.deliver_before_dispatch" => "قبل از ارسال نمی‌توان بسته تجمیعی را تحویل داد.",
        "fulfillment.package.member_state_changed" => "وضعیت مرسوله‌های عضو تغییر کرده است؛ عملیات را تازه کنید.",
        "fulfillment.package.not_found" => "بسته تجمیعی پیدا نشد.",
        "fulfillment.package.shipping_method_required" => "روش ارسال مرسوله‌های عضو برای بسته تجمیعی الزامی است.",
        "fulfillment.package.shipping_method_mismatch" =>
            "برای ایجاد بسته تجمیعی، روش ارسال مرسوله‌های انتخاب‌شده باید یکسان باشد.",
        "fulfillment.package.tracking_required" => "برای ارسال بسته تجمیعی باید کد رهگیری مرکزی ثبت شود.",
        "fulfillment.package.tracking_locked" => "پس از ارسال بسته تجمیعی، تغییر کد رهگیری مجاز نیست.",
        "fulfillment.package.checkout_required" => "شناسه سفارش برای بسته تجمیعی الزامی است.",
        _ => "این عملیات در وضعیت فعلی سفارش مجاز نیست.",
    };

    private static AdminOrderOperationAction Action(
        string code,
        string labelFa,
        string labelEn,
        Guid? sellerOrderId,
        Guid? fulfillmentId,
        Guid? shipmentId,
        Guid? returnRequestId,
        string requiredPermission,
        bool requiresConfirm,
        string? confirmMessageFa,
        Guid? orderLineId = null,
        Guid? consolidatedPackageId = null) =>
        new(
            code,
            labelFa,
            labelEn,
            sellerOrderId,
            fulfillmentId,
            shipmentId,
            returnRequestId,
            requiredPermission,
            requiresConfirm,
            confirmMessageFa,
            orderLineId,
            consolidatedPackageId);
}

/// <summary>
/// حذف تکرار اقدام‌های کل‌سفارش؛ seller-scoped دست نخورده می‌ماند.
/// </summary>
public static class AdminOrderWholeOrderActions
{
    private static readonly HashSet<string> WholeOrderCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "cancel",
        "confirm_deposit",
        "reject_deposit",
        "restore_deposit",
        "unconfirm_deposit",
        "recover_inventory_reservation",
        "restore_cancelled_order",
    };

    /// <summary>هر کد کل‌سفارش حداکثر یک‌بار؛ ترجیح با sellerOrderId خالی.</summary>
    public static List<AdminOrderOperationAction> Collapse(IReadOnlyList<AdminOrderOperationAction> actions)
    {
        var preferred = new Dictionary<string, AdminOrderOperationAction>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in actions)
        {
            if (!WholeOrderCodes.Contains(action.Code))
            {
                continue;
            }

            if (!preferred.TryGetValue(action.Code, out var existing) || action.SellerOrderId is null)
            {
                preferred[action.Code] = action;
            }
            else
            {
                _ = existing;
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<AdminOrderOperationAction>(actions.Count);
        foreach (var action in actions)
        {
            if (WholeOrderCodes.Contains(action.Code))
            {
                if (!seen.Add(action.Code))
                {
                    continue;
                }

                result.Add(preferred[action.Code]);
                continue;
            }

            result.Add(action);
        }

        return result;
    }
}
