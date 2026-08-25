import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontCheckout, submitStorefrontCheckout, toCheckoutShippingBody, toCustomerCheckoutMessage } from "./storefront-checkout-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";

test("checkout mapper keeps backend totals and pending payment", () => {
  const page = mapStorefrontCheckout({
    checkoutId: "chk-1",
    cartId: "cart-1",
    cartVersion: 2,
    currency: "IRR",
    paymentState: "PendingPayment",
    subtotalExclusiveOfTax: 100000,
    discountAmount: 0,
    taxAmount: 9000,
    shippingAmount: 0,
    payableAmount: 109000,
    sellerOrders: [
      {
        sellerOrderId: "so-1",
        orderNumber: "TB-1",
        sellerPartyId: "seller-1",
        sellerDisplayName: "فروشنده",
        status: "PendingPayment",
        subtotalExclusiveOfTax: 100000,
        taxAmount: 9000,
        discountAmount: 0,
        payableAmount: 109000,
        currency: "IRR",
        lines: [
          {
            offerId: "offer-1",
            sellerPartyId: "seller-1",
            title: "کالا",
            quantity: 1,
            lineExclusiveOfTax: 100000,
            taxAmount: 9000,
            linePayable: 109000,
            currency: "IRR",
          },
        ],
      },
    ],
  });
  assert.equal(page?.checkoutId, "chk-1");
  assert.equal(page?.paymentState, "PendingPayment");
  assert.equal(page?.payableAmount, 109000);
  assert.equal(page?.sellerOrders[0]?.orderNumber, "TB-1");
});

test("customer checkout message hides technical tax codes", () => {
  const hidden = toCustomerCheckoutMessage(
    new StorefrontCartApiError(409, "checkout.tax.unavailable", "TAX_NO_APPLICABLE_RULE"),
  );
  assert.equal(hidden.includes("TAX_"), false);
  assert.match(hidden, /مالیات|سفارش/);
});

test("checkout shipping body includes savedAddressId only when selected", () => {
  const shipping = {
    recipientName: "علی",
    contactMobile: "09120000000",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "ولیعصر",
    postalCode: "1234567890",
  };
  const guest = toCheckoutShippingBody(shipping, null);
  assert.equal("savedAddressId" in guest, false);
  const saved = toCheckoutShippingBody(shipping, "addr-1");
  assert.equal(saved.savedAddressId, "addr-1");
  assert.equal(saved.recipientName, "علی");
});

test("checkout submit sends savedAddressId for actor and omits it on guest inline path", async () => {
  const originalFetch = globalThis.fetch;
  const checkoutPage = {
    checkoutId: "chk-1",
    cartId: "cart-1",
    cartVersion: 2,
    paymentState: "PendingPayment",
    payableAmount: 1,
    sellerOrders: [],
  };
  const bodies: unknown[] = [];
  globalThis.fetch = (async (_input: string | URL | Request, init?: RequestInit) => {
    bodies.push(JSON.parse(String(init?.body ?? "{}")));
    return new Response(JSON.stringify(checkoutPage), { status: 200 });
  }) as typeof fetch;
  try {
    const shipping = {
      recipientName: "علی",
      contactMobile: "09120000000",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "ولیعصر",
      postalCode: "1234567890",
    };
    await submitStorefrontCheckout("cart-1", 2, shipping, "addr-owned");
    await submitStorefrontCheckout("cart-1", 2, shipping);
    const saved = bodies[0] as { shipping: { savedAddressId?: string } };
    const guest = bodies[1] as { shipping: { savedAddressId?: string } };
    assert.equal(saved.shipping.savedAddressId, "addr-owned");
    assert.equal(guest.shipping.savedAddressId, undefined);
  } finally {
    globalThis.fetch = originalFetch;
  }
});
