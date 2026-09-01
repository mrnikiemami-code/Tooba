import assert from "node:assert/strict";
import test from "node:test";
import {
  ADMIN_DEV_ACTOR_HEADER,
  formatAdminMoney,
  formatAdminPaymentProvider,
  formatAdminPaymentReference,
  formatAdminStatus,
  loadAdminOrders,
  mapAdminCustomers,
  mapAdminDashboard,
  mapAdminOrder,
  formatOrderSellerLabel,
  enrichAdminOrderDetail,
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

  const order = mapAdminOrder({ CheckoutId: "c1", Reference: "TOOBA-101", RecipientName: "سارا", SellerCount: 2, SellerDisplayNames: "فروشگاه آرمان", ItemCount: 3, PayableAmount: 1200, Currency: "IRR", PaymentState: "Paid", Status: "Submitted" });
  assert.equal(order?.reference, "TOOBA-101");
  assert.equal(order?.sellerDisplayNames, "فروشگاه آرمان");
  assert.equal(formatOrderSellerLabel({ sellerCount: 1, sellerDisplayNames: "فروشگاه آرمان" }), "فروشگاه آرمان");
  assert.equal(formatOrderSellerLabel({ sellerCount: 3, sellerDisplayNames: "3 فروشنده" }), "۳ فروشنده");
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
    lineCount: 2,
    sellerCount: 1,
    payableAmount: 500,
    sellerOrders: [{
      sellerOrderId: "so1",
      orderNumber: "SO-1",
      sellerDisplayName: "آرمان",
      lines: [{ offerId: "o1", title: "پیراهن", quantity: 2, unitAmount: 200, linePayable: 400 }],
    }],
    sellerFinancials: [{
      sellerOrderId: "so1",
      sellerPartyId: "sp1",
      sellerDisplayName: "آرمان",
      lineCount: 2,
      grossAmount: 400,
      commissionAmount: 8,
      payableAmount: 392,
      currency: "IRR",
      settlementStatus: "Settled",
    }],
    financialSummary: {
      totalSellerShare: 400,
      totalCommission: 8,
      grossOrderProfit: 8,
      payableToSellers: 392,
      customerGrossAmount: 400,
      shippingCost: 0,
      customerDiscounts: 0,
      totalReceivedFromCustomer: 500,
      currency: "IRR",
    },
    financialEvents: [],
  });
  assert.equal(detail?.sellerOrders[0]?.sellerDisplayName, "آرمان");
  assert.equal(detail?.lineCount, 2);
  assert.equal(detail?.sellerFinancials[0]?.settlementStatus, "Settled");
  assert.equal(detail?.financialSummary.payableToSellers, 392);
});

test("enriches legacy order detail payload without finance projections", () => {
  const detail = mapAdminOrderDetail({
    checkoutId: "c2",
    reference: "TOOBA-202",
    status: "Paid",
    paymentState: "Paid",
    subtotal: 350000,
    payableAmount: 381500,
    currency: "IRR",
    recipientName: "سارا",
    sellerOrders: [{
      sellerOrderId: "so2",
      orderNumber: "SO-2",
      sellerDisplayName: "فروشگاه آرمان",
      paymentState: "Paid",
      payableAmount: 381500,
      currency: "IRR",
      lines: [{ offerId: "o2", productTitle: "کالا", quantity: 1, unitAmount: 350000, linePayable: 381500, currency: "IRR" }],
    }],
    payment: {
      paymentId: "p1",
      checkoutId: "c2",
      status: "Succeeded",
      amount: 381500,
      currency: "IRR",
      providerCode: "wallet",
      providerRequestReference: "w|aeb80a42139f425ea7b5204a9a410b80|aaaaaaaaaaaa4aaa8aaa000000000009|381500|IRR",
      providerTransactionReference: "wallet:aeb80a42-139f-425e-a7b5-204a9a410b80",
      createdAt: "2026-08-27T21:58:36.445497+00:00",
      updatedAt: "2026-08-27T21:58:36.474787+00:00",
      completedAt: "2026-08-27T21:58:36.474787+00:00",
      reconcileEligible: false,
    },
  });
  assert.equal(detail?.lineCount, 1);
  assert.equal(detail?.sellerCount, 1);
  assert.equal(detail?.sellerFinancials.length, 1);
  assert.equal(detail?.sellerFinancials[0]?.settlementStatus, "WaitingForSettlement");
  assert.equal(detail?.financialSummary.totalReceivedFromCustomer, 381500);
  assert.equal(detail?.financialEvents.length, 1);
  assert.equal(detail?.financialEvents[0]?.paymentMethod, "کیف پول");
  assert.equal(formatAdminPaymentProvider("wallet"), "کیف پول");
  assert.match(formatAdminPaymentReference(detail!.payment!), /wallet:/);
});

test("enrichAdminOrderDetail keeps explicit server projections", () => {
  const enriched = enrichAdminOrderDetail({
    checkoutId: "c3",
    reference: "TOOBA-303",
    createdAt: "2026-08-27T00:00:00Z",
    status: "Paid",
    paymentState: "Paid",
    lineCount: 4,
    sellerCount: 2,
    subtotal: 1000,
    taxAmount: 0,
    discountAmount: 0,
    payableAmount: 1000,
    currency: "IRR",
    recipientName: "مینا",
    contactMobile: "",
    provinceName: "",
    cityName: "",
    postalAddress: "",
    postalCode: "",
    shippingMethodLabel: "",
    sellerOrders: [],
    sellerFinancials: [{ sellerOrderId: "so3", sellerPartyId: "sp3", sellerDisplayName: "آ", lineCount: 4, grossAmount: 1000, commissionAmount: 20, payableAmount: 980, currency: "IRR", settlementStatus: "Settled" }],
    financialEvents: [],
    financialSummary: { totalSellerShare: 1000, totalCommission: 20, grossOrderProfit: 20, payableToSellers: 980, customerGrossAmount: 1000, shippingCost: 0, customerDiscounts: 0, totalReceivedFromCustomer: 1000, currency: "IRR" },
    payment: null,
  });
  assert.equal(enriched.lineCount, 4);
  assert.equal(enriched.sellerCount, 2);
  assert.equal(enriched.sellerFinancials[0]?.commissionAmount, 20);
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
