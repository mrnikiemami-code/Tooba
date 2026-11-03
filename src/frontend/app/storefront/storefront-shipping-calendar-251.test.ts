/**
 * TB-251 — group delivery calendar closure + decode honesty + Continue blockers.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { mapStorefrontCart } from "./storefront-cart-map.ts";
import { groupCartLinesBySeller, resolveCartGoodsSubtotal } from "./storefront-cart-grouping.ts";
import { mapShippingProjection } from "./storefront-shipping-api.ts";
import {
  describeShippingContinueBlocker,
  resolveShippingContinueState,
} from "./storefront-shipping-continue-state.ts";
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";

const HERE = dirname(fileURLToPath(import.meta.url));

function projection(capturedPreview: unknown) {
  return mapShippingProjection({
    cartId: "44444444-4444-4444-8444-444444444444",
    cartVersion: 1,
    currency: "IRR",
    itemCount: 2,
    subtotalExclusiveOfTax: 100,
    sellerCount: 2,
    maxSellerPreparationDays: 1,
    methods: [],
    dates: [],
    timeWindows: [],
    capturedPreview,
  });
}

function day(overrides: Record<string, unknown> = {}) {
  return {
    date: "2026-11-09",
    label: "یکشنبه",
    subLabel: "۱۴۰۵/۸/۱۸",
    intervalText: "09:00–17:00",
    isEarliest: true,
    slots: [
      {
        slotKey: "2026-11-09:324000000000-612000000000",
        opensAt: "09:00",
        closesAt: "17:00",
        startTicks: 324000000000,
        endTicks: 612000000000,
      },
    ],
    ...overrides,
  };
}

function method(overrides: Record<string, unknown> = {}) {
  return {
    methodLabel: "پیشتاز",
    serviceCode: "post",
    optionCode: null,
    freightAmount: "120000",
    packagingAmount: "5000",
    payableAtCheckoutAmount: "125000",
    carrierCollectAmount: "0",
    currency: "IRR",
    earliestEligibleDate: "2026-11-09",
    availableDates: [day()],
    unavailableReason: null,
    confirmationSnapshotId: "01a186f7-dd7a-7000-8000-000000000001",
    confirmationSourceRevision: "11",
    confirmationSelectionVersion: "2",
    confirmationAreaGroupId: "01a186f7-dd7a-7000-8000-0000000000aa",
    confirmationRateRowId: "01a186f7-dd7a-7000-8000-0000000000bb",
    ...overrides,
  };
}

function group(groupKey: string, sellerPartyId: string, lineId: string, methods: unknown[]) {
  return {
    groupKey,
    typeLabel: "عادی",
    sellerPartyId,
    cartLineIds: [lineId],
    finalGoodsAmountAfterDiscountAndTax: "1000000",
    methods,
    unavailableReason: null,
  };
}

function preview(groups: unknown[]): Record<string, unknown> {
  return {
    enabled: true,
    currency: "IRR",
    groups,
    noticeReason: null,
    selectionRevision: "0",
    selectionEnabled: true,
    checkoutEnabled: false,
  };
}

const address = {
  recipient: true,
  contactMobile: true,
  province: true,
  city: true,
  postalAddress: true,
  postalCode: true,
};

test("null-duration full-window day decodes as one ok slot", () => {
  const mapped = projection(
    preview([
      group("g-a", "seller-a", "line-a", [method()]),
    ]),
  ).capturedPreview!;
  assert.equal(mapped.state, "ok");
  const date = mapped.groups[0]!.methods[0]!.availableDates[0]!;
  assert.equal(date.slotsStatus, "ok");
  assert.equal(date.slots.length, 1);
  assert.equal(date.slots[0]!.opensAt, "09:00");
  assert.equal(date.slots[0]!.closesAt, "17:00");
});

test("hourly day keeps every server slot; string ticks that are exact SafeIntegers decode", () => {
  const mapped = projection(
    preview([
      group("g-a", "seller-a", "line-a", [
        method({
          availableDates: [
            day({
              intervalText: "09:00–11:00",
              slots: [
                {
                  slotKey: "a",
                  opensAt: "09:00",
                  closesAt: "10:00",
                  startTicks: "324000000000",
                  endTicks: "360000000000",
                },
                {
                  slotKey: "b",
                  opensAt: "10:00",
                  closesAt: "11:00",
                  startTicks: 360000000000,
                  endTicks: 396000000000,
                },
              ],
            }),
          ],
        }),
      ]),
    ]),
  ).capturedPreview!;
  const date = mapped.groups[0]!.methods[0]!.availableDates[0]!;
  assert.equal(date.slotsStatus, "ok");
  assert.equal(date.slots.length, 2);
  assert.equal(date.slots[0]!.startTicks, 324000000000);
});

test("absent / empty / malformed slots are distinguished and never invent hours", () => {
  const absent = projection(
    preview([group("g1", "s1", "l1", [method({ availableDates: [day({ slots: null })] })])]),
  ).capturedPreview!;
  assert.equal(absent.groups[0]!.methods[0]!.availableDates[0]!.slotsStatus, "absent");
  assert.deepEqual(absent.groups[0]!.methods[0]!.availableDates[0]!.slots, []);

  const empty = projection(
    preview([group("g2", "s2", "l2", [method({ availableDates: [day({ slots: [] })] })])]),
  ).capturedPreview!;
  assert.equal(empty.groups[0]!.methods[0]!.availableDates[0]!.slotsStatus, "empty");

  const malformed = projection(
    preview([
      group("g3", "s3", "l3", [
        method({
          availableDates: [
            day({
              slots: [{ slotKey: "x", opensAt: "09:00", closesAt: "10:00", startTicks: "nope", endTicks: 1 }],
            }),
          ],
        }),
      ]),
    ]),
  ).capturedPreview!;
  assert.equal(malformed.groups[0]!.methods[0]!.availableDates[0]!.slotsStatus, "malformed");
  assert.deepEqual(malformed.groups[0]!.methods[0]!.availableDates[0]!.slots, []);
});

test("two groups keep independent calendars after method/date/slot choice", () => {
  const hourly = method({
    methodLabel: "پیشتاز",
    availableDates: [
      day({
        intervalText: "09:00–11:00",
        slots: [
          { slotKey: "a", opensAt: "09:00", closesAt: "10:00", startTicks: 324000000000, endTicks: 360000000000 },
          { slotKey: "b", opensAt: "10:00", closesAt: "11:00", startTicks: 360000000000, endTicks: 396000000000 },
        ],
      }),
    ],
  });
  const full = method({ methodLabel: "پیشتاز" });
  const mapped = projection(
    preview([
      group(
        "cap|platform|store|01a030d140cb70008abe6d31739956c5|type",
        "01a030d1-40cb-7000-8abe-6d31739956c5",
        "line-arman",
        [hourly],
      ),
      group(
        "cap|platform|store|01a0ffff000170008000000000000001|type",
        "01a0ffff-0001-7000-8000-000000000001",
        "line-nilva",
        [full],
      ),
    ]),
  ).capturedPreview as StorefrontCapturedPreview;
  assert.equal(mapped.groups.length, 2);
  assert.equal(mapped.groups[0]!.methods[0]!.availableDates[0]!.slots.length, 2);
  assert.equal(mapped.groups[1]!.methods[0]!.availableDates[0]!.slots.length, 1);

  const choices = {
    [mapped.groups[0]!.groupKey]: {
      methodKey: "post\u001f",
      serviceCode: "post",
      optionCode: null,
      date: "2026-11-09",
      slotStartTicks: 360000000000,
      slotEndTicks: 396000000000,
    },
    [mapped.groups[1]!.groupKey]: {
      methodKey: "post\u001f",
      serviceCode: "post",
      optionCode: null,
      date: "2026-11-09",
      slotStartTicks: 324000000000,
      slotEndTicks: 612000000000,
    },
  };
  const state = resolveShippingContinueState({
    address,
    captured: { active: true, confirmed: true, preview: mapped, choices },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(state.ready, true);

  const onlyA = resolveShippingContinueState({
    address,
    captured: {
      active: true,
      confirmed: true,
      preview: mapped,
      choices: { [mapped.groups[0]!.groupKey]: choices[mapped.groups[0]!.groupKey]! },
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(onlyA.blocker?.code, "group_date");
  assert.equal(onlyA.blocker?.groupKey, mapped.groups[1]!.groupKey);
});

test("empty and malformed chosen-day slots own Continue blockers", () => {
  const emptyPreview = projection(
    preview([group("g-empty", "s-empty", "l-empty", [method({ availableDates: [day({ slots: [] })] })])]),
  ).capturedPreview as StorefrontCapturedPreview;
  const emptyState = resolveShippingContinueState({
    address,
    captured: {
      active: true,
      confirmed: true,
      preview: emptyPreview,
      choices: {
        "g-empty": {
          methodKey: "post\u001f",
          serviceCode: "post",
          optionCode: null,
          date: "2026-11-09",
          slotStartTicks: null,
          slotEndTicks: null,
        },
      },
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(emptyState.blocker?.code, "group_slots_unavailable");
  assert.match(
    describeShippingContinueBlocker(emptyState.blocker, { locale: "fa", groupLabel: "آرمان" }) ?? "",
    /ساعت تحویل/,
  );

  const badPreview = projection(
    preview([
      group("g-bad", "s-bad", "l-bad", [
        method({
          availableDates: [
            day({
              slots: [{ slotKey: "x", opensAt: "09:00", closesAt: "10:00", startTicks: {}, endTicks: 1 }],
            }),
          ],
        }),
      ]),
    ]),
  ).capturedPreview as StorefrontCapturedPreview;
  const badState = resolveShippingContinueState({
    address,
    captured: {
      active: true,
      confirmed: true,
      preview: badPreview,
      choices: {
        "g-bad": {
          methodKey: "post\u001f",
          serviceCode: "post",
          optionCode: null,
          date: "2026-11-09",
          slotStartTicks: null,
          slotEndTicks: null,
        },
      },
    },
    legacy: { available: false, methodReady: false, scheduleReady: false },
  });
  assert.equal(badState.blocker?.code, "group_slots_malformed");
});

test("CapturedMethodRow renders owning slot blockers instead of silent null", () => {
  const source = readFileSync(join(HERE, "storefront-shipping-group-cards.tsx"), "utf8");
  assert.match(source, /shipping-group-captured-slots-unavailable-/);
  assert.match(source, /shipping-group-captured-slots-malformed-/);
  assert.match(source, /activeSlots\.length > 0 && activeDate\.slotsStatus === "ok"/);
});

test("mapStorefrontCart never labels a sole USD total as IRR", () => {
  const mapped = mapStorefrontCart({
    cartId: "c-usd",
    version: 1,
    defaultCurrency: "IRR",
    totalsByCurrency: [{ currency: "USD", subtotalExclusiveOfTax: 12.5 }],
    lines: [
      {
        lineId: "l1",
        offerId: "o1",
        catalogVariantId: "v1",
        sellerPartyId: "s1",
        title: "X",
        sellerDisplayName: "S",
        quantity: 1,
        unitAmountExclusiveOfTax: 12.5,
        lineAmountExclusiveOfTax: 12.5,
        currency: "USD",
      },
    ],
  });
  assert.ok(mapped);
  assert.equal(mapped!.currency, "IRR");
  assert.equal(mapped!.goodsSubtotalAvailable, false);
  assert.equal(mapped!.subtotalExclusiveOfTax, null);

  const resolved = resolveCartGoodsSubtotal({
    currency: "IRR",
    lines: [{ lineAmountExclusiveOfTax: 12.5 }],
    totalsByCurrency: [{ currency: "USD", subtotalExclusiveOfTax: 12.5 }],
  });
  assert.deepEqual(resolved, { state: "unavailable", currency: "IRR" });
});

test("groupCartLinesBySeller never sums mixed currencies into one subtotal", () => {
  const sections = groupCartLinesBySeller([
    {
      lineId: "a",
      offerId: "o1",
      catalogVariantId: "v1",
      sellerPartyId: "seller-1",
      productId: null,
      productSlug: null,
      title: "IRR line",
      sellerDisplayName: "فروشگاه",
      mediaAssetId: null,
      quantity: 1,
      unitAmountExclusiveOfTax: 1000,
      lineAmountExclusiveOfTax: 1000,
      currency: "IRR",
      quotedTaxExclusive: true,
      unitCode: null,
      unitDisplayName: null,
      quantityDecimalPlaces: 0,
      quantityStep: null,
      availability: "Available",
    },
    {
      lineId: "b",
      offerId: "o2",
      catalogVariantId: "v2",
      sellerPartyId: "seller-1",
      productId: null,
      productSlug: null,
      title: "USD line",
      sellerDisplayName: "فروشگاه",
      mediaAssetId: null,
      quantity: 1,
      unitAmountExclusiveOfTax: 2,
      lineAmountExclusiveOfTax: 2,
      currency: "USD",
      quotedTaxExclusive: true,
      unitCode: null,
      unitDisplayName: null,
      quantityDecimalPlaces: 0,
      quantityStep: null,
      availability: "Available",
    },
  ]);
  assert.equal(sections.length, 1);
  assert.equal(sections[0]!.sellerSubtotalExclusiveOfTax, null);
  assert.equal(sections[0]!.lines.length, 2);
});
