import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { toCustomerFinalClickMessage } from "./storefront-final-click-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../../..");
const read = (rel: string): string => fs.readFileSync(path.join(root, rel), "utf8");

const finalClick = read("src/frontend/app/storefront/storefront-final-click-api.ts");
const handoff = read("src/frontend/app/payment/storefront-payment-handoff.tsx");
const shipping = read("src/frontend/app/storefront/storefront-shipping.tsx");

function requestBody(source: string): string {
  const marker = "body: JSON.stringify({";
  const start = source.indexOf(marker);
  assert.ok(start > 0, "start-payment request body not found");
  const from = start + marker.length;
  const end = source.indexOf("}),", from);
  assert.ok(end > from, "start-payment request body end not found");
  return source.slice(from, end);
}

test("final click targets the cart-addressed Order route and never carries money, order or identity", () => {
  assert.match(finalClick, /\/v1\/storefront\/cart\/\$\{encodeURIComponent\(input\.cartId\)\}\/start-payment/);
  const body = requestBody(finalClick);
  for (const key of ["expectedCartVersion", "idempotencyKey", "useWallet", "providerCode", "couponCode"]) {
    assert.match(body, new RegExp(`${key}\\s*[:,]`), `missing ${key} in start-payment body`);
  }
  for (const forbidden of ["amount", "payable", "shippingAmount", "checkoutId", "userId", "deadline", "guestSecret"]) {
    assert.doesNotMatch(body, new RegExp(forbidden, "i"), `body must not carry ${forbidden}`);
  }
  // گواه مهمان فقط از هدر/کوکی متعارف می‌آید و کارت سبد در مسیر است، نه در بدنه؛ فهرست کلیدها دقیق است.
  const keys = [...body.matchAll(/(?:^|\n)\s*([a-zA-Z]+)\s*[:,]/g)].map((m) => m[1]);
  assert.deepEqual(keys, ["expectedCartVersion", "idempotencyKey", "useWallet", "providerCode", "couponCode"]);
  assert.match(finalClick, /cartHeaders\(\)/);
});

test("final click reuses one stable idempotency key per cart+method instead of a fresh key per render", () => {
  assert.match(finalClick, /const scoped = `\$\{FINAL_CLICK_IDEMPOTENCY_KEY\}\.\$\{cartId\}\.\$\{provider\}`/);
  assert.match(finalClick, /sessionStorage\.getItem\(scoped\)/);
  assert.match(finalClick, /sessionStorage\.setItem\(scoped, created\)/);
  assert.doesNotMatch(requestBody(finalClick), /randomUUID/);
});

test("cart-addressed payment loads the read-only Order draft and invents no checkout id or quote", () => {
  assert.match(finalClick, /\/v1\/storefront\/cart\/\$\{encodeURIComponent\(cartId\)\}\/payment-draft/);
  assert.match(finalClick, /mapStorefrontCheckout\(payload\)/);
  assert.match(handoff, /loadCartPaymentDraft\(cartId\)/);
  assert.match(handoff, /setQuote\(null\)/);
});

test("one click can start exactly one payment: cart draft path and committed checkout path are exclusive", () => {
  assert.equal((handoff.match(/startFinalClickPayment\(/g) ?? []).length, 1);
  assert.equal((handoff.match(/startStorefrontPayment\(/g) ?? []).length, 1);
  assert.match(handoff, /mode === "cart"[\s\S]{0,40}\? await startFinalClickPayment\(\{/);
  assert.match(handoff, /if \(!page \|\| paid \|\| paying\) return;/);
});

test("shipping continue saves the draft and navigates cart-addressed without any commit", () => {
  assert.doesNotMatch(shipping, /shipping\/commit/);
  assert.doesNotMatch(shipping, /commitShippingToPayment/);
  assert.match(shipping, /\/payment\?cartId=/);
  assert.doesNotMatch(shipping, /\/payment\?checkoutId=/);
  assert.match(shipping, /saveShippingSelection\(projection\.cartId, projection\.cartVersion/);
});

test("post-commit failure keeps the persisted checkout for recovery on the same order", () => {
  assert.match(handoff, /payment-recovery-same-order/);
  assert.match(handoff, /\/payment\?checkoutId=\$\{encodeURIComponent\(recoveryCheckoutId\)\}/);
  assert.match(handoff, /cause\.meta\.checkoutId/);
  const error = new StorefrontCartApiError(409, "payment.reservation.not_payable", "held", {
    checkoutId: "11111111-1111-4111-8111-111111111111",
  });
  assert.equal(error.meta.checkoutId, "11111111-1111-4111-8111-111111111111");
});

test("final click failures stay localized and never promise automatic renewal", () => {
  const notPayable = toCustomerFinalClickMessage(
    new StorefrontCartApiError(409, "payment.reservation.not_payable", "Reservation cycle closed for cart"),
  );
  assert.match(notPayable, /سبد خرید/);
  assert.doesNotMatch(notPayable, /Reservation|Held|Cycle/);

  const stale = toCustomerFinalClickMessage(
    new StorefrontCartApiError(409, "checkout.version.conflict", "Cart was updated concurrently. Refresh and retry."),
  );
  assert.doesNotMatch(stale, /Refresh/);
  assert.match(stale, /سبد/);

  assert.match(
    toCustomerFinalClickMessage(new StorefrontCartApiError(403, "checkout.access.denied", "Access denied")),
    /پرداخت/,
  );
  assert.match(
    toCustomerFinalClickMessage(new StorefrontCartApiError(400, "payment.method.unavailable", "not enabled")),
    /روش پرداخت/,
  );
  assert.match(
    toCustomerFinalClickMessage(new StorefrontCartApiError(409, "checkout.open_unpaid_limit_reached", "limit")),
    /سفارش/,
  );
  assert.match(
    toCustomerFinalClickMessage(new StorefrontCartApiError(400, "shipping.selection.required", "required")),
    /ارسال/,
  );
});
