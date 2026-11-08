/**
 * @vitest-environment jsdom
 */
import assert from "node:assert/strict";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import test from "node:test";
import {
  mapAdminOrderDetail,
  type AdminOrderDetail,
} from "./admin-api.ts";
import { AdminOrderOperationalDeliverySection } from "./admin-order-operational-delivery-section.tsx";

function baseDetail(overrides: Partial<AdminOrderDetail> = {}): AdminOrderDetail {
  const mapped = mapAdminOrderDetail({
    checkoutId: "c1",
    reference: "R1",
    currency: "IRR",
    sellerOrders: [
      {
        sellerOrderId: "so1",
        orderNumber: "SO-1",
        sellerDisplayName: "آرمان",
        lines: [{
          orderLineId: "ol1",
          offerId: "o1",
          productTitle: "شال آرمان",
          quantity: 2,
          quantityPacked: 2,
          quantityAllocated: 0,
          unitAmount: 1000,
          linePayable: 2000,
          currency: "IRR",
        }],
        shipments: [],
      },
      {
        sellerOrderId: "so2",
        orderNumber: "SO-2",
        sellerDisplayName: "نیلوا",
        lines: [{
          orderLineId: "ol2",
          offerId: "o2",
          productTitle: "پیراهن نیلوا",
          quantity: 1,
          quantityPacked: 1,
          quantityAllocated: 0,
          unitAmount: 3000,
          linePayable: 3000,
          currency: "IRR",
        }],
        shipments: [],
      },
    ],
    shippingGroups: [
      {
        shippingGroupId: "g1",
        sellerOrderId: "so1",
        sellerPartyId: "sp1",
        methodCode: "post",
        methodLabel: "پیشتاز",
        serviceCode: null,
        optionCode: null,
        shippingChargeSnapshot: 125000,
        currency: "IRR",
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        deliveryTimeZone: "Asia/Tehran",
        ownerKind: "Marketplace",
        ownerTenantKey: null,
        ownerSellerPartyId: null,
        orderLineIds: ["ol1"],
        createParcelEligible: true,
        createParcelBlockReasonFa: null,
        payableAtCheckoutAmount: 125000,
        carrierCollectAmount: 0,
        transportMode: "Manual",
      },
      {
        shippingGroupId: "g2",
        sellerOrderId: "so2",
        sellerPartyId: "sp2",
        methodCode: "post",
        methodLabel: "پیشتاز",
        serviceCode: null,
        optionCode: null,
        shippingChargeSnapshot: 125000,
        currency: "IRR",
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        deliveryTimeZone: "Asia/Tehran",
        ownerKind: "Marketplace",
        ownerTenantKey: null,
        ownerSellerPartyId: null,
        orderLineIds: ["ol2"],
        createParcelEligible: true,
        createParcelBlockReasonFa: null,
        payableAtCheckoutAmount: 125000,
        carrierCollectAmount: 0,
        transportMode: "Manual",
      },
    ],
    operationalDeliveryGroups: [
      {
        operationalGroupKey: "mp-post-combined",
        methodLabel: "پیشتاز",
        methodCode: "post",
        serviceCode: null,
        optionCode: null,
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        responsibilityLabelFa: "ارسال توسط مارکت‌پلیس",
        isCrossSellerMarketplace: true,
        separateReasonFa: null,
        consolidateEligible: true,
        consolidateBlockReasonFa: null,
        combinedFeeTotal: 250000,
        currency: "IRR",
        members: [
          {
            shippingGroupId: "g1",
            sellerOrderId: "so1",
            sellerPartyId: "sp1",
            sellerDisplayName: "آرمان",
            orderLineIds: ["ol1"],
            createParcelEligible: true,
            createParcelBlockReasonFa: null,
            transportMode: "Manual",
            feeAmount: 125000,
            currency: "IRR",
          },
          {
            shippingGroupId: "g2",
            sellerOrderId: "so2",
            sellerPartyId: "sp2",
            sellerDisplayName: "نیلوا",
            orderLineIds: ["ol2"],
            createParcelEligible: true,
            createParcelBlockReasonFa: null,
            transportMode: "Manual",
            feeAmount: 125000,
            currency: "IRR",
          },
        ],
        feeBreakdown: [
          { shippingGroupId: "g1", sellerDisplayName: "آرمان", feeAmount: 125000, currency: "IRR" },
          { shippingGroupId: "g2", sellerDisplayName: "نیلوا", feeAmount: 125000, currency: "IRR" },
        ],
      },
    ],
  });
  assert.ok(mapped);
  return { ...mapped!, ...overrides };
}

test("mounted marketplace operational card shows both sellers under one heading", () => {
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail: baseDetail(),
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
    }),
  );
  assert.match(html, /ارسال سفارش/);
  assert.match(html, /admin-order-operational-group-mp-post-combined/);
  assert.match(html, /data-cross-seller="true"/);
  assert.match(html, /آرمان/);
  assert.match(html, /نیلوا/);
  assert.match(html, /شال آرمان/);
  assert.match(html, /پیراهن نیلوا/);
  assert.match(html, /جمع تعهدهای این گروه/);
  assert.doesNotMatch(html, /store-alpha|Asia\/Tehran|GUID|tenant/i);
});

test("mounted seller-owned cards stay independent", () => {
  const detail = baseDetail({
    operationalDeliveryGroups: [
      {
        operationalGroupKey: "seller-a",
        methodLabel: "پیشتاز",
        methodCode: "post",
        serviceCode: null,
        optionCode: null,
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "14:00–15:00",
        responsibilityLabelFa: "ارسال توسط فروشنده",
        isCrossSellerMarketplace: false,
        separateReasonFa: "ارسال بر عهده فروشنده است و با فروشندگان دیگر تجمیع نمی‌شود.",
        consolidateEligible: false,
        consolidateBlockReasonFa: "تجمیع فقط برای ارسال مارکت‌پلیس سازگار مجاز است.",
        combinedFeeTotal: 125000,
        currency: "IRR",
        members: [{
          shippingGroupId: "g1",
          sellerOrderId: "so1",
          sellerPartyId: "sp1",
          sellerDisplayName: "آرمان",
          orderLineIds: ["ol1"],
          createParcelEligible: true,
          createParcelBlockReasonFa: null,
          transportMode: "Manual",
          feeAmount: 125000,
          currency: "IRR",
        }],
        feeBreakdown: [
          { shippingGroupId: "g1", sellerDisplayName: "آرمان", feeAmount: 125000, currency: "IRR" },
        ],
      },
      {
        operationalGroupKey: "seller-b",
        methodLabel: "پیشتاز",
        methodCode: "post",
        serviceCode: null,
        optionCode: null,
        deliveryDate: "2026-11-10",
        deliveryTimeWindow: "16:00–17:00",
        responsibilityLabelFa: "ارسال توسط فروشنده",
        isCrossSellerMarketplace: false,
        separateReasonFa: "ارسال بر عهده فروشنده است و با فروشندگان دیگر تجمیع نمی‌شود.",
        consolidateEligible: false,
        consolidateBlockReasonFa: "تجمیع فقط برای ارسال مارکت‌پلیس سازگار مجاز است.",
        combinedFeeTotal: 125000,
        currency: "IRR",
        members: [{
          shippingGroupId: "g2",
          sellerOrderId: "so2",
          sellerPartyId: "sp2",
          sellerDisplayName: "نیلوا",
          orderLineIds: ["ol2"],
          createParcelEligible: true,
          createParcelBlockReasonFa: null,
          transportMode: "Manual",
          feeAmount: 125000,
          currency: "IRR",
        }],
        feeBreakdown: [
          { shippingGroupId: "g2", sellerDisplayName: "نیلوا", feeAmount: 125000, currency: "IRR" },
        ],
      },
    ],
  });
  const html = renderToStaticMarkup(
    createElement(AdminOrderOperationalDeliverySection, {
      detail,
      pendingCode: null,
      selectedBySeller: {},
      qtyByLine: {},
      shippableQty: () => 1,
      onCreateShipment: () => undefined,
    }),
  );
  assert.match(html, /admin-order-operational-group-seller-a/);
  assert.match(html, /admin-order-operational-group-seller-b/);
  assert.match(html, /16:00–17:00/);
  assert.equal((html.match(/data-cross-seller="false"/g) ?? []).length, 2);
});

test("panel source mounts operational delivery section before seller packing", async () => {
  const { readFileSync } = await import("node:fs");
  const { dirname, join } = await import("node:path");
  const { fileURLToPath } = await import("node:url");
  const panel = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), "admin-order-items-shipping-panel.tsx"),
    "utf8",
  );
  const opIdx = panel.indexOf("AdminOrderOperationalDeliverySection");
  const sellerIdx = panel.indexOf("detail.sellerOrders.map");
  assert.ok(opIdx > 0);
  assert.ok(sellerIdx > opIdx);
  assert.match(panel, /AdminOrderOperationalDeliverySection/);
});
