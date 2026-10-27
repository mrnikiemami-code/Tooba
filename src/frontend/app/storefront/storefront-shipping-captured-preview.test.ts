import assert from "node:assert/strict";
import test from "node:test";

import { mapShippingProjection } from "./storefront-shipping-api.ts";

/**
 * TB-229-R1 — رمزگشای سخت‌گیر پیش‌نمایش capture‌شده.
 *
 * قواعد اثبات‌شده: مبلغ نامعتبر هرگز صفر نمی‌شود؛ صفر صریح قانونی است؛ اعشار به‌صورت متن دقیق
 * می‌ماند؛ ارز/هویت تکراری/ناقص کل پاسخ را untrusted می‌کند (نه حذف بی‌صدا)؛ غایب با نامعتبر فرق دارد.
 */

const GROUP = "11111111-1111-4111-8111-111111111111";
const SELLER = "22222222-2222-4222-8222-222222222222";
const LINE = "33333333-3333-4333-8333-333333333333";

function projection(capturedPreview: unknown): ReturnType<typeof mapShippingProjection> {
  return mapShippingProjection({
    cartId: "44444444-4444-4444-8444-444444444444",
    cartVersion: 1,
    currency: "IRR",
    itemCount: 1,
    subtotalExclusiveOfTax: 100,
    sellerCount: 1,
    maxSellerPreparationDays: 1,
    methods: [],
    dates: [],
    timeWindows: [],
    capturedPreview,
  });
}

function method(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    methodLabel: "پست پیشتاز",
    serviceCode: "post",
    optionCode: "express",
    freightAmount: "15000",
    packagingAmount: "2000.50",
    payableAtCheckoutAmount: "17000.50",
    carrierCollectAmount: "0",
    currency: "IRR",
    earliestEligibleDate: "2026-10-29",
    availableDates: [
      {
        date: "2026-10-29",
        label: "پنجشنبه",
        subLabel: "۱۴۰۵/۸/۷",
        intervalText: "09:00–12:00",
        isEarliest: true,
      },
      {
        date: "2026-10-30",
        label: "جمعه",
        subLabel: "۱۴۰۵/۸/۸",
        intervalText: "10:15–13:45",
        isEarliest: false,
      },
    ],
    unavailableReason: null,
    ...overrides,
  };
}

function group(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    groupKey: "cap|platform|store",
    typeLabel: "عادی",
    sellerPartyId: SELLER,
    cartLineIds: [LINE],
    finalGoodsAmountAfterDiscountAndTax: "99",
    methods: [method()],
    unavailableReason: null,
    ...overrides,
  };
}

function preview(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    enabled: true,
    currency: "IRR",
    groups: [group()],
    noticeReason: null,
    ...overrides,
  };
}

test("absent captured preview stays null so the legacy shipping path is untouched", () => {
  assert.equal(projection(undefined).capturedPreview, null);
  assert.equal(projection(null).capturedPreview, null);
});

test("disabled captured preview maps to an ok, disabled, empty preview", () => {
  const mapped = projection({ enabled: false, currency: "IRR", groups: [] }).capturedPreview;
  assert.equal(mapped?.state, "ok");
  assert.equal(mapped?.enabled, false);
  assert.deepEqual(mapped?.groups, []);
});

test("exact decimal wire text is preserved without any numeric conversion", () => {
  const mapped = projection(preview()).capturedPreview;
  assert.equal(mapped?.state, "ok");
  const group0 = mapped!.groups[0]!;
  assert.equal(group0.finalGoodsAmountText, "99");
  const method0 = group0.methods[0]!;
  assert.equal(method0.freightAmountText, "15000");
  assert.equal(method0.packagingAmountText, "2000.50");
  assert.equal(method0.carrierCollectAmountText, "0");
  assert.equal(method0.availableDates[1]!.intervalText, "10:15–13:45");
  assert.equal(method0.availableDates[1]!.isEarliest, false);
});

test("an invalid amount never becomes zero or another amount: the whole preview is untrusted", () => {
  for (const bad of ["abc", "12,34", "-100", "1e3", "", " ", 15000]) {
    const mapped = projection(preview({ groups: [group({ methods: [method({ freightAmount: bad })] })] }));
    const value = mapped.capturedPreview;
    assert.equal(value?.state, "untrusted", `amount ${JSON.stringify(bad)} must not be trusted`);
    assert.deepEqual(value?.groups, []);
  }
});

test("explicit zero money and explicit null money stay distinct", () => {
  const zero = projection(preview()).capturedPreview!.groups[0]!.methods[0]!;
  assert.equal(zero.carrierCollectAmountText, "0");
  const missing = projection(
    preview({ groups: [group({ methods: [method({ freightAmount: null, packagingAmount: null })] })] }),
  ).capturedPreview!.groups[0]!.methods[0]!;
  assert.equal(missing.freightAmountText, null);
  assert.equal(missing.packagingAmountText, null);
});

test("unknown goods amount stays null instead of a fabricated zero", () => {
  const mapped = projection(
    preview({ groups: [group({ finalGoodsAmountAfterDiscountAndTax: null })] }),
  ).capturedPreview!.groups[0]!;
  assert.equal(mapped.finalGoodsAmountText, null);
});

test("missing/foreign currency is untrusted, never defaulted to IRR", () => {
  assert.equal(projection(preview({ currency: "" })).capturedPreview?.state, "untrusted");
  assert.equal(projection(preview({ currency: undefined })).capturedPreview?.state, "untrusted");
  assert.equal(
    projection(preview({ groups: [group({ methods: [method({ currency: "USD" })] })] })).capturedPreview?.state,
    "untrusted",
  );
  assert.equal(
    projection(preview({ groups: [group({ methods: [method({ currency: null })] })] })).capturedPreview?.state,
    "untrusted",
  );
});

test("malformed, duplicate or missing group identity is untrusted rather than silently dropped", () => {
  assert.equal(projection(preview({ groups: [group({ groupKey: "" })] })).capturedPreview?.state, "untrusted");
  assert.equal(projection(preview({ groups: [group(), group()] })).capturedPreview?.state, "untrusted");
  assert.equal(projection(preview({ groups: [group({ cartLineIds: [] })] })).capturedPreview?.state, "untrusted");
  assert.equal(
    projection(preview({ groups: [group({ cartLineIds: [LINE, LINE] })] })).capturedPreview?.state,
    "untrusted",
  );
  assert.equal(projection(preview({ groups: [group({ sellerPartyId: "" })] })).capturedPreview?.state, "untrusted");
  assert.equal(projection(preview({ groups: "nope" })).capturedPreview?.state, "untrusted");
  assert.equal(projection(preview({ enabled: "yes" })).capturedPreview?.state, "untrusted");
});

test("duplicate methods in one group are untrusted even when only the label differs", () => {
  const mapped = projection(
    preview({
      groups: [
        group({
          methods: [
            method({ methodLabel: "پست" }),
            method({ methodLabel: "پست", optionCode: "express" }),
          ],
        }),
      ],
    }),
  ).capturedPreview;
  assert.equal(mapped?.state, "untrusted");
});

test("two distinct bindings sharing one display label both survive", () => {
  const mapped = projection(
    preview({
      groups: [
        group({
          methods: [
            method({ serviceCode: "post", optionCode: "standard", methodLabel: "ارسال" }),
            method({ serviceCode: "post", optionCode: "express", methodLabel: "ارسال" }),
          ],
        }),
      ],
    }),
  ).capturedPreview!;
  const methods = mapped.groups[0]!.methods;
  assert.equal(methods.length, 2);
  assert.deepEqual(
    methods.map((entry) => `${entry.serviceCode}:${entry.optionCode}`),
    ["post:standard", "post:express"],
  );
});

test("duplicate or malformed dates in one method are untrusted, never deduplicated silently", () => {
  const slot = {
    date: "2026-10-29",
    label: "پنجشنبه",
    subLabel: "۱۴۰۵/۸/۷",
    intervalText: "09:00–12:00",
    isEarliest: true,
  };
  assert.equal(
    projection(preview({ groups: [group({ methods: [method({ availableDates: [slot, slot] })] })] }))
      .capturedPreview?.state,
    "untrusted",
  );
  assert.equal(
    projection(preview({ groups: [group({ methods: [method({ availableDates: [{ ...slot, date: "29/10" }] })] })] }))
      .capturedPreview?.state,
    "untrusted",
  );
  assert.equal(
    projection(preview({ groups: [group({ methods: [method({ availableDates: "nope" })] })] })).capturedPreview?.state,
    "untrusted",
  );
});
