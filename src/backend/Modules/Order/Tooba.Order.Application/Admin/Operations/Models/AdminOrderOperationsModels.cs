using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Operations.Models;

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
    string? ConfirmMessageFa,
    Guid? OrderLineId = null,
    Guid? ConsolidatedPackageId = null);

/// <summary>قابلیت lifecycle یک خط — منبع انتخاب/کebab/مرسوله.</summary>
public sealed record AdminOrderLineCapability(
    Guid OrderLineId,
    Guid SellerOrderId,
    bool Selectable,
    decimal SelectableQuantityMax,
    IReadOnlyList<string> RowActionCodes,
    IReadOnlyList<string> BulkActionCodes,
    decimal ShipmentEligibleQuantity,
    string? LockedReasonCode = null,
    string? LockedReasonFa = null);

/// <summary>قابلیت تجمیعی فروشنده برای اقلام و ارسال.</summary>
public sealed record AdminSellerCapability(
    Guid SellerOrderId,
    bool SelectionAllowed,
    bool PaymentLocked,
    string? InfoMessageFa,
    IReadOnlyList<string> WholeGroupActionCodes,
    bool ShipmentCreationPossible);

/// <summary>صفحهٔ عملیات سفارش برای یک checkout.</summary>
public sealed record AdminOrderOperationsPage(
    Guid CheckoutId,
    IReadOnlyList<AdminOrderOperationAction> Actions,
    IReadOnlyList<ReturnEligibilityResult> ReturnEligibility,
    IReadOnlyList<AdminOrderLineCapability>? LineCapabilities = null,
    IReadOnlyList<AdminSellerCapability>? SellerCapabilities = null,
    string? InventoryRecoveryWarningFa = null,
    string? InventoryRecoveryClass = null,
    string? SupplyStatus = null,
    string? SupplyMessageFa = null,
    bool CanConfirmDeposit = false,
    bool CanRecoverInventory = false,
    IReadOnlyList<AdminSupplyLineShortage>? SupplyLines = null);

/// <summary>کمبود خط تأمین برای Admin — بدون GUID رزرو.</summary>
public sealed record AdminSupplyLineShortage(
    string? ItemTitle,
    string? UnitCode,
    decimal Required,
    decimal Available,
    decimal Shortage);

/// <summary>انتخاب خط/تعداد برای عملیات seller-scoped.</summary>
public sealed record AdminOrderLineSelection(Guid OrderLineId, decimal Quantity);

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
    IReadOnlyList<ReturnLineCommand>? ReturnItems,
    IReadOnlyList<AdminOrderLineSelection>? Selections = null,
    string? ShippingMethodCode = null,
    string? ProviderMetadataJson = null,
    Guid? ConsolidatedPackageId = null,
    IReadOnlyList<Guid>? ShipmentIds = null,
    Guid? ShippingGroupId = null,
    /// <summary>TB-253-R2: constituent accepted groups of one operational group workflow.</summary>
    IReadOnlyList<Guid>? OperationalGroupShippingGroupIds = null,
    /// <summary>TB-253-R2: existing unattached parcels explicitly reviewed for reuse.</summary>
    IReadOnlyList<Guid>? ReuseShipmentIds = null,
    /// <summary>
    /// TB-253-R3: the workflow occurrence this request belongs to. Prebound by the client before the
    /// first mutation and retained on retry/remount; same occurrence replays, a new occurrence is an
    /// explicit new operation.
    /// </summary>
    Guid? OperationalGroupOccurrenceId = null,
    /// <summary>TB-258: optional operator note recorded with a non-delivery attempt or origin receipt.</summary>
    string? Note = null,
    /// <summary>
    /// TB-258: the physically received lines of one origin receipt, each with its own condition. Only usable
    /// quantity funds a later resend; damaged/missing quantity never does.
    /// </summary>
    IReadOnlyList<AdminReturnReceiptLineSelection>? ReceiptItems = null);

/// <summary>TB-258: one physically received line submitted with an origin receipt.</summary>
public sealed record AdminReturnReceiptLineSelection(
    Guid OrderLineId,
    decimal Quantity,
    int Condition = 0);
