import assert from "node:assert/strict";
import test from "node:test";

import {
  addExactDecimalTexts,
  projectCapturedShippingSummary,
} from "./storefront-shipping-captured-summary.ts";
import { resolveShippingContinueState } from "./storefront-shipping-continue-state.ts";
import type {
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
  StorefrontCapturedPreviewMethod,
} from "./storefront-shipping-api.ts";

function method(overrides: Partial<StorefrontCapturedPreviewMethod> = {}): StorefrontCapturedPreviewMethod {
  return {
    methodLabel: "پیشتاز",
    serviceCode: "post",
    optionCode: "standard",
    freightAmountText: "120000",
    packagingAmountText: "5000",
    payableAtCheckoutAmountText: "125000",
    carrierCollectAmountText: null,
    currency: "IRR",
    earliestEligibleDate: "2026-11-02",
    availableDates: [
      {
        value: "2026-11-02",
        label: "روز",
        subLabel: "",
        intervalText: "14:00-15:00",
        isEarliest: true,
        slotsStatus: "ok",
        slots: [
          {
            slotKey: "2026-11-02:1-2",
            opensAt: "14:00",
            closesAt: "15:00",
            startTicks: 504000000000,
            endTicks: 540000000000,
          },
        ],
      },
    ],
    unavailableReason: null,
    confirmationSnapshotId: "snap-a",
    confirmationSourceRevision: "11",
    confirmationSelectionVersion: "2",
    confirmationAreaGroupId: "area-a",
    confirmationRateRowId: "rate-a",
    ...overrides,
  };
}

function group(overrides: Partial<StorefrontCapturedPreviewGroup> = {}): StorefrontCapturedPreviewGroup {
  return {
    groupKey: "g1",
    typeLabel: "پست",
    sellerPartyId: "seller-a",
    cartLineIds: ["l1"],
    finalGoodsAmountText: "81411464.16",
    methods: [method()],
    unavailableReason: null,
    ...overrides,
  };
}

function preview(groups: StorefrontCapturedPreviewGroup[]): StorefrontCapturedPreview {
  return {
    state: "ok",
    enabled: true,
    currency: "IRR",
    groups,
    unavailableReason: null,
    selectionRevision: "3",
    selectionEnabled: true,
    checkoutEnabled: true,
  };
}

const choice = {
  methodKey: "post\u001fstandard",
  serviceCode: "post",
  optionCode: "standard",
  date: "2026-11-02",
  slotStartTicks: 504000000000,
  slotEndTicks: 540000000000,
};

test("exact decimal addition preserves long tokens and mixed scales", () => {
  assert.equal(addExactDecimalTexts("81411464.16", "125000"), "81536464.16");
  assert.equal(addExactDecimalTexts("1.5", "2.25"), "3.75");
  assert.equal(addExactDecimalTexts("0", "0.00"), "0.00");
  assert.equal(addExactDecimalTexts("1000000000000000000", "1"), "1000000000000000001");
  assert.equal(addExactDecimalTexts("-1", "2"), null);
  assert.equal(addExactDecimalTexts("1e3", "2"), null);
  assert.equal(addExactDecimalTexts("", "2"), null);
  assert.equal(addExactDecimalTexts("12.34.5", "1"), null);
});

test("one group: selected method exposes shipping and payable while recipient incomplete", () => {
  const summary = projectCapturedShippingSummary({
    preview: preview([group()]),
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(summary.kind, "ready");
  if (summary.kind !== "ready") return;
  assert.equal(summary.shippingTotalText, "125000");
  assert.equal(summary.goodsTotalText, "81411464.16");
  assert.equal(summary.payableTotalText, "81536464.16");
  assert.equal(summary.rows.length, 1);

  const continueState = resolveShippingContinueState({
    address: {
      recipient: false,
      contactMobile: true,
      province: true,
      city: true,
      postalAddress: true,
      postalCode: true,
    },
    captured: {
      active: true,
      confirmed: true,
      preview: preview([group()]),
      choices: { g1: choice },
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(continueState.ready, false);
  assert.equal(continueState.blocker?.code, "recipient");
});

test("two groups: whole-cart totals only after both methods selected", () => {
  const g2 = group({
    groupKey: "g2",
    sellerPartyId: "seller-b",
    finalGoodsAmountText: "1000",
    methods: [method({ payableAtCheckoutAmountText: "250000", confirmationSnapshotId: "snap-b" })],
  });
  const both = preview([group(), g2]);
  const partial = projectCapturedShippingSummary({
    preview: both,
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(partial.kind, "partial");
  if (partial.kind === "partial") {
    assert.equal(partial.rows.length, 1);
    assert.equal(partial.noteCode, "need_all_methods");
  }

  const ready = projectCapturedShippingSummary({
    preview: both,
    choices: {
      g1: choice,
      g2: { ...choice, methodKey: "post\u001fstandard" },
    },
    confirmed: true,
  });
  assert.equal(ready.kind, "ready");
  if (ready.kind !== "ready") return;
  assert.equal(ready.shippingTotalText, "375000");
  assert.equal(ready.goodsTotalText, "81412464.16");
  assert.equal(ready.payableTotalText, "81787464.16");
  assert.equal(ready.rows.length, 2);
});

test("missing date still shows method fee; incomplete recipient does not erase amounts", () => {
  const noDate = {
    methodKey: choice.methodKey,
    serviceCode: choice.serviceCode,
    optionCode: choice.optionCode,
    date: null,
    slotStartTicks: null,
    slotEndTicks: null,
  };
  const summary = projectCapturedShippingSummary({
    preview: preview([group()]),
    choices: { g1: noDate },
    confirmed: true,
  });
  assert.equal(summary.kind, "ready");
  if (summary.kind === "ready") {
    assert.equal(summary.shippingTotalText, "125000");
  }
  const continueState = resolveShippingContinueState({
    address: {
      recipient: true,
      contactMobile: true,
      province: true,
      city: true,
      postalAddress: true,
      postalCode: true,
    },
    captured: {
      active: true,
      confirmed: true,
      preview: preview([group()]),
      choices: { g1: noDate },
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(continueState.ready, false);
  assert.ok(continueState.blocker?.code === "group_date" || continueState.blocker?.code === "group_slot");
});

test("pending destination hides money; invalid currency fails closed", () => {
  assert.equal(
    projectCapturedShippingSummary({
      preview: preview([group()]),
      choices: { g1: choice },
      confirmed: false,
    }).kind,
    "hidden",
  );
  const badCurrency = projectCapturedShippingSummary({
    preview: preview([group({ methods: [method({ currency: "USD" })] })]),
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(badCurrency.kind, "unavailable");
});

test("changed goods fingerprint amount invalidates until reselected current method still matches", () => {
  const summary = projectCapturedShippingSummary({
    preview: preview([group({ finalGoodsAmountText: "not-a-number" })]),
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(summary.kind, "unavailable");
});

test("carrier collect is separate and never added into payable", () => {
  const summary = projectCapturedShippingSummary({
    preview: preview([
      group({
        methods: [method({ carrierCollectAmountText: "9000", payableAtCheckoutAmountText: "125000" })],
      }),
    ]),
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(summary.kind, "ready");
  if (summary.kind !== "ready") return;
  assert.equal(summary.carrierCollectTotalText, "9000");
  assert.equal(summary.payableTotalText, "81536464.16");
});

test("zero shipping displays as exact zero token", () => {
  const summary = projectCapturedShippingSummary({
    preview: preview([group({ methods: [method({ payableAtCheckoutAmountText: "0.00" })] })]),
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(summary.kind, "ready");
  if (summary.kind === "ready") {
    assert.equal(summary.shippingIsZero, true);
    assert.equal(summary.shippingTotalText, "0.00");
  }
});
