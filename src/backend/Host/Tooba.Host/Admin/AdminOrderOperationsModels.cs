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
    IReadOnlyList<Guid>? ShipmentIds = null);
