import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontCheckout, toCustomerCheckoutMessage } from "./storefront-checkout-api.ts";
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
