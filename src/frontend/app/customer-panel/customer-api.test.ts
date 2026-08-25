import assert from "node:assert/strict";
import test from "node:test";
import {
  CUSTOMER_DEV_ACTOR_HEADER,
  customerStatusClasses,
  formatCustomerOrderStatus,
  mapCustomerDashboard,
  mapCustomerOrder,
  mapCustomerOrderDetail,
  mapCustomerProfile,
} from "./customer-api.ts";
import {
  addWishlistProduct,
  mapWishlistPage,
  StorefrontWishlistApiError,
  wishlistEmptyMessage,
} from "../storefront/storefront-wishlist-api.ts";

test("customer mapper keeps checkout identity and snapshot amount", () => {
  const order = mapCustomerOrder({
    checkoutId: "checkout-1",
    reference: "ORD-1",
    payableAmount: 1_850_000,
    currency: "IRR",
    paymentState: "Paid",
  });
  assert.equal(order?.checkoutId, "checkout-1");
  assert.equal(order?.payableAmount, 1_850_000);
  assert.equal("price" in (order ?? {}), false);
});

test("customer payment presentation preserves backend pending paid and failed states", () => {
  assert.equal(formatCustomerOrderStatus("PendingPayment"), "در انتظار پرداخت");
  assert.equal(formatCustomerOrderStatus("Paid"), "پرداخت‌شده");
  assert.equal(formatCustomerOrderStatus("Failed"), "پرداخت ناموفق");
  assert.match(customerStatusClasses("Failed"), /red/);
});

test("customer dashboard exposes capability availability without fake counts", () => {
  const page = mapCustomerDashboard({
    actorUserId: "actor-1",
    displayName: "مشتری",
    totalOrders: 2,
    pendingOrders: 1,
    paidOrders: 1,
    wishlistAvailable: false,
    wishlistCount: 3,
    addressBookAvailable: false,
    recentOrders: [],
  });
  assert.equal(page?.wishlistAvailable, false);
  assert.equal(page?.wishlistCount, 3);
  assert.equal(page?.addressBookAvailable, false);
  assert.equal(page?.totalOrders, 2);
});

test("wishlist maps current live offer availability and real ratings only", () => {
  const page = mapWishlistPage({
    totalCount: 1,
    items: [{
      savedAt: "2026-08-25T12:00:00Z",
      product: {
        productId: "product-1",
        slug: "live-product",
        title: "کالای زنده",
        primaryOfferId: "offer-live",
        offerAmountExclusiveOfTax: 250_000,
        promotionalAmountExclusiveOfTax: 220_000,
        availableUnits: 4,
        inStock: true,
        reviewCount: 2,
        averageRating: 4.5,
      },
    }],
  });
  assert.equal(page?.items[0]?.productId, "product-1");
  assert.equal(page?.items[0]?.card.primaryOfferId, "offer-live");
  assert.equal(page?.items[0]?.card.offerAmountExclusiveOfTax, 250_000);
  assert.equal(page?.items[0]?.card.inStock, true);
  assert.equal(page?.items[0]?.card.averageRating, 4.5);

  const unrated = mapWishlistPage([{ productId: "p2", slug: "p2", reviewCount: 0, averageRating: 5 }]);
  assert.equal(unrated?.items[0]?.card.averageRating, null);
});

test("wishlist empty helper only describes an actual empty collection", () => {
  assert.match(wishlistEmptyMessage(0) ?? "", /هنوز/);
  assert.equal(wishlistEmptyMessage(1), null);
});

test("wishlist toggle sends ProductId and surfaces 401 without optimistic success", async () => {
  const originalFetch = globalThis.fetch;
  let requestedUrl = "";
  globalThis.fetch = (async (input: string | URL | Request) => {
    requestedUrl = String(input);
    return new Response(null, { status: 401 });
  }) as typeof fetch;
  try {
    await assert.rejects(
      () => addWishlistProduct("product-intent"),
      (error: unknown) => error instanceof StorefrontWishlistApiError && error.status === 401,
    );
    assert.equal(requestedUrl, "/v1/customer/wishlist/product-intent");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("customer detail maps seller and shipping snapshots", () => {
  const page = mapCustomerOrderDetail({
    checkoutId: "checkout-1",
    reference: "ORD-1",
    paymentState: "Failed",
    recipientName: "مشتری",
    postalAddress: "تهران",
    sellerOrders: [{
      sellerOrderId: "seller-order-1",
      sellerDisplayName: "فروشگاه آرمان",
      lines: [{ offerId: "offer-1", title: "کالا", quantity: 2, linePayable: 200 }],
    }],
  });
  assert.equal(page?.sellerOrders[0]?.sellerDisplayName, "فروشگاه آرمان");
  assert.equal(page?.sellerOrders[0]?.lines[0]?.quantity, 2);
  assert.equal(page?.postalAddress, "تهران");
  assert.equal(page?.paymentState, "Failed");
});

test("customer profile remains read-only when backend has no write capability", () => {
  const page = mapCustomerProfile({ actorUserId: "actor-1", displayName: "مشتری", editable: false });
  assert.equal(page?.editable, false);
  assert.equal(CUSTOMER_DEV_ACTOR_HEADER, "X-Tooba-Dev-Actor-User-Id");
});
