/**
 * TB-240-R7 — حقایق متمرکز برای «دلیلِ مالکِ غیرفعال‌بودن ادامه» و «تاریخِ نشان‌داده‌شده = انتخاب واقعی».
 *
 * این فایل فقط رفتار خالص همین تصمیم را می‌سنجد؛ هیچ سرور/DOM شبیه‌سازی‌شده‌ای اینجا ادعا نمی‌شود.
 * دو حقیقت منبع (source) هم نگهبان همان دو نقص گزارش‌شدهٔ کاربر است:
 *  - یک تاریخ «نزدیک‌ترین» نباید بدون انتخاب واقعی، انتخاب‌شده به‌نظر برسد.
 *  - دکمهٔ غیرفعال باید دلیل کوتاه و خوانا نشان دهد.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  describeShippingContinueBlocker,
  resolveShippingContinueState,
  type ShippingAddressCompleteness,
  type ShippingContinueInput,
} from "./storefront-shipping-continue-state.ts";
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";
import type { StorefrontCapturedGroupChoice } from "./storefront-shipping-group-cards.tsx";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

const adrs: ShippingAddressCompleteness = {
  recipient: true,
  contactMobile: true,
  province: true,
  city: true,
  postalAddress: true,
  postalCode: true,
};

function slot(value: string, intervalText: string | null, isEarliest = false) {
  return { value, label: value, subLabel: "", intervalText, isEarliest };
}

function method(overrides: Partial<StorefrontCapturedPreview["groups"][number]["methods"][number]> = {}) {
  return {
    methodLabel: "ارسال فروشگاه",
    serviceCode: "post",
    optionCode: "standard",
    freightAmountText: "68000.00",
    packagingAmountText: "3000.00",
    payableAtCheckoutAmountText: "71000.00",
    carrierCollectAmountText: "0.00",
    currency: "IRR",
    earliestEligibleDate: "2026-10-29",
    availableDates: [slot("2026-10-29", "09:00–17:00", true), slot("2026-10-31", "10:00–16:00")],
    unavailableReason: null,
    confirmationSnapshotId: "snap-1",
    confirmationSourceRevision: "3",
    confirmationSelectionVersion: "1",
    confirmationAreaGroupId: "area-1",
    confirmationRateRowId: "row-1",
    ...overrides,
  };
}

function group(
  groupKey: string,
  sellerPartyId: string,
  methods: StorefrontCapturedPreview["groups"][number]["methods"],
  finalGoodsAmountText: string | null = "2900000.00",
) {
  return {
    groupKey,
    typeLabel: "سنگین",
    sellerPartyId,
    cartLineIds: [`line-${groupKey}`],
    finalGoodsAmountText,
    methods,
    unavailableReason: null,
  };
}

function preview(groups: StorefrontCapturedPreview["groups"]): StorefrontCapturedPreview {
  return {
    state: "ok",
    enabled: true,
    currency: "IRR",
    groups,
    unavailableReason: null,
    selectionRevision: "0",
    selectionEnabled: true,
    checkoutEnabled: false,
  };
}

const arman = group("cap|platform|store|arman|heavy", "seller-arman", [method()]);
const digi = group("cap|platform|store|digi|heavy", "seller-digi", [
  method({ serviceCode: "tipax", optionCode: "express", earliestEligibleDate: "2026-10-31" }),
]);

function input(overrides: Partial<ShippingContinueInput> = {}): ShippingContinueInput {
  return {
    address: { ...adrs },
    captured: {
      active: true,
      confirmed: true,
      preview: preview([arman]),
      choices: {},
    },
    legacy: { available: true, methodReady: false, scheduleReady: false },
    ...overrides,
  };
}

const armanChoice: StorefrontCapturedGroupChoice = {
  methodKey: "post\u001fstandard",
  serviceCode: "post",
  optionCode: "standard",
  date: "2026-10-29",
};
const digiChoice: StorefrontCapturedGroupChoice = {
  methodKey: "tipax\u001fexpress",
  serviceCode: "tipax",
  optionCode: "express",
  date: "2026-10-31",
};

test("captured: a real choice for every group enables Continue", () => {
  const state = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([arman, digi]),
      choices: { [arman.groupKey]: armanChoice, [digi.groupKey]: digiChoice },
    },
  }));
  assert.equal(state.ready, true);
  assert.equal(state.blocker, null);
});

test("captured: a missing group choice blocks with the owning group key, not a generic reason", () => {
  const state = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([arman, digi]),
      choices: { [arman.groupKey]: armanChoice },
    },
  }));
  assert.equal(state.ready, false);
  assert.equal(state.blocker?.code, "group_date");
  assert.equal(state.blocker?.groupKey, digi.groupKey);
});

test("captured: choosing one group never imposes the other group's choice", () => {
  // فقط گروه دوم انتخاب شده؛ گروه اول باید خودش دلیل باشد.
  const state = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([arman, digi]),
      choices: { [digi.groupKey]: digiChoice },
    },
  }));
  assert.equal(state.blocker?.code, "group_date");
  assert.equal(state.blocker?.groupKey, arman.groupKey);
});

test("captured: a method with no lawful date reports group_no_dates instead of asking for a date", () => {
  const noDates = group("cap|nogroup", "seller-x", [
    method({ availableDates: [], earliestEligibleDate: null, unavailableReason: "rate_row.absent" }),
  ]);
  const state = resolveShippingContinueState(input({
    captured: { active: true, confirmed: true, preview: preview([noDates]), choices: {} },
  }));
  assert.equal(state.blocker?.code, "group_no_dates");
  assert.equal(state.blocker?.groupKey, "cap|nogroup");
});

test("captured: a group with zero methods reports group_unavailable", () => {
  const empty = group("cap|empty", "seller-y", [], "120000.00");
  const state = resolveShippingContinueState(input({
    captured: { active: true, confirmed: true, preview: preview([empty]), choices: {} },
  }));
  assert.equal(state.blocker?.code, "group_unavailable");
});

test("captured: a stale/unconfirmed response blocks with preview_pending before any group reason", () => {
  const state = resolveShippingContinueState(input({
    captured: { active: true, confirmed: false, preview: preview([arman]), choices: {} },
  }));
  assert.equal(state.blocker?.code, "preview_pending");
});

test("captured: a date that is not in the current server calendar is not accepted", () => {
  const state = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([arman]),
      choices: { [arman.groupKey]: { ...armanChoice, date: "2026-11-30" } },
    },
  }));
  assert.equal(state.blocker?.code, "group_date");
});

test("captured: a multi-slot day blocks Continue until an offered slot is chosen", () => {
  const hourly = method({
    availableDates: [
      {
        ...slot("2026-10-29", "09:00–11:00", true),
        slots: [
          { slotKey: "09:00-10:00", opensAt: "09:00", closesAt: "10:00", startTicks: 324000000000, endTicks: 360000000000 },
          { slotKey: "10:00-11:00", opensAt: "10:00", closesAt: "11:00", startTicks: 360000000000, endTicks: 396000000000 },
        ],
      },
    ],
  });
  const hourlyGroup = group(arman.groupKey, "seller-arman", [hourly]);
  const dayOnly = resolveShippingContinueState(input({
    captured: { active: true, confirmed: true, preview: preview([hourlyGroup]), choices: { [arman.groupKey]: armanChoice } },
  }));
  assert.equal(dayOnly.blocker?.code, "group_slot");
  assert.equal(dayOnly.blocker?.groupKey, arman.groupKey);

  const chosen = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([hourlyGroup]),
      choices: { [arman.groupKey]: { ...armanChoice, slotStartTicks: 360000000000, slotEndTicks: 396000000000 } },
    },
  }));
  assert.equal(chosen.ready, true);

  // نوبتی که در همان پاسخ نیست (مثلاً گردشده یا ساخته‌شده) ادامه را باز نمی‌کند.
  const foreign = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([hourlyGroup]),
      choices: { [arman.groupKey]: { ...armanChoice, slotStartTicks: 300000000000, slotEndTicks: 360000000000 } },
    },
  }));
  assert.equal(foreign.blocker?.code, "group_slot");
});

test("captured: missing server confirmation reflections are reported, never silently accepted", () => {
  const broken = group("cap|broken", "seller-z", [method({ confirmationRateRowId: null })]);
  const state = resolveShippingContinueState(input({
    captured: {
      active: true,
      confirmed: true,
      preview: preview([broken]),
      choices: { "cap|broken": { ...armanChoice } },
    },
  }));
  assert.equal(state.ready, false);
  assert.equal(state.blocker?.code, "preview_incomplete");
});

test("address completeness is the first owning reason in page order", () => {
  const cases: [Partial<ShippingAddressCompleteness>, string][] = [
    [{ recipient: false }, "recipient"],
    [{ contactMobile: false }, "contact_mobile"],
    [{ province: false }, "province"],
    [{ city: false }, "city"],
    [{ postalAddress: false }, "postal_address"],
    [{ postalCode: false }, "postal_code"],
  ];
  for (const [patch, expected] of cases) {
    const state = resolveShippingContinueState(input({ address: { ...adrs, ...patch } }));
    assert.equal(state.blocker?.code, expected);
    assert.equal(state.ready, false);
  }
});

test("legacy path keeps its own blockers and never uses a captured reason", () => {
  const noMethod = resolveShippingContinueState(input({
    captured: { active: false, confirmed: false, preview: null, choices: {} },
    legacy: { available: true, methodReady: false, scheduleReady: false },
  }));
  assert.equal(noMethod.blocker?.code, "method");

  const noSchedule = resolveShippingContinueState(input({
    captured: { active: false, confirmed: false, preview: null, choices: {} },
    legacy: { available: true, methodReady: true, scheduleReady: false },
  }));
  assert.equal(noSchedule.blocker?.code, "schedule");

  const ready = resolveShippingContinueState(input({
    captured: { active: false, confirmed: false, preview: null, choices: {} },
    legacy: { available: true, methodReady: true, scheduleReady: true },
  }));
  assert.equal(ready.ready, true);

  const noMethods = resolveShippingContinueState(input({
    captured: { active: false, confirmed: false, preview: null, choices: {} },
    legacy: { available: false, methodReady: true, scheduleReady: true },
  }));
  assert.equal(noMethods.blocker?.code, "no_preview");
});

test("every blocker has a short, group-specific, non-technical Persian text", () => {
  const fa = describeShippingContinueBlocker(
    { code: "group_date", groupKey: arman.groupKey },
    { locale: "fa", groupLabel: "فروشگاه آرمان" },
  );
  assert.ok(fa && fa.includes("فروشگاه آرمان"));
  assert.ok(fa && fa.includes("تاریخ"));
  assert.ok(fa && !fa.includes("snapshot") && !fa.includes("groupKey"));

  const en = describeShippingContinueBlocker({ code: "postal_code" }, { locale: "en" });
  assert.match(String(en), /postal code/i);

  const unnamed = describeShippingContinueBlocker({ code: "group_no_dates" }, { locale: "fa" });
  assert.ok(unnamed && unnamed.includes("این گروه"));

  // TB-241: روز چندنوبتی بدون انتخاب ساعت — همان تصمیم واحد، با متن کوتاه و نام گروه.
  const slotFa = describeShippingContinueBlocker(
    { code: "group_slot", groupKey: arman.groupKey },
    { locale: "fa", groupLabel: "فروشگاه آرمان" },
  );
  assert.ok(slotFa && slotFa.includes("فروشگاه آرمان"));
  assert.ok(slotFa && slotFa.includes("ساعت"));
  assert.ok(slotFa && !slotFa.includes("slot") && !slotFa.includes("groupKey"));
  assert.equal(describeShippingContinueBlocker(null, { locale: "fa" }), null);
});

// --- نگهبان‌های منبع: دو نقص واقعی گزارش‌شده -------------------------------------------------

test("captured date chips only look selected for a real committed choice", () => {
  const cards = fs.readFileSync(path.join(root, "app/storefront/storefront-shipping-group-cards.tsx"), "utf8");
  // TB-241: خودِ کنترل نوار تاریخ/نوبت از کارت جدا شد تا هر فایل کوچک بماند؛ همان DOM است.
  const chips = fs.readFileSync(path.join(root, "app/storefront/storefront-shipping-date-chips.tsx"), "utf8");
  const surface = `${cards}\n${chips}`;
  assert.match(cards, /const recommended = method\.earliestEligibleDate \?\? method\.availableDates\[0\]\?\.value \?\? "";/);
  assert.match(cards, /const active = selected && selectedDate \? selectedDate : "";/);
  assert.match(surface, /aria-pressed=\{selected\}/);
  assert.match(surface, /data-selected=\{selected \? "true" : "false"\}/);
  // TB-242/243: پیشنهاد هرگز به‌عنوان انتخاب دیده نمی‌شود؛ خلاصه فقط پس از انتخاب واقعی است.
  // برچسب‌ها از پیام‌های locale می‌آیند (نه رشتهٔ فارسی سخت‌کد در کارت).
  assert.match(cards, /shippingDeliveryMessagesFor\(locale\)/);
  assert.match(cards, /copy\.pickDateFirst/);
  assert.match(cards, /copy\.hourNotChosen/);
  assert.doesNotMatch(surface, /بازهٔ روز پیشنهادی — هنوز انتخاب نشده/);
  // هیچ fallback خودکار به زودترین تاریخ به‌عنوان انتخاب وجود ندارد.
  assert.doesNotMatch(cards, /const active = selectedDate \?\? method\.earliestEligibleDate/);
  assert.match(cards, /data-testid=\{`shipping-group-delivery-\$\{group\.groupKey\}`\}/);
  assert.match(chips, /shippingDeliveryMessagesFor\(locale\)/);
  assert.match(chips, /copy\.deliveryHourLegend/);
  assert.match(chips, /min-h-11/);
  assert.match(chips, /whitespace-nowrap/);
});

test("the page renders one short owning blocker next to Continue from the shared decision", () => {
  const page = fs.readFileSync(path.join(root, "app/storefront/storefront-shipping.tsx"), "utf8");
  assert.match(page, /resolveShippingContinueState\(/);
  assert.match(page, /describeShippingContinueBlocker\(/);
  assert.match(page, /const isValid = continueState\.ready;/);
  assert.match(page, /data-testid="shipping-continue-blocker"/);
  assert.match(page, /aria-describedby=\{continueBlockerText \? "shipping-continue-blocker" : undefined\}/);
  // دکمه همان تصمیم واحد را مصرف می‌کند، نه یک اعتبارسنج دوم.
  assert.match(page, /disabled=\{!isValid \|\| busy\}/);
});
