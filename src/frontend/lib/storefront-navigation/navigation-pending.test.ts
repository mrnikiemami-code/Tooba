import assert from "node:assert/strict";
import test from "node:test";
import {
  beginNavigationPending,
  clearNavigationPending,
  isModifiedNavigationEvent,
  normalizeNavigationHref,
  reconcileNavigationPending,
  shouldIgnoreDuplicatePendingClick,
} from "./navigation-pending.ts";

test("modified / middle clicks stay native", () => {
  assert.equal(isModifiedNavigationEvent({ ctrlKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ metaKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ button: 1 }), true);
  assert.equal(isModifiedNavigationEvent({ button: 0 }), false);
});

test("product-card style href begins pending immediately", () => {
  const pending = beginNavigationPending("/fa/products/demo-1", 10);
  assert.equal(pending.href, "/fa/products/demo-1");
  assert.equal(pending.startedAtMs, 10);
});

test("same-tick duplicate same-target click is ignored", () => {
  const pending = beginNavigationPending("/fa/products/demo-1", 100);
  assert.equal(shouldIgnoreDuplicatePendingClick(pending, "/fa/products/demo-1", 120), true);
  assert.equal(shouldIgnoreDuplicatePendingClick(pending, "/fa/cart", 120), false);
});

test("success clears pending when location matches", () => {
  const pending = beginNavigationPending("/fa/products/demo-1", 1);
  const cleared = reconcileNavigationPending(pending, "/fa/products/demo-1");
  assert.equal(cleared.href, null);
});

test("browser Back clears via explicit clear helper", () => {
  const pending = beginNavigationPending("/fa/cart", 5);
  assert.equal(clearNavigationPending().href, null);
  assert.ok(pending.href);
});

test("normalize compares path regardless of origin", () => {
  assert.equal(
    normalizeNavigationHref("http://127.0.0.1:3000/fa/cart", "http://127.0.0.1:3000"),
    "/fa/cart",
  );
  assert.equal(normalizeNavigationHref("/fa/cart"), "/fa/cart");
});
