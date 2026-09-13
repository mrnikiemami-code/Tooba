import assert from "node:assert/strict";
import test from "node:test";
import { loginPath, sanitizeReturnTo } from "./login-return-to.ts";

test("internal locale returnTo is accepted", () => {
  assert.equal(sanitizeReturnTo("/fa/shipping", "fa"), "/fa/shipping");
  assert.equal(sanitizeReturnTo("/en/checkout", "en"), "/en/checkout");
});

test("external and protocol-relative returnTo are rejected", () => {
  assert.equal(sanitizeReturnTo("https://evil.example/x", "fa"), "/fa/shipping");
  assert.equal(sanitizeReturnTo("//evil.example/x", "fa"), "/fa/shipping");
  assert.equal(sanitizeReturnTo("javascript:alert(1)", "en"), "/en/shipping");
});

test("secrets in returnTo are rejected and locale is preserved", () => {
  assert.equal(sanitizeReturnTo("/fa/shipping?guestSecret=abc", "fa"), "/fa/shipping");
  assert.equal(loginPath("en", "/en/payment"), "/en/login?returnTo=%2Fen%2Fpayment");
  assert.equal(loginPath("fa", "/fa/shipping"), "/fa/login?returnTo=%2Ffa%2Fshipping");
});
