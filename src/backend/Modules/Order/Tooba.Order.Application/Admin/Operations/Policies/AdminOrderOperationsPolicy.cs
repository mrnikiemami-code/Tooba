using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Domain;
using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Operations.Policies;

/// <summary>
/// Public policy helpers previously hosted on AdminOrderOperationsComposer.
/// </summary>
public static class AdminOrderOperationsPolicy
{
    public static readonly HashSet<string> CancelledBlockedCodes = AdminOrderOperationsOrchestrator.CancelledBlockedCodes;

    public const string WholeOrderCancelBlockedAfterDispatchFa =
        AdminOrderOperationsOrchestrator.WholeOrderCancelBlockedAfterDispatchFa;

    public const string WholeOrderCancelConfirmFa =
        AdminOrderOperationsOrchestrator.WholeOrderCancelConfirmFa;

    public static bool Has(OrderAdminEffectiveAccess effective, string permissionId) =>
        AdminOrderOperationsOrchestrator.Has(effective, permissionId);

    public static bool HasAny(OrderAdminEffectiveAccess effective, params string[] permissionIds) =>
        AdminOrderOperationsOrchestrator.HasAny(effective, permissionIds);

    public static string Prefer(OrderAdminEffectiveAccess effective, params string[] permissionIds) =>
        AdminOrderOperationsOrchestrator.Prefer(effective, permissionIds);

    public static bool CanCancel(AdminOrderOpsSellerOrderSnapshot order, FulfillmentSnapshot? fulfillment) =>
        AdminOrderOperationsOrchestrator.CanCancel(order, fulfillment);

    public static bool IsCheckoutCancelled(AdminOrderOpsCheckoutSnapshot group) =>
        AdminOrderOperationsOrchestrator.IsCheckoutCancelled(group);

    public static bool IsCheckoutCancelled(IEnumerable<SellerOrderStatus> statuses)
    {
        var list = statuses as IList<SellerOrderStatus> ?? statuses.ToList();
        return list.Count > 0 && list.All(x => x == SellerOrderStatus.Cancelled);
    }

    public static bool HasDispatchedQuantity(FulfillmentSnapshot? fulfillment) =>
        AdminOrderOperationsOrchestrator.HasDispatchedQuantity(fulfillment);

    public static bool HasDispatchedOrDelivered(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        AdminOrderOperationsOrchestrator.HasDispatchedOrDelivered(fulfillments);

    public static bool HasCompletedRefund(IReadOnlyList<ReturnSnapshot> returns, string? paymentStatus = null) =>
        AdminOrderOperationsOrchestrator.HasCompletedRefund(returns, paymentStatus);

    public static bool HasIrreversibleFinanceBlock(
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns) =>
        AdminOrderOperationsOrchestrator.HasIrreversibleFinanceBlock(fulfillments, returns);

    public static bool HasStartedFulfillment(IReadOnlyList<FulfillmentSnapshot> fulfillments) =>
        AdminOrderOperationsOrchestrator.HasStartedFulfillment(fulfillments);

    public static bool CanRestoreCancelledOrder(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null) =>
        AdminOrderOperationsOrchestrator.CanRestoreCancelledOrder(
            group, fulfillments, returns, blockedBySellerPayout, paymentStatus);

    public static string RestoreForbiddenCode(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null) =>
        AdminOrderOperationsOrchestrator.RestoreForbiddenCode(
            group, fulfillments, returns, blockedBySellerPayout, paymentStatus);

    public static string RestoreForbiddenMessage(
        AdminOrderOpsCheckoutSnapshot group,
        IReadOnlyList<FulfillmentSnapshot> fulfillments,
        IReadOnlyList<ReturnSnapshot> returns,
        bool blockedBySellerPayout = false,
        string? paymentStatus = null) =>
        AdminOrderOperationsOrchestrator.RestoreForbiddenMessage(
            group, fulfillments, returns, blockedBySellerPayout, paymentStatus);

    public static string RestoreCodeToFa(string code) =>
        AdminOrderOperationsOrchestrator.RestoreCodeToFa(code);

    public static string FulfillmentOpToFa(string code) =>
        AdminOrderOperationsOrchestrator.FulfillmentOpToFa(code);

    public static bool SelectionsAreHomogeneousUnpackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections) =>
        AdminOrderOperationsOrchestrator.SelectionsAreHomogeneousUnpackable(snapshot, selections);

    public static bool SelectionsAreHomogeneousPackable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections) =>
        AdminOrderOperationsOrchestrator.SelectionsAreHomogeneousPackable(snapshot, selections);

    public static bool SelectionsAreHomogeneousProcessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections) =>
        AdminOrderOperationsOrchestrator.SelectionsAreHomogeneousProcessable(snapshot, selections);

    public static bool SelectionsAreHomogeneousUnprocessable(
        FulfillmentSnapshot snapshot,
        IReadOnlyList<AdminOrderLineSelection> selections) =>
        AdminOrderOperationsOrchestrator.SelectionsAreHomogeneousUnprocessable(snapshot, selections);

    public static List<AdminOrderOperationAction> Collapse(IReadOnlyList<AdminOrderOperationAction> actions) =>
        AdminOrderWholeOrderActions.Collapse(actions);
}
