import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontPayment, mapStorefrontPaymentInitiation, toCustomerPaymentMessage } from "./storefront-payment-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";

test("payment initiation mapper keeps host redirect and amount", () => {
  const page = mapStorefrontPaymentInitiation({
    paymentId: "pay-1",
    attemptId: "att-1",
    checkoutId: "chk-1",
    status: "Pending",
    providerCode: "fake",
    providerRequestReference: "fake-abc",
    redirectUrl: "/payment/sandbox?paymentId=pay-1",
    amount: 1951100,
    currency: "IRR",
  });
  assert.equal(page?.paymentId, "pay-1");
  assert.equal(page?.amount, 1951100);
  assert.match(page?.redirectUrl ?? "", /sandbox/);
  assert.equal(page?.status, "Pending");
});

test("payment mapper does not invent succeeded", () => {
  const page = mapStorefrontPayment({
    paymentId: "pay-1",
    checkoutId: "chk-1",
    amount: 100,
    currency: "IRR",
    status: "Pending",
    providerCode: "fake",
  });
  assert.equal(page?.status, "Pending");
});

test("customer payment message hides gateway codes", () => {
  const hidden = toCustomerPaymentMessage(
    new StorefrontCartApiError(400, "payment.rejected", "GATEWAY_REJECTED"),
  );
  assert.equal(hidden.includes("GATEWAY_"), false);
});
