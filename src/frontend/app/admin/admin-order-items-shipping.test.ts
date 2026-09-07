import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapAdminOrderDetail } from "./admin-api.ts";
import { sellerQuickActionLabels } from "./admin-order-operations-scope.ts";

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
    dispatch: "ارسال",
  });
  assert.deepEqual(sellerQuickActionLabels(true), {
    pack: "بسته‌بندی انتخاب‌شده‌ها",
    createShipment: "ایجاد مرسوله از انتخاب‌شده‌ها",
    dispatch: "ارسال انتخاب‌شده‌ها",
  });
});
