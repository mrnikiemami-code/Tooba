import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminOrderDetail } from "./admin-api.ts";
import {
  compatibleBulkCodes,
  MIXED_SELECTION_MESSAGE_FA,
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
    pack: "بسته‌بندی همه اقلام آماده",
    createShipment: "ایجاد مرسوله",
    unpack: "بازگشت از بسته‌بندی",
    dispatch: "ارسال",
  });
  assert.deepEqual(sellerQuickActionLabels(true), {
    pack: "بسته‌بندی انتخاب‌شده‌ها",
    createShipment: "ایجاد مرسوله از انتخاب‌شده‌ها",
    unpack: "بازگشت از بسته‌بندی انتخاب‌شده‌ها",
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
  assert.match(panel, /rowActionsForLine/);
  assert.match(panel, /admin-order-line-kebab-/);
  assert.match(panel, /lineActions\.length === 0 \? null/);
  assert.doesNotMatch(panel, /title="عملیات ردیف از نوار فروشنده و کارت مرسوله"/);
  assert.match(panel, /pack_selected/);
  assert.match(panel, /mark_processing/);
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
  assert.equal(MIXED_SELECTION_MESSAGE_FA, "ردیف‌های انتخاب‌شده در وضعیت‌های متفاوت یا ناسازگار هستند.");
});

test("qty input stays exact and pack-selected sends selections", () => {
  const panel = readFileSync(join(dir, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /admin-order-line-qty-/);
  assert.match(panel, /buildSelections\(seller, "pack"\)/);
  assert.match(panel, /orderLineId: lineId, quantity: qty/);
  assert.match(panel, /runSellerOp\(seller, "pack_selected"/);
  assert.match(panel, /runSellerOp\(seller, "mark_packed"\)/);
});

test("fulfillment sequence errors map to FA", () => {
  assert.equal(mapAdminErrorMessage("fulfillment.pack.requires_processing", "fa"), "ابتدا پردازش را شروع کنید.");
  assert.equal(mapAdminErrorMessage("fulfillment.pack.not_processing", "fa"), "این قلم هنوز در مرحله پردازش نیست.");
  assert.equal(mapAdminErrorMessage("fulfillment.ship.not_packed", "fa"), "این قلم هنوز بسته‌بندی نشده است.");
  assert.equal(mapAdminErrorMessage("fulfillment.selection.qty_exceeded", "fa"), "تعداد انتخاب‌شده بیشتر از تعداد قابل عملیات است.");
  assert.equal(mapAdminErrorMessage("fulfillment.bulk.incompatible", "fa"), "ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.");
  assert.equal(mapAdminErrorMessage("fulfillment.bulk.cross_seller", "fa"), "عملیات گروهی روی فروشندگان متفاوت مجاز نیست.");
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
