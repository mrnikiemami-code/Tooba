import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontCart } from "./storefront-cart-api.ts";

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
