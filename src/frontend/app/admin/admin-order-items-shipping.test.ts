import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminOrderDetail } from "./admin-api.ts";
import {
  canonicalReturnDisplay,
  compatibleBulkCodes,
  deriveLineCapability,
  isIncompatibleSelection,
  isPaymentLockedSeller,
  lineLifecycleActions,
  MIXED_SELECTION_MESSAGE_FA,
  PAYMENT_LOCKED_BANNER_FA,
  rowActionsForLine,
} from "./admin-order-line-actions.ts";
import { sellerQuickActionLabels } from "./admin-order-operations-scope.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("items-shipping shell is mounted above financial section", () => {
  const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
  const panelIdx = detail.indexOf("AdminOrderItemsShippingPanel");
  const financeIdx = detail.indexOf("بخش مالی سفارش");
  assert.ok(panelIdx > 0);
  assert.ok(financeIdx > panelIdx);
  assert.match(detail, /اقلام و ارسال|AdminOrderItemsShippingPanel/);
});

test("seller groups stay separate and selection cannot cross sellers in one op", () => {
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /admin-order-seller-group-/);
  assert.match(panel, /selectedBySeller/);
  assert.match(panel, /toggleLine\(seller\.id/);
  assert.doesNotMatch(panel, /selectedAcrossSellers|globalSelection/);
});

test("shipments map from additive seller DTO", () => {
  const detail = mapAdminOrderDetail({
    checkoutId: "c1",
    reference: "R1",
    sellerOrders: [{
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      fulfillmentId: "f1",
      fulfillmentStatus: "Packed",
      lines: [{
        orderLineId: "ol1",
        offerId: "o1",
        productTitle: "کالا",
        quantity: 2,
        quantityShipped: 1,
        unitAmount: 100,
        linePayable: 200,
        currency: "IRR",
      }],
      shipments: [{
        shipmentId: "sh1",
        status: "Created",
        carrierDisplayName: "تیپاکس",
        trackingReference: "TRK-1",
        itemCount: 1,
        lines: [{ orderLineId: "ol1", quantity: 1 }],
      }],
    }],
  });
  assert.equal(detail?.sellerOrders[0]?.fulfillmentId, "f1");
  assert.equal(detail?.sellerOrders[0]?.shipments.length, 1);
  assert.equal(detail?.sellerOrders[0]?.shipments[0]?.carrierDisplayName, "تیپاکس");
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.orderLineId, "ol1");
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.quantityShipped, 1);
});

test("selected-state labels switch correctly", () => {
  assert.deepEqual(sellerQuickActionLabels(false), {
    startProcessing: "شروع پردازش",
    pack: "بسته‌بندی همه اقلام آماده",
    createShipment: "ایجاد مرسوله جدید",
    unpack: "بازگشت از بسته‌بندی",
    unprocess: "برگشت از پردازش",
    dispatch: "ارسال",
  });
  assert.deepEqual(sellerQuickActionLabels(true), {
    startProcessing: "شروع پردازش انتخاب‌شده‌ها",
    pack: "بسته‌بندی انتخاب‌شده‌ها",
    createShipment: "ایجاد مرسوله از انتخاب‌شده‌ها",
    unpack: "بازگشت از بسته‌بندی انتخاب‌شده‌ها",
    unprocess: "برگشت از پردازش انتخاب‌شده‌ها",
    dispatch: "ارسال انتخاب‌شده‌ها",
  });
});

test("detail and whole-order menus hide start-processing and pack", () => {
  const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
  assert.match(detail, /scope="whole-order"/);
  assert.doesNotMatch(detail, /scope="detail"/);
});

test("row kebab is projection-driven and hidden when empty", () => {
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /lineLifecycleActions/);
  assert.match(panel, /admin-order-line-kebab-/);
  assert.match(panel, /lineActions\.length === 0 \? null/);
  assert.doesNotMatch(panel, /title="عملیات ردیف از نوار فروشنده و کارت مرسوله"/);
  assert.match(panel, /pack_selected/);
  assert.match(panel, /mark_processing/);
  assert.match(panel, /unprocess/);
  assert.match(panel, /برگشت از پردازش|unprocess/);
  assert.match(panel, /MIXED_SELECTION_MESSAGE_FA/);
});

test("exact selection and mixed bulk helpers", () => {
  const actions = [
    { code: "pack_selected", orderLineId: "L1" },
    { code: "pack_selected", orderLineId: "L2" },
    { code: "unpack", orderLineId: "L3" },
  ];
  assert.deepEqual(rowActionsForLine(actions, "L1").map((a) => a.code), ["pack_selected"]);
  assert.equal(rowActionsForLine(actions, "none").length, 0);
  assert.deepEqual([...compatibleBulkCodes(actions, ["L1", "L2"])], ["pack_selected"]);
  assert.equal(compatibleBulkCodes(actions, ["L1", "L3"]).size, 0);
  assert.equal(MIXED_SELECTION_MESSAGE_FA, "برای عملیات گروهی، اقلام هم‌مرحله را انتخاب کنید.");
});

test("single selection never shows mixed and recovers seller-level row actions", () => {
  const sellerPack = [{ code: "pack_selected", orderLineId: undefined as string | undefined }];
  const recovered = lineLifecycleActions(sellerPack, "L1", { packable: true, unpackable: false });
  assert.deepEqual(recovered.map((a) => a.code), ["pack_selected"]);
  assert.equal(lineLifecycleActions(sellerPack, "L1", { packable: false, unpackable: false }).length, 0);
  assert.equal(isIncompatibleSelection(1, new Set(), [new Set(["pack_selected"])]), false);
  assert.equal(
    isIncompatibleSelection(2, new Set(), [new Set(["pack_selected"]), new Set(["unpack"])]),
    true,
  );
  assert.equal(
    isIncompatibleSelection(2, new Set(["pack_selected"]), [new Set(["pack_selected"]), new Set(["pack_selected"])]),
    false,
  );
});

test("return display is shown once when deadline and remaining match", () => {
  assert.equal(
    canonicalReturnDisplay({
      returnStatusCode: "before_delivery",
      isReturnable: true,
      returnDeadlineDisplay: "۷ روز پس از تحویل",
      returnRemainingDisplay: "۷ روز پس از تحویل",
    }),
    "۷ روز پس از تحویل",
  );
  assert.equal(
    canonicalReturnDisplay({
      returnStatusCode: "eligible",
      isReturnable: true,
      returnDeadlineDisplay: "۱۴۰۵/۰۱/۲۰",
      returnRemainingDisplay: "۳ روز مانده",
    }),
    "۳ روز مانده",
  );
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /canonicalReturnDisplay\(line\)/);
  assert.doesNotMatch(panel, /returnDeadlineDisplay.*returnRemainingDisplay/);
});

test("qty input stays exact and pack-selected sends selections", () => {
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /admin-order-line-qty-/);
  assert.match(panel, /buildSelections\(seller, "pack"\)/);
  assert.match(panel, /orderLineId: lineId, quantity: qty/);
  assert.match(panel, /runSellerOp\(seller, "pack_selected"/);
  assert.match(panel, /runSellerOp\(seller, "mark_packed"\)/);
  assert.match(panel, /parseQuantityInput/);
  assert.match(panel, /step="any"/);
  assert.doesNotMatch(panel, /Math\.max\(1, cap.selectableQuantityMax/);
  assert.doesNotMatch(panel, /Math\.max\(1,/);
  assert.doesNotMatch(panel, /type="number"/);
});

test("fulfillment sequence errors map to FA", () => {
  assert.equal(mapAdminErrorMessage("fulfillment.pack.requires_processing", "fa"), "ابتدا پردازش را شروع کنید.");
  assert.equal(mapAdminErrorMessage("fulfillment.pack.not_processing", "fa"), "این قلم هنوز در مرحله پردازش نیست.");
  assert.equal(mapAdminErrorMessage("fulfillment.ship.not_packed", "fa"), "این قلم هنوز بسته‌بندی نشده است.");
  assert.equal(mapAdminErrorMessage("fulfillment.selection.qty_exceeded", "fa"), "تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.");
  assert.equal(mapAdminErrorMessage("fulfillment.bulk.incompatible", "fa"), "ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.");
  assert.equal(mapAdminErrorMessage("fulfillment.bulk.cross_seller", "fa"), "عملیات گروهی روی فروشندگان متفاوت مجاز نیست.");
  assert.equal(mapAdminErrorMessage("order.cancelled.blocks_action", "fa"), "سفارش لغوشده است؛ این عملیات مجاز نیست.");
});

test("panel preserves horizontal scroll and return deadline column", () => {
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /overflow-x-auto/);
  assert.match(panel, /min-w-\[920px\]/);
  assert.match(panel, /مهلت مرجوعی/);
  assert.match(panel, /admin-order-line-return-/);
  assert.match(panel, /admin-order-line-qty-/);
  assert.match(panel, /unpack/);
  assert.match(panel, /cancel_shipment/);
  assert.doesNotMatch(panel, /ReadyToFulfill/);
  assert.doesNotMatch(panel, /window\.prompt\(|window\.confirm\(|window\.alert\(/);
});

test("capability projection drives payment lock and start/pack row actions", () => {
  assert.equal(isPaymentLockedSeller({ status: "PendingPayment", paymentState: "PendingPayment" }), true);
  assert.equal(isPaymentLockedSeller({ status: "Paid", paymentState: "Paid", fulfillmentId: "f1" }), false);
  const locked = deriveLineCapability({
    paymentLocked: true,
    operationalStatus: "PendingPayment",
    packable: 2,
    unpackable: 0,
    shippable: 0,
    quantity: 2,
    projectedCodes: ["pack_selected", "mark_processing"],
  });
  assert.equal(locked.selectable, false);
  assert.deepEqual(locked.rowActionCodes, []);
  const ready = deriveLineCapability({
    paymentLocked: false,
    operationalStatus: "ReadyToFulfill",
    packable: 2,
    unpackable: 0,
    shippable: 0,
    quantity: 2,
    projectedCodes: ["mark_processing"],
  });
  assert.deepEqual(ready.rowActionCodes, ["mark_processing"]);
  assert.equal(ready.selectable, true);
  const processing = deriveLineCapability({
    paymentLocked: false,
    operationalStatus: "Processing",
    packable: 2,
    unpackable: 0,
    unprocessable: 2,
    shippable: 0,
    quantity: 2,
    projectedCodes: ["pack_selected", "mark_packed", "unprocess"],
  });
  assert.deepEqual(processing.rowActionCodes, ["pack_selected", "unprocess"]);
  const fractional = deriveLineCapability({
    paymentLocked: false,
    operationalStatus: "Processing",
    packable: 0.75,
    unpackable: 0,
    unprocessable: 0.75,
    shippable: 0,
    quantity: 1.25,
    projectedCodes: ["pack_selected", "unprocess"],
  });
  assert.equal(fractional.selectableQuantityMax, 0.75);
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /PAYMENT_LOCKED_BANNER_FA|admin-order-seller-payment-locked-/);
  assert.match(panel, /admin-order-seller-create-shipment-/);
  assert.equal(PAYMENT_LOCKED_BANNER_FA.includes("پرداخت این بخش از سفارش هنوز تأیید نشده"), true);
});

test("create shipment modal sends selections without deferred T005 block", () => {
  const modal = readFileSync(join(dir, "admin-create-shipment-modal.tsx"), "utf8");
  assert.match(modal, /selections/);
  assert.doesNotMatch(modal, /admin-create-shipment-deferred|به T005 موکول/);
});

test("mapAdminOrderDetail maps return deadline fields", () => {
  const detail = mapAdminOrderDetail({
    checkoutId: "c1",
    reference: "R1",
    sellerOrders: [{
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      lines: [{
        orderLineId: "ol1",
        offerId: "o1",
        productTitle: "کالا",
        quantity: 2,
        quantityPacked: 1,
        quantityShipped: 0,
        quantityAllocated: 0,
        unitAmount: 100,
        linePayable: 200,
        currency: "IRR",
        isReturnable: true,
        returnWindowDays: 7,
        returnDeadlineDisplay: "۷ روز پس از تحویل",
        returnRemainingDisplay: "۷ روز پس از تحویل",
        returnStatusCode: "before_delivery",
      }],
      shipments: [],
    }],
  });
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.quantityPacked, 1);
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.returnStatusCode, "before_delivery");
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.returnDeadlineDisplay, "۷ روز پس از تحویل");
});
