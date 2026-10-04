// One-off generator for the W1 Fulfillment error catalog + resx (not shipped).
const fs = require("fs");

// name -> [code, classification, status, en, fa]
const rows = [
  ["Missing","fulfillment.missing","NotFound",404,"Fulfillment was not found.","مورد تحویل یافت نشد."],
  ["NotFound","fulfillment.not_found","NotFound",404,"Fulfillment was not found.","مورد تحویل یافت نشد."],
  ["Rejected","fulfillment.rejected","Business",400,"Fulfillment mutation was rejected.","تغییر مورد تحویل رد شد."],
  ["OrderNotFound","fulfillment.order.not_found","NotFound",404,"Source order was not found.","سفارش مبدأ یافت نشد."],
  ["OrderNotPaid","fulfillment.order.not_paid","Business",400,"Source order is not paid.","سفارش مبدأ پرداخت نشده است."],
  ["OrderLineNotFound","fulfillment.order_line.not_found","NotFound",404,"Order line was not found.","خط سفارش یافت نشد."],
  ["QuantityPositive","fulfillment.qty.positive","Business",400,"Quantity must be positive.","مقدار باید بزرگ‌تر از صفر باشد."],
  ["StatusTerminal","fulfillment.status.terminal","Conflict",409,"Fulfillment is in a terminal status.","مورد تحویل در وضعیت پایانی است."],
  ["StatusProcessingInvalid","fulfillment.status.processing_invalid","Conflict",409,"Processing status transition is invalid.","تغییر وضعیت پردازش نامعتبر است."],
  ["SelectionRequired","fulfillment.selection.required","Business",400,"A selection is required.","انتخاب الزامی است."],
  ["OutboxUnmappedEvent","fulfillment.outbox.unmapped_event","Platform",503,"Fulfillment outbox event has no mapping.","رویداد outbox تحویل نگاشت ندارد."],

  ["ProcessingQuantityExceeds","fulfillment.processing.qty_exceeds","Business",400,"Processing quantity exceeds the sellable amount.","مقدار پردازش از مقدار قابل فروش بیشتر است."],
  ["ProcessingQuantityPositive","fulfillment.processing.qty_positive","Business",400,"Processing quantity must be positive.","مقدار پردازش باید بزرگ‌تر از صفر باشد."],
  ["ProcessingReleaseInvalid","fulfillment.processing.release_invalid","Conflict",409,"Processing release is invalid.","آزادسازی پردازش نامعتبر است."],
  ["ProcessingReleaseQuantityPositive","fulfillment.processing.release_qty_positive","Business",400,"Processing release quantity must be positive.","مقدار آزادسازی پردازش باید بزرگ‌تر از صفر باشد."],
  ["ProcessAfterDelivered","fulfillment.process.after_delivered","Conflict",409,"Cannot process after delivery.","پس از تحویل امکان پردازش نیست."],
  ["UnconfirmAlreadyStarted","fulfillment.unconfirm.already_started","Conflict",409,"Cannot unconfirm a started fulfillment.","مورد تحویل شروع‌شده را نمی‌توان لغو تأیید کرد."],

  ["PackQuantityExceeds","fulfillment.pack.qty_exceeds","Business",400,"Packing quantity exceeds the processed amount.","مقدار بسته‌بندی از مقدار پردازش‌شده بیشتر است."],
  ["PackQuantityPositive","fulfillment.pack.qty_positive","Business",400,"Packing quantity must be positive.","مقدار بسته‌بندی باید بزرگ‌تر از صفر باشد."],
  ["PackReleaseExceeds","fulfillment.pack.release_exceeds","Business",400,"Release exceeds the packed amount.","آزادسازی از مقدار بسته‌بندی‌شده بیشتر است."],
  ["PackReleaseQuantityPositive","fulfillment.pack.release_qty_positive","Business",400,"Release quantity must be positive.","مقدار آزادسازی باید بزرگ‌تر از صفر باشد."],
  ["PackReleaseAllocated","fulfillment.pack.release_allocated","Conflict",409,"Cannot release an already allocated line.","خط تخصیص‌یافته را نمی‌توان آزاد کرد."],
  ["PackRequiresProcessing","fulfillment.pack.requires_processing","Conflict",409,"Packing requires the line to be processed first.","بسته‌بندی نیازمند پردازش قبلی خط است."],
  ["PackAfterDelivered","fulfillment.pack.after_delivered","Conflict",409,"Cannot pack after delivery.","پس از تحویل امکان بسته‌بندی نیست."],
  ["ShipQuantityExceeds","fulfillment.ship.qty_exceeds","Business",400,"Ship quantity exceeds the packed amount.","مقدار ارسال از مقدار بسته‌بندی‌شده بیشتر است."],
  ["ShipQuantityPositive","fulfillment.ship.qty_positive","Business",400,"Ship quantity must be positive.","مقدار ارسال باید بزرگ‌تر از صفر باشد."],
  ["CancelAlreadyDispatched","fulfillment.cancel.already_dispatched","Conflict",409,"Cannot cancel an already dispatched fulfillment.","مورد تحویل ارسال‌شده را نمی‌توان لغو کرد."],
  ["RestoreAlreadyDispatched","fulfillment.restore.already_dispatched","Conflict",409,"Cannot restore an already dispatched fulfillment.","مورد تحویل ارسال‌شده را نمی‌توان بازگرداند."],

  ["ShipmentNotFound","fulfillment.shipment.not_found","NotFound",404,"Shipment was not found.","محموله یافت نشد."],
  ["ShipmentQuantityExceedsOrdered","fulfillment.shipment.qty_exceeds_ordered","Business",400,"Shipment quantity exceeds the ordered amount.","مقدار محموله از مقدار سفارش بیشتر است."],
  ["ShipmentQuantityExceedsPacked","fulfillment.shipment.qty_exceeds_packed","Business",400,"Shipment quantity exceeds the packed amount.","مقدار محموله از مقدار بسته‌بندی‌شده بیشتر است."],
  ["ShipmentCancelInvalid","fulfillment.shipment.cancel_invalid","Conflict",409,"Shipment cancellation is invalid in this state.","لغو محموله در این وضعیت نامعتبر است."],
  ["ShipmentCarrierRequired","fulfillment.shipment.carrier_required","Business",400,"Carrier display name is required.","نام نمایشی حامل الزامی است."],
  ["ShipmentCreateTerminal","fulfillment.shipment.create_terminal","Conflict",409,"Cannot create a shipment for a terminal fulfillment.","برای مورد تحویل پایانی امکان ایجاد محموله نیست."],
  ["ShipmentLockedByConsolidatedPackage","fulfillment.shipment.locked_by_consolidated_package","Conflict",409,"Shipment is locked by a consolidated package.","محموله توسط بستهٔ تجمیعی قفل شده است."],

  ["DispatchInvalidStatus","fulfillment.dispatch.invalid_status","Conflict",409,"Dispatch is invalid in this state.","ارسال در این وضعیت نامعتبر است."],
  ["DispatchTrackingRequired","fulfillment.dispatch.tracking_required","Business",400,"Dispatch requires a tracking reference.","ارسال نیازمند کد رهگیری است."],
  ["DeliverInvalidStatus","fulfillment.deliver.invalid_status","Conflict",409,"Delivery is invalid in this state.","تحویل در این وضعیت نامعتبر است."],
  ["TrackingRequired","fulfillment.tracking.required","Business",400,"Tracking reference is required.","کد رهگیری الزامی است."],
  ["TrackingAlreadySet","fulfillment.tracking.already_set","Conflict",409,"Tracking reference was already set.","کد رهگیری قبلاً ثبت شده است."],
  ["TrackingDuplicate","fulfillment.tracking.duplicate","Conflict",409,"Duplicate tracking reference.","کد رهگیری تکراری است."],
  ["TrackingInvalidState","fulfillment.tracking.invalid_state","Conflict",409,"Tracking state does not allow this change.","وضعیت رهگیری اجازهٔ این تغییر را نمی‌دهد."],
  ["TrackingLockedAfterDispatch","fulfillment.tracking.locked_after_dispatch","Conflict",409,"Tracking is locked after dispatch.","کد رهگیری پس از ارسال قفل شده است."],
  ["TrackingNothingToCorrect","fulfillment.tracking.nothing_to_correct","Conflict",409,"There is nothing to correct on the tracking reference.","برای کد رهگیری چیزی برای اصلاح وجود ندارد."],

  ["PackageNotFound","fulfillment.package.not_found","NotFound",404,"Consolidated package was not found.","بستهٔ تجمیعی یافت نشد."],
  ["PackageRequiresMultiSeller","fulfillment.package.requires_multi_seller","Conflict",409,"Consolidation requires multiple sellers.","تجمیع نیازمند چند فروشنده است."],
  ["PackageMixedCheckout","fulfillment.package.mixed_checkout","Conflict",409,"Consolidation cannot span checkouts.","تجمیع نمی‌تواند چند تسویه را در بر گیرد."],
  ["PackageCheckoutRequired","fulfillment.package.checkout_required","Business",400,"Checkout is required for consolidation.","تسویه برای تجمیع الزامی است."],
  ["PackageShippingMethodRequired","fulfillment.package.shipping_method_required","Business",400,"Shipping method is required for consolidation.","روش ارسال برای تجمیع الزامی است."],
  ["PackageShippingMethodMismatch","fulfillment.package.shipping_method_mismatch","Conflict",409,"Consolidated members must share one shipping method.","اعضای تجمیع باید روش ارسال یکسان داشته باشند."],
  ["PackageShipmentAlreadyMember","fulfillment.package.shipment_already_member","Conflict",409,"Shipment is already a package member.","محموله از قبل عضو بسته است."],
  ["PackageShipmentNotEligible","fulfillment.package.shipment_not_eligible","Conflict",409,"Shipment is not eligible for consolidation.","محموله برای تجمیع واجد شرایط نیست."],
  ["PackageDuplicateShipment","fulfillment.package.duplicate_shipment","Conflict",409,"Duplicate shipment in the package.","محمولهٔ تکراری در بسته."],
  ["PackageMemberStateChanged","fulfillment.package.member_state_changed","Conflict",409,"Package member state changed underneath the operation.","وضعیت عضو بسته در حین عملیات تغییر کرد."],
  ["PackageDispatchInvalidState","fulfillment.package.dispatch_invalid_state","Conflict",409,"Package dispatch is invalid in this state.","ارسال بسته در این وضعیت نامعتبر است."],
  ["PackageDeliverBeforeDispatch","fulfillment.package.deliver_before_dispatch","Conflict",409,"Package cannot be delivered before dispatch.","بسته پیش از ارسال قابل تحویل نیست."],
  ["PackageCancelAfterDispatch","fulfillment.package.cancel_after_dispatch","Conflict",409,"Package cannot be cancelled after dispatch.","بسته پس از ارسال قابل لغو نیست."],
  ["PackageTrackingLocked","fulfillment.package.tracking_locked","Conflict",409,"Package tracking is locked.","کد رهگیری بسته قفل شده است."],
  ["PackageTrackingRequired","fulfillment.package.tracking_required","Business",400,"Package tracking is required.","کد رهگیری بسته الزامی است."],

  ["ShippingMethodUnsupported","fulfillment.shipping_method.unsupported","Business",400,"Shipping method is unsupported.","روش ارسال پشتیبانی نمی‌شود."],
  ["ShippingPostRecipientRequired","fulfillment.shipping.post.recipient_required","Validation",400,"Post provider requires a recipient name.","ارسال پستی نیازمند نام گیرنده است."],
  ["ShippingPostAddressRequired","fulfillment.shipping.post.address_required","Validation",400,"Post provider requires a full address.","ارسال پستی نیازمند نشانی کامل است."],
  ["ShippingPostalInvalid","fulfillment.shipping.postal_invalid","Validation",400,"Postal code is invalid.","کد پستی نامعتبر است."],
  ["ShippingTipaxAddressRequired","fulfillment.shipping.tipax.address_required","Validation",400,"Tipax provider requires a full address.","ارسال تیپاکس نیازمند نشانی کامل است."],
  ["ShippingCourierAddressRequired","fulfillment.shipping.courier.address_required","Validation",400,"Courier provider requires a full address.","ارسال پیک نیازمند نشانی کامل است."],
  ["ShippingPickupLocationRequired","fulfillment.shipping.pickup.location_required","Validation",400,"Pickup provider requires a location.","تحویل حضوری نیازمند مکان است."],
  ["ShippingMobileInvalid","fulfillment.shipping.mobile_invalid","Validation",400,"Mobile number is invalid.","شمارهٔ همراه نامعتبر است."],

  ["SellerOrderHandleDenied","seller.order.handle.denied","Forbidden",403,"Seller lacks order.handle permission.","فروشنده مجوز order.handle ندارد."],
  ["SellerOrderHandleScopeDenied","seller.order.handle.scope_denied","Forbidden",403,"Order.handle category scope does not cover all lines.","دامنهٔ دستهٔ order.handle همهٔ خطوط را پوشش نمی‌دهد."],
  ["SellerOrderMissing","seller.order.missing","NotFound",404,"Seller order was not found.","سفارش فروشنده یافت نشد."],
  ["CustomerActorMissing","customer.actor.missing","Forbidden",401,"Customer actor is missing.","هویت مشتری موجود نیست."],
  ["CustomerOrderMissing","customer.order.missing","NotFound",404,"Customer order was not found.","سفارش مشتری یافت نشد."],

  ["WorkQueueBulkFailed","fulfillment.work_queue.bulk_failed","Business",400,"Fulfillment work-queue bulk operation failed.","عملیات گروهی صف کار تحویل ناموفق بود."],
  ["WorkQueueBulkUnsupported","fulfillment.work_queue.bulk_unsupported","Business",400,"Fulfillment work-queue bulk action is unsupported.","عملیات گروهی صف کار تحویل پشتیبانی نمی‌شود."],
  ["WorkQueueBulkEmpty","fulfillment.work_queue.bulk_empty","Business",400,"Fulfillment work-queue bulk selection is empty.","انتخاب گروهی صف کار تحویل خالی است."],
  ["WorkQueueCrossSeller","fulfillment.work_queue.cross_seller","Business",400,"Fulfillment work-queue bulk cannot span sellers.","عملیات گروهی صف کار نمی‌تواند چند فروشنده را در بر گیرد."],
  ["WorkQueueRowMismatch","fulfillment.work_queue.row_mismatch","Business",400,"Fulfillment work-queue row does not match server state.","ردیف صف کار با وضعیت سرور مطابقت ندارد."],
  ["WorkQueueIncompatible","fulfillment.work_queue.incompatible","Business",400,"Fulfillment work-queue bulk selection is incompatible.","انتخاب گروهی صف کار ناسازگار است."],
  ["WorkQueueShipmentMissing","fulfillment.work_queue.shipment_missing","Business",400,"Fulfillment work-queue shipment is missing.","محمولهٔ صف کار تحویل موجود نیست."],

  ["ShippingServiceNotFound","shipping_service.not_found","NotFound",404,"Shipping service was not found.","سرویس ارسال یافت نشد."],
  ["ShippingServiceCodeDuplicate","shipping_service.code_duplicate","Business",400,"Shipping service code already exists.","کد سرویس ارسال تکراری است."],
  ["ShippingServiceCodeRequired","shipping_service.code.required","Validation",400,"Shipping service code is required.","کد سرویس ارسال الزامی است."],
  ["ShippingServiceNameRequired","shipping_service.name.required","Validation",400,"Shipping service name is required.","نام سرویس ارسال الزامی است."],
  ["ShippingServiceLanguageInvalid","shipping_service.language_invalid","Validation",400,"Shipping service language is invalid.","زبان سرویس ارسال نامعتبر است."],
  ["ShippingServiceOptionCodeRequired","shipping_service_option.code.required","Validation",400,"Shipping service option code is required.","کد گزینهٔ سرویس ارسال الزامی است."],
  ["ShippingServiceOptionNameRequired","shipping_service_option.name.required","Validation",400,"Shipping service option name is required.","نام گزینهٔ سرویس ارسال الزامی است."],
];

const pad = (s, n) => s + " ".repeat(Math.max(0, n - s.length));

let out = `using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Infrastructure.Errors;

/// <summary>
/// Explicit error catalog for every stable Fulfillment-owned semantic code.
/// Duplicate usage is allowed; duplicate descriptor ownership is not — this is the single
/// owner of the \`fulfillment.*\`, \`shipping_service.*\` and \`shipping_service_option.*\` keyspaces
/// plus the seller/customer authorization codes Fulfillment emits at its own boundary.
/// </summary>
public sealed class FulfillmentErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
`;

for (const [name, code, cls, status, en] of rows) {
  const statusName = {400:"Status400BadRequest",401:"Status401Unauthorized",403:"Status403Forbidden",404:"Status404NotFound",409:"Status409Conflict",503:"Status503ServiceUnavailable"}[status];
  out += `        D(FulfillmentErrorCodes.${name}, ErrorClassification.${cls}, StatusCodes.${statusName},\n            "${en.replace(/"/g,'\\"')}"),\n`;
}
out += `    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
`;
fs.writeFileSync(process.argv[2], out);
console.log("descriptors:", rows.length);
console.log("unique codes:", new Set(rows.map(r=>r[1])).size);
