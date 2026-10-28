import assert from "node:assert/strict";
import test from "node:test";

import {
  buildCapturedSelectionIntent,
  capturedSelectionComplete,
  capturedSelectionContextKey,
} from "./storefront-shipping-captured-selection.ts";
import type {
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
} from "./storefront-shipping-api.ts";

/**
 * TB-237 — قصد ثبت انتخاب capture‌شده فقط از پاسخ موفق سرور.
 *
 * این تست قرارداد **ساخت درخواست** را ثابت می‌کند، نه قیمت/مالکیت: مبلغ کالا متن دقیق سرور است،
 * قصد ناقص هرگز ارسال نمی‌شود، روش با کلید سرویس/گزینهٔ همان گروه پیدا می‌شود و هر تغییر context
 * تأییدهای قبلی را باطل می‌کند.
 */
function method(overrides: Record<string, unknown> = {}) {
  return {
    methodLabel: "پست پیشتاز",
    serviceCode: "post",
    optionCode: "standard",
    freightAmountText: "120000.00",
    packagingAmountText: "5000.00",
    payableAtCheckoutAmountText: "125000.00",
    carrierCollectAmountText: "0.00",
    currency: "IRR",
    earliestEligibleDate: "2026-10-29",
    availableDates: [
      { value: "2026-10-29", label: "پنجشنبه", subLabel: "۸ آبان", intervalText: "09:00-17:00", isEarliest: true },
    ],
    unavailableReason: null,
    confirmationSnapshotId: "01a17d0e-0419-7000-b575-f507d2f89abc",
    confirmationSourceRevision: "4294967296",
    confirmationSelectionVersion: "1",
    confirmationAreaGroupId: "01a17d0e-aaaa-7000-b575-f507d2f89abc",
    confirmationRateRowId: "01a17d0e-bbbb-7000-b575-f507d2f89abc",
    ...overrides,
  };
}

function group(overrides: Partial<StorefrontCapturedPreviewGroup> = {}): StorefrontCapturedPreviewGroup {
  return {
    groupKey: "cap|post|store|heavy",
    typeLabel: "سنگین",
    sellerPartyId: "01a030d1-40cb-7000-8abe-6d31739956c5",
    cartLineIds: ["01a17cfd-c1b1-7000-b10d-8cf21258a1cd"],
    finalGoodsAmountText: "9485771.00",
    methods: [method()],
    unavailableReason: null,
    ...overrides,
  };
}

const choice = { methodKey: "post\u001fstandard", serviceCode: "post", optionCode: "standard", date: "2026-10-29" };

function preview(overrides: Partial<StorefrontCapturedPreview> = {}): StorefrontCapturedPreview {
  return {
    state: "ok",
    enabled: true,
    currency: "IRR",
    groups: [group()],
    unavailableReason: null,
    selectionRevision: "0",
    ...overrides,
  };
}

test("a complete selection carries the exact server goods text and every confirmation echo verbatim", () => {
  const intent = buildCapturedSelectionIntent(preview(), { "cap|post|store|heavy": choice });
  assert.ok(intent);
  assert.equal(intent.length, 1);
  const [only] = intent;
  // متن دقیق — هیچ تبدیل عددی/گردکردنی روی پول رخ نمی‌دهد.
  assert.equal(only.expectedGoodsAmount, "9485771.00");
  assert.equal(only.confirmationSourceRevision, "4294967296");
  assert.equal(only.confirmationSnapshotId, "01a17d0e-0419-7000-b575-f507d2f89abc");
  assert.equal(only.confirmationSelectionVersion, "1");
  assert.equal(only.selectedDeliveryDate, "2026-10-29");
  assert.equal(only.selectedDeliveryTimeWindow, "09:00-17:00");
  assert.deepEqual(only.cartLineIds, ["01a17cfd-c1b1-7000-b10d-8cf21258a1cd"]);
});

test("a missing/blank/malformed goods echo blocks the whole intent instead of sending a guess", () => {
  for (const goods of [null, "", "  ", "9,485,771", "+9485771", "9485771.0e3"]) {
    const intent = buildCapturedSelectionIntent(
      preview({ groups: [group({ finalGoodsAmountText: goods as string | null })] }),
      { "cap|post|store|heavy": choice },
    );
    assert.equal(intent, null, `goods=${String(goods)} must not produce an intent`);
    assert.equal(
      capturedSelectionComplete(preview({ groups: [group({ finalGoodsAmountText: goods as string | null })] }), { "cap|post|store|heavy": choice }),
      false,
    );
  }
});

test("explicit zero goods is lawful and stays the literal text zero", () => {
  const intent = buildCapturedSelectionIntent(preview({ groups: [group({ finalGoodsAmountText: "0" })] }), {
    "cap|post|store|heavy": { ...choice },
  });
  assert.ok(intent);
  assert.equal(intent[0].expectedGoodsAmount, "0");
});

test("no method chosen, a date outside that method, or no preview at all never builds a request", () => {
  assert.equal(buildCapturedSelectionIntent(preview(), {}), null);
  assert.equal(
    buildCapturedSelectionIntent(preview(), { "cap|post|store|heavy": { ...choice, date: "2026-11-30" } }),
    null,
  );
  assert.equal(buildCapturedSelectionIntent(null, { "cap|post|store|heavy": choice }), null);
  assert.equal(
    buildCapturedSelectionIntent(preview({ state: "untrusted", groups: [] }), { "cap|post|store|heavy": choice }),
    null,
  );
});

test("two owners sharing the same binding keep independent choices and goods", () => {
  const other = group({
    groupKey: "cap|post|seller|heavy",
    sellerPartyId: "01a030d1-40cb-7000-8abe-6d3173995600",
    cartLineIds: ["01a17cfd-c1b1-7000-b10d-8cf21258a1ff"],
    finalGoodsAmountText: "198.00",
  });
  const intent = buildCapturedSelectionIntent(preview({ groups: [group(), other] }), {
    "cap|post|store|heavy": choice,
    "cap|post|seller|heavy": choice,
  });
  assert.ok(intent);
  assert.equal(intent.length, 2);
  assert.deepEqual(intent.map((entry) => entry.expectedGoodsAmount), ["9485771.00", "198.00"]);
  assert.deepEqual(intent.map((entry) => entry.groupKey), ["cap|post|store|heavy", "cap|post|seller|heavy"]);
});

test("any destination, coupon, cart-version or revision change invalidates previous confirmations", () => {
  const base = { destinationKey: "d1", couponCode: null, cartVersion: 7, snapshotId: null, selectionVersion: "1" };
  const key = capturedSelectionContextKey(base);
  assert.equal(capturedSelectionContextKey({ ...base }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, destinationKey: "d2" }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, couponCode: "SAVE10" }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, cartVersion: 8 }), key);
  assert.notEqual(capturedSelectionContextKey({ ...base, selectionVersion: "2" }), key);
});
