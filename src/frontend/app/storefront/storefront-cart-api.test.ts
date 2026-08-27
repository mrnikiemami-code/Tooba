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
