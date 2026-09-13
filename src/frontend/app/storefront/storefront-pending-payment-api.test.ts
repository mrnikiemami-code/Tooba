import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";
import {
  formatCountdown,
  mapStorefrontPendingPayments,
  remainingSecondsFromServer,
  shouldRefreshOnceAtZero,
  toCustomerPendingPaymentMessage,
} from "./storefront-pending-payment-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("countdown is derived from server ExpiresAt and counts locally", () => {
  const serverTime = "2026-09-13T10:00:00.000Z";
  const holdEndsAt = "2026-09-13T10:10:00.000Z";
  const received = Date.parse(serverTime);
  assert.equal(remainingSecondsFromServer(holdEndsAt, serverTime, received, received), 600);
  assert.equal(remainingSecondsFromServer(holdEndsAt, serverTime, received, received + 90_000), 510);
  assert.equal(formatCountdown(570), "09:30");
  assert.equal(formatCountdown(0), "00:00");
  assert.equal(shouldRefreshOnceAtZero(1, 0, false), true);
  assert.equal(shouldRefreshOnceAtZero(1, 0, true), false);
  assert.equal(shouldRefreshOnceAtZero(0, 0, false), false);
});

test("failed payment mapping does not invent a new timer", () => {
  const page = mapStorefrontPendingPayments({
    serverTime: "2026-09-13T10:00:00Z",
    items: [
      {
        checkoutId: "chk-1",
        cartId: "cart-1",
        orderReference: "SO-1",
        payableAmount: 1000,
        currency: "IRR",
        items: [{ title: "کالا", quantity: 1 }],
        paymentPresentation: "failedRetryable",
        canInitiatePayment: true,
        canRetryPayment: false,
        isManualAwaitingReview: false,
        primaryAction: "pay",
        reservationPresentation: "held",
        cycleNumber: 1,
        secondsRemaining: 500,
        serverTime: "2026-09-13T10:00:00Z",
        holdEndsAt: "2026-09-13T10:08:20Z",
        maxCycles: 3,
        retryCountRemaining: 2,
      },
    ],
  });
  assert.equal(page?.items[0]?.holdEndsAt, "2026-09-13T10:08:20Z");
  assert.equal(page?.items[0]?.secondsRemaining, 500);
  assert.equal(page?.items[0]?.primaryAction, "pay");
});

test("customer pending errors never use checkout registration copy", () => {
  assert.equal(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(409, "payment.unpaid.supply_unavailable", "x")),
    "این سفارش در حال حاضر قابل تأمین نیست.",
  );
  assert.match(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(409, "inventory.reservation.retry_limit_reached", "x")),
    /به پایان رسیده/,
  );
  assert.match(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(409, "payment.already_succeeded", "x")),
    /با موفقیت انجام شده/,
  );
  assert.match(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(403, "payment.access.denied", "x")),
    /دسترسی/,
  );
  assert.equal(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(403, "payment.access.denied", "x")).includes("ثبت سفارش انجام نشد"),
    false,
  );
  assert.match(
    toCustomerPendingPaymentMessage(new StorefrontCartApiError(409, "payment.already_succeeded", "x"), "en"),
    /already been paid/,
  );
});

test("pending UX source keeps FA RTL EN LTR and no raw cycle enums", () => {
  const ui = fs.readFileSync(path.join(root, "app/storefront/storefront-pending-payments.tsx"), "utf8");
  const cart = fs.readFileSync(path.join(root, "app/storefront/storefront-cart.tsx"), "utf8");
  const api = fs.readFileSync(path.join(root, "app/storefront/storefront-pending-payment-api.ts"), "utf8");
  assert.match(ui, /dir=\{locale === "fa" \? "rtl" : "ltr"\}/);
  assert.match(ui, /Awaiting payment/);
  assert.match(ui, /در انتظار پرداخت/);
  assert.match(ui, /data-testid="pending-payment-section"/);
  assert.match(cart, /hasPending=\{pendingItems.length > 0\}/);
  assert.match(cart, /سبد فعال شما خالی است/);
  assert.match(api, /listCommittedCheckoutProofs/);
  assert.doesNotMatch(api, /readCartSession\(\)/);
  assert.doesNotMatch(ui, /ReservationCycleStatus/);
  assert.doesNotMatch(ui, /inventory\.reservation/);
  assert.match(ui, /setInterval/);
  assert.doesNotMatch(ui, /setInterval\(\(\) => fetch/);
  assert.match(ui, /heldPay/);
  assert.match(ui, /failedRetryable/);
});
