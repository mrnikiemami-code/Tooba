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
}