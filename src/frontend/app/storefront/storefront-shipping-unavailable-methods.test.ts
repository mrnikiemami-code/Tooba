/**
 * TB-165 — آزمون متمرکز توضیح «روش بدون نرخ معتبر» در استورفرانت.
 *
 * سنجه‌ها:
 * - گیت خاموش/پاسخ قدیمی (فیلد غایب یا null) ⇒ هیچ توضیحی و رفتار دقیقاً قبلی.
 * - هویت سطح-روش: دو فروشنده با روش یکسان یک توضیح می‌گیرند، نه دو؛ و به روش/فروشندهٔ همسایه نسبت
 *   داده نمی‌شود.
 * - روش واجد شرایط سرور هرگز با ردیف توضیحی غیرفعال/جایگزین نمی‌شود.
 * - کد دلیل ناشناخته/خالی ⇒ متن عمومی راستگو؛ هیچ مبلغ/«رایگان» ساخته نمی‌شود.
 * - روش unavailable قابل انتخاب نمی‌شود و گروه دیگر با از دست رفتن یکی نامعتبر نمی‌شود.
 * - سطوح RTL و فقط-خواندنی: بلوک توضیح هیچ دکمه/هندلر کلیک ندارد.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { mapShippingProjection } from "./storefront-shipping-api.ts";
import { buildShippingItemGroups, isItemGroupingComplete } from "./storefront-shipping-item-groups.ts";
import { resolveGroupSchedules } from "./storefront-shipping-group-schedule.ts";
import {
  SHIPPING_UNAVAILABLE_FALLBACK_TEXT,
  resolveShippingUnavailableNotices,
  shippingUnavailableReasonText,
} from "./storefront-shipping-unavailable-methods.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const noticeSource = fs.readFileSync(path.join(here, "storefront-shipping-unavailable-methods.tsx"), "utf8");
const groupCardsSource = fs.readFileSync(path.join(here, "storefront-shipping-group-cards.tsx"), "utf8");
const singleGroupSource = fs.readFileSync(path.join(here, "storefront-shipping-single-group-section.tsx"), "utf8");

const SELLER_A = "9b3f4a1c-8f2e-4c77-9d31-2ac0d0d1a001";
const SELLER_B = "9b3f4a1c-8f2e-4c77-9d31-2ac0d0d1a002";
const WINDOWS = [{ value: "09-12", label: "۹ تا ۱۲" }];

function projectionWith(input: {
  unavailableMethods?: { methodCode: string; reasonCode: string }[];
  methods?: { methodCode: string; label: string; priceAmount: number }[];
  horizons?: { sellerPartyId: string; methodCode: string; minimum: string }[];
}) {
  return mapShippingProjection({
    cartId: "cart-1",
    cartVersion: 2,
    currency: "IRR",
    itemCount: 2,
    subtotalExclusiveOfTax: 1_000_000,
    sellerCount: 2,
    methods:
      input.methods ??
      [{ methodCode: "tipax:standard", label: "تیپاکس عادی", serviceCode: "tipax", priceAmount: 205_000, leadDays: 3 }],
    deliveryDates: [],
    deliveryTimeWindows: [],
    shippingGroupHorizons: (input.horizons ?? []).map((row) => ({
      groupKey: `${row.sellerPartyId}|${row.methodCode}`,
      shippingGroupId: null,
      sellerPartyId: row.sellerPartyId,
      methodCode: row.methodCode,
      minimumDeliveryDate: row.minimum,
      eligibleDates: [{ value: row.minimum, label: row.minimum, subLabel: "", isEarliest: true }],
      eligibleTimeWindows: WINDOWS,
      selectedDeliveryDate: null,
      selectedDeliveryTimeWindow: null,
      leadDays: 3,
      sellerPreparationDays: 2,
    })),
    ...(input.unavailableMethods ? { unavailableMethods: input.unavailableMethods } : {}),
  });
}

test("گیت خاموش/پاسخ قدیمی: فیلد غایب یا null هیچ توضیحی نمی‌سازد و روش‌ها دست‌نخورده‌اند", () => {
  const absent = projectionWith({});
  assert.equal(absent.unavailableMethods, null);
  assert.deepEqual(resolveShippingUnavailableNotices({ unavailableMethods: absent.unavailableMethods, methods: absent.methods }), []);

  const explicitNull = mapShippingProjection({ cartId: "c", methods: [], unavailableMethods: null });
  assert.equal(explicitNull.unavailableMethods, null);
  assert.equal(explicitNull.methods.length, 0);
});

test("نگاشت UnavailableMethods: فقط ردیف‌های با methodCode معتبر، بدون مبلغ", () => {
  const projection = projectionWith({
    unavailableMethods: [
      { methodCode: "tipax:express", reasonCode: "shipping.quote.owned_option_not_priced" },
      { methodCode: "", reasonCode: "shipping.coverage.missing" },
    ],
  });
  assert.deepEqual(projection.unavailableMethods, [
    { methodCode: "tipax:express", reasonCode: "shipping.quote.owned_option_not_priced" },
  ]);
  const notices = resolveShippingUnavailableNotices({
    unavailableMethods: projection.unavailableMethods,
    methods: projection.methods,
  });
  assert.equal(notices.length, 1);
  assert.equal(notices[0]!.methodCode, "tipax:express");
  assert.equal("priceAmount" in notices[0]!, false);
});

test("هویت سطح-روش: دو فروشنده با روش یکسان یک توضیح می‌گیرند و به روش همسایه نسبت داده نمی‌شود", () => {
  const notices = resolveShippingUnavailableNotices({
    unavailableMethods: [
      { methodCode: "tipax:express", reasonCode: "shipping.quote.owned_option_not_priced" },
      { methodCode: "tipax:express", reasonCode: "shipping.quote.owned_option_not_priced" },
    ],
    methods: [{ methodCode: "tipax:standard", label: "تیپاکس عادی", serviceCode: "tipax", iconKey: "truck", priceAmount: 205_000, leadDays: 3, isFree: false, estimationLabel: "" }],
  });
  assert.equal(notices.length, 1);
  assert.equal(notices[0]!.methodCode, "tipax:express");
  // روش واجد شرایط هرگز پیام رد خود را از روش دیگر نمی‌گیرد.
  assert.equal(notices.some((n) => n.methodCode === "tipax:standard"), false);
  // بدون برچسب سرور برای روش ردشده، برچسب ساخته نمی‌شود و خود کد روش نمایش داده می‌شود.
  assert.equal(notices[0]!.label, "tipax:express");
});

test("روش واجد شرایط سرور با ردیف توضیحی غیرفعال نمی‌شود (توضیح جایگزین روش نمی‌شود)", () => {
  const notices = resolveShippingUnavailableNotices({
    unavailableMethods: [{ methodCode: "tipax:standard", reasonCode: "shipping.coverage.method_restricted" }],
    methods: [{ methodCode: "tipax:standard", label: "تیپاکس عادی", serviceCode: "tipax", iconKey: "truck", priceAmount: 205_000, leadDays: 3, isFree: false, estimationLabel: "" }],
  });
  assert.deepEqual(notices, []);
});

test("ترجمهٔ کد پایدار و متن عمومی برای کد ناشناخته/خالی؛ هیچ مبلغ یا «رایگان»", () => {
  assert.equal(
    shippingUnavailableReasonText("shipping.quote.owned_option_not_priced"),
    "نرخ اختصاصی این گزینهٔ خدمت در تنظیمات حمل تعریف نشده است.",
  );
  assert.equal(shippingUnavailableReasonText("shipping.unknown.future_code"), SHIPPING_UNAVAILABLE_FALLBACK_TEXT);
  assert.equal(shippingUnavailableReasonText(""), SHIPPING_UNAVAILABLE_FALLBACK_TEXT);
  assert.equal(shippingUnavailableReasonText(null), SHIPPING_UNAVAILABLE_FALLBACK_TEXT);
  assert.equal(SHIPPING_UNAVAILABLE_FALLBACK_TEXT.includes("رایگان"), false);
});

test("روش unavailable قابل انتخاب نمی‌شود؛ گروه A روش ندارد و گروه B دست‌نخورده می‌ماند", () => {
  const projection = projectionWith({
    unavailableMethods: [{ methodCode: "tipax:express", reasonCode: "shipping.coverage.method_restricted" }],
    horizons: [
      { sellerPartyId: SELLER_A, methodCode: "tipax:standard", minimum: "2026-10-21" },
      { sellerPartyId: SELLER_B, methodCode: "tipax:standard", minimum: "2026-10-22" },
    ],
  });
  const lines = [
    { lineId: "line-a", sellerPartyId: SELLER_A, sellerDisplayName: "فروشنده الف", title: "کالا", quantity: 1 },
    { lineId: "line-b", sellerPartyId: SELLER_B, sellerDisplayName: "فروشنده ب", title: "کالا", quantity: 1 },
  ];
  // تلاش برای انتخاب روشی که سرور آن را واجد شرایط نکرده: هیچ گروهی از آن ساخته نمی‌شود.
  const grouping = buildShippingItemGroups({
    lines: lines as never,
    methodCodeByLine: { "line-a": "tipax:express", "line-b": "tipax:standard" },
    methods: projection.methods,
  });
  assert.equal(grouping.groups.some((g) => g.methodCode === "tipax:express"), false);
  assert.equal(isItemGroupingComplete(grouping, lines as never), false);

  // گروه B (روش معتبر) مستقل و کامل می‌ماند؛ از دست رفتن اعتبار گروه A آن را پاک نمی‌کند.
  const groups = [
    { key: `${SELLER_A}|tipax:standard`, sellerPartyId: SELLER_A, methodCode: "tipax:standard" },
    { key: `${SELLER_B}|tipax:standard`, sellerPartyId: SELLER_B, methodCode: "tipax:standard" },
  ];
  const schedules = resolveGroupSchedules({ isGroupedCart: true, projection, groups });
  assert.equal(schedules[groups[0]!.key]?.resolved, true);
  assert.equal(schedules[groups[1]!.key]?.resolved, true);
  assert.equal(schedules[groups[0]!.key]?.minimumDeliveryDate, "2026-10-21");
  assert.equal(schedules[groups[1]!.key]?.minimumDeliveryDate, "2026-10-22");
});

test("فقط-خواندنی و RTL: بلوک توضیح هیچ دکمه/انتخاب‌کننده‌ای ندارد و در هر دو سطح روش وصل است", () => {
  assert.equal(/<button/i.test(noticeSource), false);
  assert.equal(/onClick/.test(noticeSource), false);
  assert.equal(/aria-disabled="true"/.test(noticeSource), true);
  assert.equal(/text-right/.test(noticeSource), true);
  assert.equal(/data-shipping-unavailable="true"/.test(noticeSource), true);
  // مسیر تک‌گروهی و مسیر چندگروهی هر دو توضیح را نشان می‌دهند.
  assert.equal(singleGroupSource.includes("StorefrontShippingUnavailableMethods"), true);
  assert.equal(groupCardsSource.includes("StorefrontShippingUnavailableMethods"), true);
  // کارت گروه هرگز از ردیف توضیحی روش/مبلغ نمی‌سازد.
  assert.equal(/unavailableNotices\.map|unavailableNotices\.find/.test(groupCardsSource), false);
});
