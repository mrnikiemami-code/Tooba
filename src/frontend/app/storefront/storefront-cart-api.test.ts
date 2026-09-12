import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontCart, toCustomerCartMessage, StorefrontCartApiError } from "./storefront-cart-api.ts";

test("cart mapper keeps offer identity and ignores product price fields", () => {
  const cart = mapStorefrontCart({
    cartId: "cart-1",
    version: 4,
    market: "IR",
    currency: "IRR",
    channel: "Marketplace",
    itemCount: 2,
    subtotalExclusiveOfTax: 3580000,
    guestSecret: "once",
    price: 1,
    lines: [
      {
        lineId: "line-1",
        offerId: "offer-primary",
        catalogVariantId: "variant-1",
        sellerPartyId: "seller-1",
        productId: "product-1",
        productSlug: "linen-shirt",
        title: "پیراهن",
        sellerDisplayName: "دیجی‌استایل نمونه",
        quantity: 2,
        unitAmountExclusiveOfTax: 1790000,
        lineAmountExclusiveOfTax: 3580000,
        currency: "IRR",
        quotedTaxExclusive: true,
        price: 99,
      },
    ],
  });
  assert.equal(cart?.cartId, "cart-1");
  assert.equal(cart?.lines[0]?.offerId, "offer-primary");
  assert.equal(cart?.subtotalExclusiveOfTax, 3580000);
  assert.equal("price" in (cart?.lines[0] ?? {}), false);
});

test("bootstrapCartSessionFromQuery writes session when both ids present", async () => {
  const { bootstrapCartSessionFromQuery, readCartSession, clearCartSession } = await import("./storefront-cart-api.ts");
  const store = new Map<string, string>();
  const sessionStorage: Storage = {
    get length() {
      return store.size;
    },
    clear() {
      store.clear();
    },
    key(index) {
      return Array.from(store.keys())[index] ?? null;
    },
    getItem(k) {
      return store.has(k) ? store.get(k)! : null;
    },
    setItem(k, v) {
      store.set(k, String(v));
    },
    removeItem(k) {
      store.delete(k);
    },
  };
  (globalThis as { window?: Window }).window = {
    sessionStorage,
    dispatchEvent: () => true,
  } as unknown as Window;
  clearCartSession();
  assert.equal(bootstrapCartSessionFromQuery({ cartId: null, guestSecret: "x" }), false);
  assert.equal(bootstrapCartSessionFromQuery({ cartId: "c1", guestSecret: "s1" }), true);
  assert.deepEqual(readCartSession(), { cartId: "c1", guestSecret: "s1" });
});

test("customer cart message hides Held reservation wording", () => {
  const hidden = toCustomerCartMessage(
    new StorefrontCartApiError(409, "cart.inventory.stale", "فقط رزرو Held قابل آزادسازی یا مصرف است."),
  );
  assert.equal(hidden.includes("Held"), false);
  assert.match(hidden, /موجودی/);
});

test("cart mapper defaults missing status to Active", () => {
  const cart = mapStorefrontCart({
    cartId: "cart-status",
    version: 1,
    lines: [],
  });
  assert.equal(cart?.status, "Active");
  const converted = mapStorefrontCart({
    cartId: "cart-converted",
    version: 8,
    status: "Converted",
    lines: [],
  });
  assert.equal(converted?.status, "Converted");
});

function mockSession() {
  const store = new Map<string, string>();
  const sessionStorage: Storage = {
    get length() {
      return store.size;
    },
    clear() {
      store.clear();
    },
    key(index) {
      return Array.from(store.keys())[index] ?? null;
    },
    getItem(k) {
      return store.has(k) ? store.get(k)! : null;
    },
    setItem(k, v) {
      store.set(k, String(v));
    },
    removeItem(k) {
      store.delete(k);
    },
  };
  (globalThis as { window?: Window }).window = {
    sessionStorage,
    dispatchEvent: () => true,
  } as unknown as Window;
  return store;
}

test("committed checkout proofs are keyed by checkout and survive active cart detach", async () => {
  const {
    writeCartSession,
    clearCartSession,
    readCartSession,
    writeCommittedCheckoutProof,
    readCommittedCheckoutProof,
    persistCommittedCheckoutAndDetachActiveCart,
    resolveCommittedCheckoutAccess,
    resolvePaymentResultAccess,
  } = await import("./storefront-cart-api.ts");
  mockSession();
  writeCartSession("old-cart", "old-secret");
  persistCommittedCheckoutAndDetachActiveCart("chk-a", "old-cart");
  assert.deepEqual(readCartSession(), { cartId: null, guestSecret: null });
  assert.equal(readCommittedCheckoutProof("chk-a")?.guestSecret, "old-secret");
  writeCommittedCheckoutProof({
    checkoutId: "chk-b",
    cartId: "cart-b",
    guestSecret: "secret-b",
    storeKey: "storefront",
  });
  assert.equal(readCommittedCheckoutProof("chk-a")?.cartId, "old-cart");
  assert.equal(readCommittedCheckoutProof("chk-b")?.cartId, "cart-b");
  writeCartSession("new-cart", "new-secret");
  const accessA = resolveCommittedCheckoutAccess("chk-a");
  assert.deepEqual(accessA, { cartId: "old-cart", guestSecret: "old-secret" });
  assert.notEqual(accessA.cartId, readCartSession().cartId);
  assert.deepEqual(resolvePaymentResultAccess("pay-missing"), { cartId: null, guestSecret: null });
});

test("ensureStorefrontCart rotates Converted cart and never posts lines to it", async () => {
  const { writeCartSession, ensureStorefrontCart, addOfferToCart, readCartSession } = await import(
    "./storefront-cart-api.ts"
  );
  mockSession();
  writeCartSession("converted-cart", "old-secret");
  const calls: Array<{ url: string; method: string }> = [];
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async (input: string | URL | Request, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    calls.push({ url, method });
    if (url.includes("/cart/converted-cart") && method === "GET") {
      return new Response(
        JSON.stringify({
          cartId: "converted-cart",
          version: 8,
          status: "Converted",
          lines: [],
          guestSecret: "old-secret",
        }),
        { status: 200 },
      );
    }
    if (url.includes("/cart/fresh-cart") && method === "GET") {
      return new Response(
        JSON.stringify({
          cartId: "fresh-cart",
          version: 1,
          status: "Active",
          lines: [],
          guestSecret: "new-secret",
        }),
        { status: 200 },
      );
    }
    if (url.endsWith("/v1/storefront/cart") && method === "POST") {
      return new Response(
        JSON.stringify({
          cartId: "fresh-cart",
          version: 1,
          status: "Active",
          lines: [],
          guestSecret: "new-secret",
        }),
        { status: 200 },
      );
    }
    if (url.includes("/cart/fresh-cart/lines") && method === "POST") {
      return new Response(
        JSON.stringify({
          cartId: "fresh-cart",
          version: 2,
          status: "Active",
          lines: [{ lineId: "l1", offerId: "o1", quantity: 1 }],
          guestSecret: "new-secret",
        }),
        { status: 200 },
      );
    }
    return new Response(JSON.stringify({ errorCode: "cart.rejected" }), { status: 400 });
  }) as typeof fetch;
  try {
    const rotated = await ensureStorefrontCart();
    assert.equal(rotated.cartId, "fresh-cart");
    assert.equal(rotated.status, "Active");
    assert.deepEqual(readCartSession(), { cartId: "fresh-cart", guestSecret: "new-secret" });
    await addOfferToCart("o1", 1);
    assert.equal(calls.some((c) => c.url.includes("converted-cart") && c.method === "POST"), false);
    assert.equal(calls.some((c) => c.url.includes("fresh-cart/lines") && c.method === "POST"), true);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("cart mapper keeps quantity policy fields for mini-cart and /cart", () => {
  const cart = mapStorefrontCart({
    cartId: "cart-2",
    version: 1,
    market: "IR",
    currency: "IRR",
    channel: "Marketplace",
    itemCount: 1.5,
    subtotalExclusiveOfTax: 150000,
    lines: [
      {
        lineId: "line-2",
        offerId: "offer-2",
        catalogVariantId: "variant-2",
        sellerPartyId: "seller-2",
        title: "وزن‌دار",
        sellerDisplayName: "فروشنده",
        quantity: 1.5,
        unitAmountExclusiveOfTax: 100000,
        lineAmountExclusiveOfTax: 150000,
        currency: "IRR",
        quotedTaxExclusive: true,
        quantityDecimalPlaces: 2,
        quantityStep: 0.5,
        unitDisplayName: "کیلوگرم",
      },
    ],
  });
  assert.equal(cart?.itemCount, 1.5);
  assert.equal(cart?.lines[0]?.quantityDecimalPlaces, 2);
  assert.equal(cart?.lines[0]?.quantityStep, 0.5);
  assert.equal(cart?.lines[0]?.unitDisplayName, "کیلوگرم");
});
