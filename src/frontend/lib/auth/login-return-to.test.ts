import assert from "node:assert/strict";
import test from "node:test";
import { canonicalReturnTo, loginPath, sanitizeReturnTo } from "./login-return-to.ts";

test("internal locale returnTo is accepted", () => {
  assert.equal(sanitizeReturnTo("/fa/shipping", "fa"), "/fa/shipping");
  assert.equal(sanitizeReturnTo("/en/checkout", "en"), "/en/checkout");
});

test("missing returnTo stays on the storefront home, not shipping", () => {
  assert.equal(sanitizeReturnTo(null, "fa"), "/fa");
  assert.equal(sanitizeReturnTo("", "en"), "/en");
  assert.equal(loginPath("fa"), "/fa/login?returnTo=%2Ffa");
});

test("external and protocol-relative returnTo are rejected", () => {
  assert.equal(sanitizeReturnTo("https://evil.example/x", "fa"), "/fa");
  assert.equal(sanitizeReturnTo("//evil.example/x", "fa"), "/fa");
  assert.equal(sanitizeReturnTo("javascript:alert(1)", "en"), "/en");
});

test("secrets in returnTo are rejected and locale is preserved", () => {
  assert.equal(sanitizeReturnTo("/fa/shipping?guestSecret=abc", "fa"), "/fa");
  assert.equal(loginPath("en", "/en/payment"), "/en/login?returnTo=%2Fen%2Fpayment");
  assert.equal(loginPath("fa", "/fa/shipping"), "/fa/login?returnTo=%2Ffa%2Fshipping");
});

test("canonicalReturnTo matches SSR rewrite path and public locale URL", () => {
  assert.equal(canonicalReturnTo("fa", "/cart"), "/fa/cart");
  assert.equal(canonicalReturnTo("fa", "/fa/cart"), "/fa/cart");
  assert.equal(canonicalReturnTo("fa", "/"), "/fa");
  assert.equal(canonicalReturnTo("fa", "/fa"), "/fa");
  assert.equal(loginPath("fa", canonicalReturnTo("fa", "/cart")), "/fa/login?returnTo=%2Ffa%2Fcart");
  assert.equal(loginPath("fa", canonicalReturnTo("fa", "/fa/cart")), "/fa/login?returnTo=%2Ffa%2Fcart");
});
