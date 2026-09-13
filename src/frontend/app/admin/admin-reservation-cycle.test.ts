import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
import {
  formatReservationCountdown,
  remainingSecondsFromServer,
  shouldRefreshOnceAtZero,
} from "./admin-reservation-cycle.ts";
import { mapAdminOrder, mapAdminOrderDetail, mapAdminReceipt, mapAdminReservationCycleAudit } from "./admin-api.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const detail = readFileSync(join(dir, "admin-order-detail-screen.tsx"), "utf8");
const screens = readFileSync(join(dir, "admin-screens.tsx"), "utf8");
const ops = readFileSync(join(dir, "admin-order-operations.ts"), "utf8");
const api = readFileSync(join(dir, "admin-api.ts"), "utf8");
const cycle = readFileSync(join(dir, "admin-reservation-cycle.ts"), "utf8");

test("order detail covers required reservation states and history", () => {
  const audit = mapAdminReservationCycleAudit({
    statusLabelFa: "رزرو فعال",
    statusLabelEn: "Active reservation",
    reasonLabelFa: "رزرو اولیه پرداخت",
    reasonLabelEn: "Initial payment reservation",
    currentCycleNumber: 1,
    totalCyclesUsed: 1,
    maxCycles: 3,
    retryCountRemaining: 2,
    startedAt: "2026-09-13T08:00:00Z",
    expiresAt: "2026-09-13T08:10:00Z",
    serverTime: "2026-09-13T08:00:00Z",
    secondsRemaining: 600,
    supplyStatus: "Reserved",
    supplyStatusLabelFa: "موجودی موردنیاز این سفارش رزرو شده است.",
    retryLimitReached: false,
    canRetryReservation: false,
    canExtendTimer: false,
    history: [{
      cycleNumber: 1,
      statusLabelFa: "رزرو فعال",
      statusLabelEn: "Active reservation",
      reasonLabelFa: "رزرو اولیه پرداخت",
      reasonLabelEn: "Initial",
      startedAt: "2026-09-13T08:00:00Z",
      expiresAt: "2026-09-13T08:10:00Z",
      endedAt: null,
      effectiveHoldMinutes: 90,
      effectiveMaxCycles: 3,
      policySource: "offer",
      policySourceLabelFa: "پیشنهاد",
      policySourceLabelEn: "Offer",
      paymentAttemptRef: "abcd1234",
    }],
    events: [],
    shortages: [],
  });
  assert.equal(audit?.statusLabelFa, "رزرو فعال");
  assert.equal(audit?.history[0]?.effectiveHoldMinutes, 90);
  assert.equal(audit?.canExtendTimer, false);
  assert.match(detail, /رزرو موجودی/);
  assert.match(detail, /رزرو فعال/);
  assert.match(detail, /مهلت رزرو پایان یافته/);
  assert.match(detail, /رزرو با لغو سفارش آزاد شد/);
  assert.match(detail, /رزرو مجدد ناموفق/);
  assert.match(detail, /رزرو پس از پرداخت نهایی شد/);
  assert.match(detail, /حداکثر دفعات رزرو این سفارش استفاده شده است/);
  assert.match(detail, /رزرو مجدد به دلیل کمبود موجودی انجام نشد/);
  assert.match(detail, /admin-reservation-history/);
  assert.match(detail, /admin-reservation-shortages/);
  assert.match(detail, /dir=\{dir\}/);
  assert.match(detail, /Inventory reservation/);
});

test("countdown uses server ExpiresAt and refreshes once at zero", () => {
  const received = Date.parse("2026-09-13T08:00:00Z");
  assert.equal(
    remainingSecondsFromServer("2026-09-13T08:10:00Z", "2026-09-13T08:00:00Z", received, received),
    600,
  );
  assert.equal(
    remainingSecondsFromServer("2026-09-13T08:10:00Z", "2026-09-13T08:00:00Z", received, received + 90_000),
    510,
  );
  assert.equal(formatReservationCountdown(75), "01:15");
  assert.equal(formatReservationCountdown(3600), "01:00:00");
  assert.doesNotMatch(formatReservationCountdown(86400), /1440/);
  assert.equal(shouldRefreshOnceAtZero(2, 0, false), true);
  assert.equal(shouldRefreshOnceAtZero(0, 0, true), false);
  assert.match(detail, /remainingSecondsFromServer/);
  assert.match(detail, /shouldRefreshOnceAtZero/);
  assert.match(detail, /window.setInterval/);
  assert.doesNotMatch(detail, /setInterval\([^\n]*fetch/);
});

test("grids show compact reservation without per-row fetch", () => {
  const order = mapAdminOrder({
    CheckoutId: "c1",
    Reference: "TOOBA-101",
    ReservationLabel: "فعال #1",
    ReservationState: "active",
    ReservationCycleNumber: 1,
  });
  assert.equal(order?.reservationLabel, "فعال #1");
  const receipt = mapAdminReceipt({
    PaymentId: "p1",
    CheckoutId: "c1",
    ReservationLabel: "پایان‌یافته #1",
    ReservationState: "expired",
    ReservationRetryPossible: true,
    ReservationNeedsReacquire: true,
  });
  assert.equal(receipt?.reservationLabel, "پایان‌یافته #1");
  assert.equal(receipt?.reservationRetryPossible, true);
  assert.match(screens, /رزرو موجودی/);
  assert.match(screens, /reservationStateEnumOptions/);
  assert.match(screens, /ADMIN_ORDER_GRID_VIEW_KEY/);
  assert.match(screens, /ADMIN_RECEIPT_GRID_VIEW_KEY/);
  assert.doesNotMatch(screens, /reservation-cycle\/\$\{row/);
  assert.doesNotMatch(api, /\/reservation-cycle\/\$\{/);
  assert.match(detail, /admin-payment-reservation-label/);
  assert.doesNotMatch(detail, /admin-reservation-history[\s\S]*admin-payment-reservation-label/);
});

test("capabilities forbid extend timer and invented retry", () => {
  assert.match(ops, /canRetryReservation/);
  assert.match(ops, /canExtendTimer/);
  assert.doesNotMatch(detail, /تمدید رزرو/);
  assert.doesNotMatch(detail, /ریست تایمر/);
  assert.doesNotMatch(screens, /تمدید رزرو/);
  assert.match(cycle, /canExtendTimer/);
  assert.match(detail, /موجودی قابل تأمین است و هنگام تأیید واریز به‌صورت خودکار رزرو می‌شود/);
});

test("mapped order detail keeps reservation projection without raw enums", () => {
  const mapped = mapAdminOrderDetail({
    CheckoutId: "c1",
    Reference: "R",
    SubmittedAt: "2026-09-13T08:00:00Z",
    Status: "PendingPayment",
    PaymentState: "PendingPayment",
    LineCount: 1,
    SellerCount: 1,
    Subtotal: 1,
    TaxAmount: 0,
    DiscountAmount: 0,
    PayableAmount: 1,
    Currency: "IRR",
    RecipientName: "سارا",
    ContactMobile: "09",
    ProvinceName: "تهران",
    CityName: "تهران",
    PostalAddress: "آدرس",
    PostalCode: "1",
    ShippingMethodLabel: "پست",
    SellerOrders: [],
    SellerFinancials: [],
    FinancialEvents: [],
    FinancialSummary: {},
    ReservationCycle: {
      StatusLabelFa: "رزرو فعال",
      StatusLabelEn: "Active reservation",
      ReasonLabelFa: "رزرو اولیه پرداخت",
      CurrentCycleNumber: 2,
      TotalCyclesUsed: 2,
      MaxCycles: 3,
      RetryCountRemaining: 1,
      ServerTime: "2026-09-13T08:00:00Z",
      SecondsRemaining: 10,
      SupplyStatus: "Reserved",
      CanRetryReservation: false,
      CanExtendTimer: false,
      History: [],
      Events: [],
      Shortages: [],
    },
  });
  assert.equal(mapped?.reservationCycle?.statusLabelFa, "رزرو فعال");
  assert.equal(mapped?.reservationCycle?.currentCycleNumber, 2);
  assert.doesNotMatch(detail, /ReservationCycleStatus/);
  assert.doesNotMatch(screens, /ReservationCycleReason/);
});
