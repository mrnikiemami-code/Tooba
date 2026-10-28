import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  allowsFreshPaymentAction,
  resolveExistingPaymentState,
} from "./storefront-payment-existing-state.ts";
import type { StorefrontPendingPaymentItem } from "../storefront/storefront-pending-payment-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

function item(overrides: Partial<StorefrontPendingPaymentItem> = {}): StorefrontPendingPaymentItem {
  return {
    checkoutId: "ck-1",
    cartId: "cart-1",
    orderReference: "ref",
    payableAmount: 1000,
    currency: "IRR",
    items: [],
    paymentPresentation: "",
    canInitiatePayment: true,
    canRetryPayment: false,
    isManualAwaitingReview: false,
    primaryAction: "pay",
    reservationPresentation: "held",
    cycleNumber: 1,
    secondsRemaining: 60,
    serverTime: "",
    holdEndsAt: null,
    maxCycles: 1,
    retryCountRemaining: 1,
    supplyStatus: null,
    paymentId: "pay-1",
    hasReachedRetryLimit: false,
    ...overrides,
  };
}

test("failed status read is UNKNOWN, never 'no payment started'", () => {
  const state = resolveExistingPaymentState({ status: "failed" }, "ck-1");
  assert.equal(state.kind, "unknown");
  assert.equal(allowsFreshPaymentAction(state, "checkout"), false);
});

test("verified read without this checkout is the only path that reopens a fresh action", () => {
  const state = resolveExistingPaymentState({ status: "verified", page: { serverTime: "", items: [] } }, "ck-1");
  assert.equal(state.kind, "none");
  assert.equal(allowsFreshPaymentAction(state, "checkout"), true);
});

test("verified pending row for the same checkout keeps the continuation and its payment id", () => {
  const state = resolveExistingPaymentState(
    { status: "verified", page: { serverTime: "", items: [item({ checkoutId: "other", paymentId: "pay-x" }), item()] } },
    "ck-1",
  );
  assert.deepEqual(state, { kind: "started", paymentId: "pay-1", awaitingManual: false });
  assert.equal(allowsFreshPaymentAction(state, "checkout"), false);
});

test("awaitingManual is only stamped when the pending row really is manual awaiting review", () => {
  const manual = resolveExistingPaymentState(
    { status: "verified", page: { serverTime: "", items: [item({ isManualAwaitingReview: true })] } },
    "ck-1",
  );
  assert.deepEqual(manual, { kind: "started", paymentId: "pay-1", awaitingManual: true });
});

test("a pending row without a payment id is unknown and fabricates nothing", () => {
  const state = resolveExistingPaymentState(
    { status: "verified", page: { serverTime: "", items: [item({ paymentId: null })] } },
    "ck-1",
  );
  assert.equal(state.kind, "unknown");
  assert.equal(allowsFreshPaymentAction(state, "checkout"), false);
});

test("a cart-mode draft page is not blocked by an order-scoped payment state", () => {
  assert.equal(allowsFreshPaymentAction({ kind: "unknown" }, "cart"), true);
});

test("the page carries the tri-state: no catch(() => null) swallow and no fabricated awaitingManual", () => {
  const source = fs.readFileSync(path.join(root, "app/payment/storefront-payment-handoff.tsx"), "utf8");
  assert.doesNotMatch(source, /loadStorefrontPendingPayments\(\)\.catch/);
  assert.match(source, /status: "failed"/);
  assert.match(source, /payment-status-unknown/);
  assert.match(source, /allowsFreshPaymentAction\(existingState, mode\)/);
  assert.doesNotMatch(source, /awaitingManual=1`\}\s*\/>/);
});
