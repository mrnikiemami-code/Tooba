using Tooba.Returns.Application;

namespace Tooba.Host.Admin;

/// <summary>یک اقدام lifecycle قابل‌نمایش برای سفارش ادمین.</summary>
public sealed record AdminOrderOperationAction(
    string Code,
    string LabelFa,
    string LabelEn,
    Guid? SellerOrderId,
    Guid? FulfillmentId,
    Guid? ShipmentId,
    Guid? ReturnRequestId,
    string RequiredPermission,
    bool RequiresConfirm,
    string? ConfirmMessageFa);

/// <summary>صفحهٔ عملیات سفارش برای یک checkout.</summary>
public sealed record AdminOrderOperationsPage(
    Guid CheckoutId,
    IReadOnlyList<AdminOrderOperationAction> Actions,
    IReadOnlyList<ReturnEligibilityResult> ReturnEligibility);

/// <summary>بدنهٔ اجرای یک عملیات سفارش.</summary>
public sealed record AdminOrderOperationRequest(
    string Code,
    Guid? SellerOrderId,
    Guid? FulfillmentId,
    Guid? ShipmentId,
    Guid? ReturnRequestId,
    string? CarrierDisplayName,
    string? TrackingReference,
    string? Reason,
    string? IdempotencyKey,
    IReadOnlyList<ReturnLineCommand>? ReturnItems);
