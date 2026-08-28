import assert from "node:assert/strict";
import test from "node:test";
import {
  ADMIN_DEV_ACTOR_HEADER,
  formatAdminMoney,
  formatAdminStatus,
  loadAdminOrders,
  mapAdminCustomers,
  mapAdminDashboard,
  mapAdminOrder,
  mapAdminOrderDetail,
  mapAdminSellers,
  loadAdminReviews,
  mapAdminReviews,
  moderateAdminReview,
} from "./admin-api.ts";

test("maps live dashboard, order, seller and customer DTOs", () => {
  const dashboard = mapAdminDashboard({ ActiveProducts: 2, ActiveOffers: 3, OpenOrders: 4, PaidOrders: 1, PendingOrders: 3, SellersCount: 5, CustomersCount: 6 });
  assert.equal(dashboard?.activeOffers, 3);
  assert.equal(dashboard?.customersCount, 6);

  const order = mapAdminOrder({ CheckoutId: "c1", Reference: "TOOBA-101", RecipientName: "سارا", SellerCount: 2, ItemCount: 3, PayableAmount: 1200, Currency: "IRR", PaymentState: "Paid", Status: "Submitted" });
  assert.equal(order?.reference, "TOOBA-101");
  assert.equal(order?.lineCount, 3);

  assert.equal(mapAdminSellers([{ SellerPartyId: "s1", SellerDisplayName: "فروشگاه آرمان", ActiveOffers: 7 }])[0]?.activeOfferCount, 7);
  assert.equal(mapAdminCustomers([{ ActorUserId: "u1", DisplayName: "مینا", OrderCount: 4, LastOrderAt: "2026-08-25T00:00:00Z" }])[0]?.orderCount, 4);
});

test("maps review moderation rows without internal actor data", () => {
  const page = mapAdminReviews({
    Reviews: [{
      ReviewId: "r1", AuthorDisplayName: "سارا", ProductTitle: "پیراهن",
      Rating: 4, Body: "نظر واقعی مشتری", VerifiedPurchase: true,
      Status: "Pending", CreatedAt: "2026-08-25T00:00:00Z", ActorUserId: "private",
    }],
    Page: 1, PageSize: 20, TotalCount: 1,
  });
  assert.equal(page?.rows[0]?.reviewerDisplayName, "سارا");
  assert.equal(page?.rows[0]?.verifiedPurchase, true);
  assert.equal("ActorUserId" in (page?.rows[0] ?? {}), false);
});

test("review list and moderation expose server denied", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () => new Response(null, { status: 403 })) as typeof fetch;
  try {
    assert.equal((await loadAdminReviews()).state, "denied");
    assert.equal((await moderateAdminReview("r1", "publish")).state, "denied");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("maps admin order detail seller snapshots", () => {
  const detail = mapAdminOrderDetail({
    checkoutId: "c1",
    reference: "TOOBA-101",
    payableAmount: 500,
    sellerOrders: [{
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      lines: [{ offerId: "o1", title: "پیراهن", quantity: 2, unitAmount: 200, linePayable: 400 }],
    }],
  });
  assert.equal(detail?.sellerOrders[0]?.sellerDisplayName, "آرمان");
  assert.equal(detail?.sellerOrders[0]?.lines[0]?.quantity, 2);
});

test("uses Persian money and status labels", () => {
  assert.equal(formatAdminStatus("PendingPayment"), "در انتظار پرداخت");
  assert.equal(formatAdminStatus("Archived"), "بایگانی");
  assert.equal(formatAdminStatus("Delivered"), "تحویل‌شده");
  assert.equal(formatAdminStatus("Mixed"), "ترکیبی");
  assert.match(formatAdminMoney(125000, "IRR"), /ریال$/);
});

test("sends development actor header and exposes denied state", async () => {
  const originalFetch = globalThis.fetch;
  let capturedHeaders: HeadersInit | undefined;
  globalThis.fetch = (async (_input: RequestInfo | URL, init?: RequestInit) => {
    capturedHeaders = init?.headers;
    return new Response(null, { status: 403 });
  }) as typeof fetch;
  try {
    const result = await loadAdminOrders();
    assert.equal(result.state, "denied");
    assert.equal(result.status, 403);
    assert.ok(capturedHeaders && ADMIN_DEV_ACTOR_HEADER in (capturedHeaders as Record<string, string>));
  } finally {
    globalThis.fetch = originalFetch;
  }
});
