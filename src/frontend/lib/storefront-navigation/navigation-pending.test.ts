import assert from "node:assert/strict";
import test from "node:test";
import {
  beginNavigationPending,
  clearNavigationPending,
  isModifiedNavigationEvent,
  NAVIGATION_RETRY_DISMISS_MS,
  navigationPendingLiveMessage,
  navigationPendingTimedOut,
  navigationTimeoutFeedbackVisible,
  normalizeNavigationHref,
  reconcileNavigationPending,
  resolveBeginNavigation,
  shouldIgnoreDuplicatePendingClick,
} from "./navigation-pending.ts";

test("modified / middle clicks stay native", () => {
  assert.equal(isModifiedNavigationEvent({ ctrlKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ metaKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ shiftKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ altKey: true }), true);
  assert.equal(isModifiedNavigationEvent({ button: 1 }), true);
  assert.equal(isModifiedNavigationEvent({ button: 0 }), false);
});

test("ordinary click starts pending in resolveBeginNavigation", () => {
  const result = resolveBeginNavigation({
    href: "/fa/products/demo-1",
    currentHref: "/fa",
    pending: clearNavigationPending(),
    nowMs: 10,
  });
  assert.equal(result.kind, "started");
  if (result.kind === "started") {
    assert.equal(result.pending.href, "/fa/products/demo-1");
    assert.ok((result.pending.startedAtMs ?? 0) <= 10 + 1);
  }
});

test("second ordinary exact-target click while pending is duplicate", () => {
  const pending = beginNavigationPending("/fa/products/demo-1", 100);
  const result = resolveBeginNavigation({
    href: "/fa/products/demo-1",
    currentHref: "/fa",
    pending,
    nowMs: 120,
  });
  assert.equal(result.kind, "duplicate");
  assert.equal(shouldIgnoreDuplicatePendingClick(pending, "/fa/cart", 120), false);
});

test("Ctrl/Cmd/Shift/Alt and middle-click are native and do not start pending", () => {
  for (const event of [
    { ctrlKey: true },
    { metaKey: true },
    { shiftKey: true },
    { altKey: true },
    { button: 1 },
  ]) {
    const result = resolveBeginNavigation({
      href: "/fa/products/demo-1",
      currentHref: "/fa",
      pending: clearNavigationPending(),
      nowMs: 1,
      event,
    });
    assert.equal(result.kind, "native", JSON.stringify(event));
  }
});

test("same-current-target does not start pending", () => {
  const result = resolveBeginNavigation({
    href: "/fa/cart?x=1#top",
    currentHref: "/fa/cart?x=1#top",
    pending: clearNavigationPending(),
    nowMs: 1,
  });
  assert.equal(result.kind, "same-location");
});

test("pathname query and hash completion each clear the matching pending target", () => {
  const pending = beginNavigationPending("/fa/cart?tab=1#lines", 1);
  assert.equal(reconcileNavigationPending(pending, "/fa/cart?tab=1#lines").href, null);
  assert.equal(reconcileNavigationPending(pending, "/fa/cart").href, "/fa/cart?tab=1#lines");
});

test("browser Back clears via explicit clear helper", () => {
  const pending = beginNavigationPending("/fa/cart", 5);
  assert.equal(clearNavigationPending().href, null);
  assert.ok(pending.href);
});

test("failed/no-op navigation times out", () => {
  const pending = beginNavigationPending("/fa/products/x", 1000);
  assert.equal(navigationPendingTimedOut(pending, 5999), false);
  assert.equal(navigationPendingTimedOut(pending, 6000), true);
});

test("FA and EN live-region strings are both asserted", () => {
  assert.match(navigationPendingLiveMessage("fa", "pending"), /رفتن|صفحه/);
  assert.equal(
    navigationPendingLiveMessage("fa", "retry"),
    "باز شدن صفحه طول کشید؛ دوباره تلاش کنید.",
  );
  assert.match(navigationPendingLiveMessage("en", "pending"), /Navigating/);
  assert.equal(
    navigationPendingLiveMessage("en", "retry"),
    "The page is taking too long to open. Try again.",
  );
});

test("timeout feedback is visible only after pending clears", () => {
  assert.equal(navigationTimeoutFeedbackVisible(null, true), false);
  assert.equal(navigationTimeoutFeedbackVisible("x", true), false);
  assert.equal(navigationTimeoutFeedbackVisible("x", false), true);
  assert.ok(NAVIGATION_RETRY_DISMISS_MS >= 4000 && NAVIGATION_RETRY_DISMISS_MS <= 8000);
});

test("normalize compares path regardless of origin", () => {
  assert.equal(
    normalizeNavigationHref("http://127.0.0.1:3000/fa/cart", "http://127.0.0.1:3000"),
    "/fa/cart",
  );
  assert.equal(normalizeNavigationHref("/fa/cart"), "/fa/cart");
});
