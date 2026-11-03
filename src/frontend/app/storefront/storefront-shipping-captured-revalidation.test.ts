import assert from "node:assert/strict";
import test from "node:test";

import {
  formatCapturedFeeRowLabel,
  resolveCapturedRevalidationAdvisory,
} from "./storefront-shipping-captured-revalidation.ts";
import { projectCapturedShippingSummary } from "./storefront-shipping-captured-summary.ts";
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
    optionCode: null,
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
    finalGoodsAmountText: "1000",
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
  methodKey: "post\u001f",
  serviceCode: "post",
  optionCode: null as string | null,
  date: "2026-11-02",
  slotStartTicks: 504000000000,
  slotEndTicks: 540000000000,
};

const sellers = { "seller-a": "فروشگاه آرمان", "seller-b": "فروشگاه نیلوا" };
const staleMsg = "انتخاب ارسال دیگر معتبر نیست؛ لطفاً دوباره انتخاب کنید.";

test("revalidation advisory clears after both groups are lawfully reselected; totals remain", () => {
  const both = preview([
    group(),
    group({
      groupKey: "g2",
      sellerPartyId: "seller-b",
      finalGoodsAmountText: "2000",
      methods: [method({ confirmationSnapshotId: "snap-b", confirmationRateRowId: "rate-b" })],
    }),
  ]);
  const incomplete = resolveCapturedRevalidationAdvisory({
    confirmed: true,
    preview: both,
    choices: {},
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.ok(incomplete);
  assert.equal(incomplete?.kind, "group_reselect");

  const choices = { g1: choice, g2: { ...choice } };
  const cleared = resolveCapturedRevalidationAdvisory({
    confirmed: true,
    preview: both,
    choices,
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.equal(cleared, null);

  const summary = projectCapturedShippingSummary({ preview: both, choices, confirmed: true });
  assert.equal(summary.kind, "ready");
  if (summary.kind === "ready") {
    assert.equal(summary.shippingTotalText, "250000");
    assert.equal(summary.rows.length, 2);
  }
});

test("one invalid group withholds ready total and names the owning seller", () => {
  const both = preview([
    group(),
    group({
      groupKey: "g2",
      sellerPartyId: "seller-b",
      finalGoodsAmountText: "2000",
      methods: [method({ confirmationSnapshotId: "snap-b" })],
    }),
  ]);
  const advisory = resolveCapturedRevalidationAdvisory({
    confirmed: true,
    preview: both,
    choices: { g1: choice },
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.ok(advisory);
  assert.match(advisory!.text, /نیلوا|آرمان|این گروه|Reselect|دوباره/);
  assert.ok(advisory!.groupKey === "g2" || advisory!.groupKey === "g1");

  const summary = projectCapturedShippingSummary({
    preview: both,
    choices: { g1: choice },
    confirmed: true,
  });
  assert.equal(summary.kind, "partial");
});

test("two equal method labels render distinct seller fee labels", () => {
  const a = formatCapturedFeeRowLabel({
    methodLabel: "پیشتاز",
    sellerPartyId: "seller-a",
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  const b = formatCapturedFeeRowLabel({
    methodLabel: "پیشتاز",
    sellerPartyId: "seller-b",
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.equal(a, "فروشگاه آرمان — پیشتاز");
  assert.equal(b, "فروشگاه نیلوا — پیشتاز");
  assert.notEqual(a, b);
});

test("recipient blocker coexists with ready money and no revalidation advisory when selection complete", () => {
  const one = preview([group()]);
  const choices = { g1: choice };
  const advisory = resolveCapturedRevalidationAdvisory({
    confirmed: true,
    preview: one,
    choices,
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.equal(advisory, null);
  const summary = projectCapturedShippingSummary({ preview: one, choices, confirmed: true });
  assert.equal(summary.kind, "ready");
  const cont = resolveShippingContinueState({
    address: {
      recipient: false,
      contactMobile: true,
      province: true,
      city: true,
      postalAddress: true,
      postalCode: true,
    },
    captured: { active: true, confirmed: true, preview: one, choices },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(cont.ready, false);
  assert.equal(cont.blocker?.code, "recipient");
});

test("operation errors are orthogonal: advisory resolution never clears a network message token", () => {
  const networkError = "network boom";
  const one = preview([group()]);
  // Selecting a method only affects advisory derivation; callers keep their own error string.
  const afterSelect = resolveCapturedRevalidationAdvisory({
    confirmed: true,
    preview: one,
    choices: { g1: choice },
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.equal(afterSelect, null);
  assert.equal(networkError, "network boom");
});

test("unconfirmed destination suppresses advisory even with a projection message", () => {
  const one = preview([group()]);
  const advisory = resolveCapturedRevalidationAdvisory({
    confirmed: false,
    preview: one,
    choices: {},
    projectionMessage: staleMsg,
    sellerLabelByPartyId: sellers,
    locale: "fa",
  });
  assert.equal(advisory, null);
});

test("blank projection message never invents an advisory", () => {
  assert.equal(
    resolveCapturedRevalidationAdvisory({
      confirmed: true,
      preview: preview([group()]),
      choices: {},
      projectionMessage: null,
      sellerLabelByPartyId: sellers,
      locale: "fa",
    }),
    null,
  );
});
