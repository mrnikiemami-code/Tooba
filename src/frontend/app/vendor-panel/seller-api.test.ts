import assert from "node:assert/strict";
import test from "node:test";
import {
  DEV_ACTOR_HEADER,
  formatMoney,
  formatOfferStatus,
  formatPaymentState,
  mapSellerDashboard,
  mapSellerOfferDetail,
  mapSellerOfferList,
  mapSellerOrderDetail,
  mapSellerOrderList,
  SELLER_PARTY_HEADER,
} from "./seller-api.ts";

test("seller offer list mapper keeps amount off Product price field", () => {
  const rows = mapSellerOfferList([
    {
      offerId: "o1",
      catalogVariantId: "v1",
      productId: "p1",
      productTitle: "پیراهن",
      sellerSku: "LIVE-A",
      status: "Active",
      amount: 1850000,
      currency: "IRR",
      availableUnits: 12,
    },
  ]);
  assert.equal(rows[0]?.offerId, "o1");
  assert.equal(rows[0]?.amount, 1850000);
  assert.equal(rows[0]?.sellerSku, "LIVE-A");
  assert.equal("price" in (rows[0] ?? {}), false);
  assert.equal("stock" in (rows[0] ?? {}), false);
});

test("seller offer detail preserves catalog read-only seam", () => {
  const detail = mapSellerOfferDetail({
    offerId: "o1",
    sellerPartyId: "s1",
    sellerDisplayName: "آرمان",
    catalogVariantId: "v1",
    productId: "p1",
    productTitle: "پیراهن",
    brandName: "برند",
    sellerSku: "LIVE-A",
    status: "Active",
    channel: "Marketplace",
    amount: 10,
    currency: "IRR",
    onHand: 5,
    reserved: 1,
    availableUnits: 4,
    catalogReadOnly: true,
  });
  assert.ok(detail);
  assert.equal(detail?.catalogReadOnly, true);
  assert.equal(detail?.availableUnits, 4);
});

test("seller order list and detail keep seller slice only", () => {
  const rows = mapSellerOrderList([
    {
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      submittedAt: "2026-08-24T00:00:00Z",
      recipientName: "علی",
      lineCount: 2,
      payableAmount: 100,
      currency: "IRR",
      paymentState: "Paid",
      status: "Paid",
    },
  ]);
  assert.equal(rows[0]?.sellerOrderId, "so1");
  assert.equal(rows[0]?.lineCount, 2);

  const detail = mapSellerOrderDetail({
    sellerOrderId: "so1",
    orderNumber: "SO-1",
    sellerPartyId: "s1",
    submittedAt: "2026-08-24T00:00:00Z",
    status: "Paid",
    paymentState: "Paid",
    subtotal: 90,
    taxAmount: 10,
    discountAmount: 0,
    payableAmount: 100,
    currency: "IRR",
    recipientName: "علی",
    contactMobile: "0912",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان",
    postalCode: "1",
    shippingMethodLabel: "پست",
    lines: [{ offerId: "o1", title: "پیراهن", quantity: 1, unitAmount: 90, linePayable: 100, currency: "IRR" }],
  });
  assert.ok(detail);
  assert.equal(detail?.lines.length, 1);
  assert.equal(detail?.sellerPartyId, "s1");
});

test("dashboard mapper and seller header constant", () => {
  const summary = mapSellerDashboard({
    sellerPartyId: "s1",
    sellerDisplayName: "آرمان",
    activeOffers: 1,
    openOrders: 2,
    paidOrders: 3,
  });
  assert.equal(summary?.activeOffers, 1);
  assert.equal(SELLER_PARTY_HEADER, "X-Tooba-Seller-Party-Id");
  assert.equal(DEV_ACTOR_HEADER, "X-Tooba-Dev-Actor-User-Id");
});

test("persian operator formatting for status and money", () => {
  assert.equal(formatOfferStatus("Active"), "فعال");
  assert.equal(formatPaymentState("PendingPayment"), "در انتظار پرداخت");
  assert.equal(formatPaymentState("Paid"), "پرداخت‌شده");
  assert.match(formatMoney(1850000, "IRR"), /ریال/);
});

test("authorized route context keeps actor distinct from seller party", () => {
  assert.notEqual(DEV_ACTOR_HEADER, SELLER_PARTY_HEADER);
  assert.equal(formatOfferStatus("Suspended"), "معلق");
});
