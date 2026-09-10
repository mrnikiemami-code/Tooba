import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("payment handoff has no fake card number/CVV/expiry fields", () => {
  const source = fs.readFileSync(path.join(root, "app/payment/storefront-payment-handoff.tsx"), "utf8");
  assert.match(source, /loadStorefrontPaymentMethods/);
  assert.match(source, /payment-submit/);
  assert.doesNotMatch(source, /CVV|cvv|cardNumber|card-number|expiry|انقضای کارت|رمز دوم/);
});

test("payment method picker can hide gateway when host omits it", () => {
  const source = fs.readFileSync(path.join(root, "app/storefront/storefront-payment-methods.tsx"), "utf8");
  assert.match(source, /hostEnabledCodes/);
  assert.doesNotMatch(source, /CVV|cardNumber|expiryMonth/);
});
