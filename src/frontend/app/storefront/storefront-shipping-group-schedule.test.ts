/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — آزمون متمرکز زمان‌بندی مستقل هر گروه ارسال.
 *
 * سنجه‌ها:
 * - دو فروشنده با **روش یکسان** و آماده‌سازی متفاوت ⇒ دو افق مستقل با دو «اولین نوبت ممکن» متمایز.
 * - هیچ تلاقی/دیرترین-حداقل سراسری و هیچ تاریخ/بازهٔ ساختگی در کلاینت.
 * - استقلال انتخاب‌ها: تغییر تاریخ/بازهٔ یک گروه هیچ فیلدی از گروه دیگر را تغییر نمی‌دهد.
 * - بازیابی رفرش/برگشت از انتخاب ذخیره‌شدهٔ **همان گروه** و پاک‌سازی هدفمند مقدار نامعتبر.
 * - نگاشت دقیق فیلدهای قرارداد سرور (`shippingGroupHorizons`) و مسیر تک‌گروهی سازگار.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { mapShippingProjection, type StorefrontShippingProjection } from "./storefront-shipping-api.ts";
import {
  allGroupSchedulesComplete,
  findGroupHorizon,
  isGroupScheduleComplete,
  mergeGroupSelections,
  pickGroupDeliveryDate,
  pickGroupDeliveryWindow,
  resolveGroupSchedules,
  setGroupSelection,
  type StorefrontShippingGroupSelection,
} from "./storefront-shipping-group-schedule.ts";
import {
  isShippingDateHorizonComplete,
  pickEligibleDeliveryDate,
  pickEligibleDeliveryWindow,
  resolveSingleGroupHorizon,
} from "./storefront-shipping-group-horizon.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const shippingSource = fs.readFileSync(path.join(here, "storefront-shipping.tsx"), "utf8");
const cardsSource = fs.readFileSync(path.join(here, "storefront-shipping-group-cards.tsx"), "utf8");
const scheduleSource = fs.readFileSync(path.join(here, "storefront-shipping-group-schedule.ts"), "utf8");
const horizonSource = fs.readFileSync(path.join(here, "storefront-shipping-group-horizon.ts"), "utf8");
const singleGroupSource = fs.readFileSync(path.join(here, "storefront-shipping-single-group-section.tsx"), "utf8");
const deliveryCardSource = fs.readFileSync(path.join(here, "storefront-shipping-delivery-card.tsx"), "utf8");

const SELLER_A = "9b3f4a1c-8f2e-4c77-9d31-2ac0d0d1a001";
const SELLER_B = "9b3f4a1c-8f2e-4c77-9d31-2ac0d0d1a002";
const WINDOWS = [
  { value: "09-12", label: "۹ تا ۱۲" },
  { value: "12-15", label: "۱۲ تا ۱۵" },
];

function projectionFrom(input: {
  sellers: { sellerPartyId: string; methodCode: string; minimum: string; dates: string[]; selectedDate?: string }[];
}): StorefrontShippingProjection {
  return mapShippingProjection({
    cartId: "cart-1",
    cartVersion: 4,
    currency: "IRR",
    itemCount: 2,
    subtotalExclusiveOfTax: 1_000_000,
    sellerCount: 2,
    maxSellerPreparationDays: 3,
    methods: [
      { methodCode: "tipax:standard", label: "تیپاکس عادی", serviceCode: "tipax", priceAmount: 180_000, leadDays: 3 },
      { methodCode: "post:express", label: "پست پیشتاز", serviceCode: "post", priceAmount: 200_000, leadDays: 2 },
    ],
    deliveryDates: [],
    deliveryTimeWindows: [],
    shippingGroupHorizons: input.sellers.map((row) => ({
      groupKey: `${row.sellerPartyId.replace(/-/g, "").toLowerCase()}|${row.methodCode}`,
      shippingGroupId: null,
      sellerPartyId: row.sellerPartyId,
      methodCode: row.methodCode,
      minimumDeliveryDate: row.minimum,
      eligibleDates: row.dates.map((value, index) => ({
        value,
        label: value,
        subLabel: "",
        isEarliest: index === 0,
      })),
      eligibleTimeWindows: WINDOWS,
      selectedDeliveryDate: row.selectedDate ?? null,
      selectedDeliveryTimeWindow: row.selectedDate ? "09-12" : null,
      leadDays: 3,
      sellerPreparationDays: row.minimum === "2026-10-21" ? 2 : 3,
    })),
  });
}

test("دو فروشنده با روش یکسان: دو افق مستقل و دو «اولین نوبت ممکن» متمایز", () => {
  const projection = projectionFrom({
    sellers: [
      { sellerPartyId: SELLER_A, methodCode: "tipax:standard", minimum: "2026-10-21", dates: ["2026-10-21", "2026-10-22"] },
      { sellerPartyId: SELLER_B, methodCode: "tipax:standard", minimum: "2026-10-22", dates: ["2026-10-22", "2026-10-23"] },
    ],
  });

  const groups = [
    { key: `${SELLER_A}|tipax:standard`, sellerPartyId: SELLER_A, methodCode: "tipax:standard" },
    { key: `${SELLER_B}|tipax:standard`, sellerPartyId: SELLER_B, methodCode: "tipax:standard" },
  ];
  const schedules = resolveGroupSchedules({ isGroupedCart: true, projection, groups });

  assert.equal(schedules[groups[0].key].minimumDeliveryDate, "2026-10-21");
  assert.equal(schedules[groups[1].key].minimumDeliveryDate, "2026-10-22");
  assert.deepEqual(schedules[groups[0].key].dates.map((d) => d.value), ["2026-10-21", "2026-10-22"]);
  assert.deepEqual(schedules[groups[1].key].dates.map((d) => d.value), ["2026-10-22", "2026-10-23"]);
  assert.equal(schedules[groups[0].key].sellerPreparationDays, 2);
  assert.equal(schedules[groups[1].key].sellerPreparationDays, 3);
  // هیچ تلاقی/دیرترین-حداقل سراسری: افق هر گروه دست‌نخورده و مستقل است.
  assert.notDeepEqual(schedules[groups[0].key].dates, schedules[groups[1].key].dates);
  assert.equal(schedules[groups[0].key].resolved, true);
});

test("گروه بدون ردیف سرور: افق resolved نمی‌شود و تاریخ حدسی ساخته نمی‌شود", () => {
  const projection = projectionFrom({
    sellers: [
      { sellerPartyId: SELLER_A, methodCode: "tipax:standard", minimum: "2026-10-21", dates: ["2026-10-21"] },
    ],
  });

  const schedules = resolveGroupSchedules({
    isGroupedCart: true,
    projection,
    groups: [{ key: "b|post:express", sellerPartyId: SELLER_B, methodCode: "post:express" }],
  });

  const only = schedules["b|post:express"];
  assert.equal(only.resolved, false);
  assert.deepEqual(only.dates, []);
  assert.equal(isGroupScheduleComplete(only, "2026-10-21", "09-12"), false);
  assert.equal(
    allGroupSchedulesComplete({
      schedules,
      selections: { "b|post:express": { deliveryDate: "2026-10-21", deliveryTime: "09-12" } },
    }),
    false,
  );
});

test("استقلال انتخاب‌ها: تغییر گروه A هیچ فیلدی از گروه B را عوض نمی‌کند", () => {
  const projection = projectionFrom({
    sellers: [
      { sellerPartyId: SELLER_A, methodCode: "tipax:standard", minimum: "2026-10-21", dates: ["2026-10-21", "2026-10-22"] },
      { sellerPartyId: SELLER_B, methodCode: "tipax:standard", minimum: "2026-10-22", dates: ["2026-10-22", "2026-10-23"] },
    ],
  });
  const groups = [
    { key: "a", sellerPartyId: SELLER_A, methodCode: "tipax:standard" },
    { key: "b", sellerPartyId: SELLER_B, methodCode: "tipax:standard" },
  ];
  const schedules = resolveGroupSchedules({ isGroupedCart: true, projection, groups });

  let selections: Record<string, StorefrontShippingGroupSelection> = mergeGroupSelections({
    previous: {},
    schedules,
  });
  // نزدیک‌ترین تاریخ مجاز هر گروه از خودش (نه مشترک/سراسری).
  assert.equal(selections.a.deliveryDate, "2026-10-21");
  assert.equal(selections.b.deliveryDate, "2026-10-22");
  assert.equal(selections.a.deliveryTime, "");

  selections = setGroupSelection(selections, "a", { deliveryDate: "2026-10-22", deliveryTime: "12-15" });
  assert.equal(selections.a.deliveryDate, "2026-10-22");
  assert.equal(selections.a.deliveryTime, "12-15");
  assert.equal(selections.b.deliveryDate, "2026-10-22");
  assert.equal(selections.b.deliveryTime, "");

  // انتخاب A نامعتبر می‌شود: فقط A پاک می‌شود و B دست‌نخورده می‌ماند.
  const narrowed = { ...schedules, a: { ...schedules.a, dates: [schedules.a.dates[0]], timeWindows: [] } };
  const afterInvalid = mergeGroupSelections({ previous: selections, schedules: narrowed });
  assert.equal(afterInvalid.a.deliveryDate, "2026-10-21");
  assert.equal(afterInvalid.b.deliveryDate, "2026-10-22");
  // گروه ناپدیدشده حذف می‌شود و انتخابش به گروه دیگر منتقل نمی‌شود.
  const withoutB = mergeGroupSelections({ previous: selections, schedules: { a: schedules.a } });
  assert.deepEqual(Object.keys(withoutB), ["a"]);
});

test("بازیابی: انتخاب ذخیره‌شدهٔ همان گروه برمی‌گردد و مقدار نامعتبر همان گروه پاک می‌شود", () => {
  const projection = projectionFrom({
    sellers: [
      {
        sellerPartyId: SELLER_A,
        methodCode: "tipax:standard",
        minimum: "2026-10-21",
        dates: ["2026-10-21", "2026-10-22"],
        selectedDate: "2026-10-22",
      },
    ],
  });
  const schedules = resolveGroupSchedules({
    isGroupedCart: true,
    projection,
    groups: [{ key: "a", sellerPartyId: SELLER_A, methodCode: "tipax:standard" }],
  });

  const restored = mergeGroupSelections({ previous: {}, schedules });
  assert.equal(restored.a.deliveryDate, "2026-10-22");
  assert.equal(restored.a.deliveryTime, "09-12");
  assert.equal(isGroupScheduleComplete(schedules.a, restored.a.deliveryDate, restored.a.deliveryTime), true);
  assert.equal(pickGroupDeliveryDate("2026-10-20", schedules.a), "2026-10-21");
  assert.equal(pickGroupDeliveryWindow("18-21", schedules.a), "");
});

test("نگاشت قرارداد سرور: shippingGroupHorizons با فیلدهای camelCase و PascalCase", () => {
  const camel = mapShippingProjection({
    cartId: "c",
    shippingGroupHorizons: [
      {
        groupKey: "k1",
        shippingGroupId: "11111111-1111-7111-8111-111111111111",
        sellerPartyId: SELLER_A,
        methodCode: "tipax:standard",
        minimumDeliveryDate: "2026-10-21",
        eligibleDates: [{ value: "2026-10-21", label: "چهارشنبه", subLabel: "۲۹ مهر", isEarliest: true }],
        eligibleTimeWindows: [{ value: "09-12", label: "۹ تا ۱۲" }],
        selectedDeliveryDate: "2026-10-21",
        selectedDeliveryTimeWindow: "09-12",
        leadDays: 3,
        sellerPreparationDays: 2,
      },
    ],
  });
  assert.equal(camel.shippingGroupHorizons.length, 1);
  assert.equal(camel.shippingGroupHorizons[0].groupKey, "k1");
  assert.equal(camel.shippingGroupHorizons[0].shippingGroupId, "11111111-1111-7111-8111-111111111111");
  assert.equal(camel.shippingGroupHorizons[0].eligibleDates[0].isEarliest, true);
  assert.equal(camel.shippingGroupHorizons[0].eligibleTimeWindows[0].value, "09-12");
  assert.equal(camel.shippingGroupHorizons[0].sellerPreparationDays, 2);

  const pascal = mapShippingProjection({
    CartId: "c",
    ShippingGroupHorizons: [
      {
        GroupKey: "k2",
        ShippingGroupId: null,
        SellerPartyId: SELLER_B,
        MethodCode: "post:express",
        MinimumDeliveryDate: "2026-10-20",
        EligibleDates: [],
        EligibleTimeWindows: [],
        SelectedDeliveryDate: null,
        SelectedDeliveryTimeWindow: null,
        LeadDays: 2,
        SellerPreparationDays: 1,
      },
    ],
  });
  assert.equal(pascal.shippingGroupHorizons[0].groupKey, "k2");
  assert.equal(pascal.shippingGroupHorizons[0].shippingGroupId, null);
  // ردیف بدون کلید پایدار/روش نادیده گرفته می‌شود (بدون حدس).
  assert.equal(mapShippingProjection({ shippingGroupHorizons: [{ GroupKey: "", MethodCode: "" }] }).shippingGroupHorizons.length, 0);
});

test("تطبیق ردیف سرور با گروه: بی‌حساسیت به خط تیره/بزرگی و کوچکی", () => {
  const rows = [
    {
      groupKey: "k",
      shippingGroupId: null,
      sellerPartyId: SELLER_A.toUpperCase().replace(/-/g, ""),
      methodCode: "TIPAX:Standard",
      minimumDeliveryDate: "2026-10-21",
      eligibleDates: [],
      eligibleTimeWindows: [],
      selectedDeliveryDate: null,
      selectedDeliveryTimeWindow: null,
      leadDays: 3,
      sellerPreparationDays: 2,
    },
  ];
  assert.ok(findGroupHorizon(rows, { sellerPartyId: SELLER_A, methodCode: "tipax:standard" }));
  assert.equal(findGroupHorizon(rows, { sellerPartyId: SELLER_B, methodCode: "tipax:standard" }), null);
  assert.equal(findGroupHorizon(rows, { sellerPartyId: SELLER_A, methodCode: "post:express" }), null);
});

test("تک‌گروهی: افق دقیقاً همان projection است و رفتار قدیمی تغییر نمی‌کند", () => {
  const single = mapShippingProjection({
    cartId: "c",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [{ value: "2026-10-19", label: "x", subLabel: "y", isEarliest: true }],
    deliveryTimeWindows: [{ value: "09-12", label: "۹" }],
  });
  const horizon = resolveSingleGroupHorizon(single);
  assert.equal(horizon.dates, single.deliveryDates);
  assert.equal(horizon.timeWindows, single.deliveryTimeWindows);
  assert.equal(horizon.minimumDeliveryDate, "2026-10-19");
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-19", "09-12"), true);
  assert.equal(pickEligibleDeliveryDate("2026-10-18", horizon), "2026-10-19");
  assert.equal(pickEligibleDeliveryWindow("18-21", horizon), "");
  assert.deepEqual(resolveGroupSchedules({ isGroupedCart: false, projection: single, groups: [] }), {});
});

test("سیم‌کشی سطح ارسال: کارت مستقل هر گروه، شرط ادامه و payload هر گروه", () => {
  // کارت‌ها به‌تفکیک گروه واقعی و با کلید پایدار؛ نه یک انتخاب‌کنندهٔ تاریخ/بازهٔ سراسری.
  assert.match(shippingSource, /resolveGroupSchedules\(\{ isGroupedCart, projection, groups: groupSelectionsByMethod \}\)/);
  assert.match(shippingSource, /<StorefrontShippingGroupCards/);
  assert.match(shippingSource, /allGroupSchedulesComplete\(\{ schedules: groupSchedules, selections: groupSelections \}\)/);
  // مسیر تک‌گروهی فقط وقتی سبد تک‌گروهی است رندر می‌شود (هیچ کارت تحویل سراسری در سبد چندگروهی).
  assert.match(singleGroupSource, /<StorefrontShippingDeliveryCard/);
  assert.match(deliveryCardSource, /data-testid="shipping-delivery"/);
  assert.match(shippingSource, /\{isGroupedCart \? null : \(\s*<StorefrontShippingSingleGroupSection/);
  // payload: زمان‌بندی هر گروه جداگانه فرستاده می‌شود؛ هیچ تاریخ سراسری روی گروه‌ها کپی نمی‌شود.
  assert.match(shippingSource, /selectedDeliveryDate: groupSelections\[group\.key\]\?\.deliveryDate \?\? ""/);
  assert.match(shippingSource, /selectedDeliveryTimeWindow: groupSelections\[group\.key\]\?\.deliveryTime \?\? ""/);
  // انتخاب «هر خط» و بازیابی قطعی حفظ شده است.
  assert.match(shippingSource, /restoreLineMethodSelections\(\{/);
  assert.match(shippingSource, /setGroupSelection\(prev, groupKey, \{ deliveryDate: value \}\)/);
  assert.doesNotMatch(shippingSource, /shipping\/commit/);
  assert.doesNotMatch(shippingSource, /deriveGroupDateHorizon|distinctSelectedMethodCodes|useGroupShippingDateHorizon/);

  // کارت هر گروه: تاریخ/بازه و «اولین نوبت ممکن» درون همان کارت.
  assert.match(cardsSource, /data-testid=\{`shipping-group-\$\{group\.identity\}`\}/);
  assert.match(cardsSource, /shipping-group-date-\$\{identity\}-\$\{d\.value\}/);
  assert.match(cardsSource, /shipping-group-window-\$\{identity\}-\$\{t\.value\}/);
  assert.match(cardsSource, /اولین نوبت ممکن/);
  assert.doesNotMatch(cardsSource, /fetch\(/);

  // ماژول‌های خالص: بدون fetch و بدون ساخت تاریخ در کلاینت.
  assert.doesNotMatch(scheduleSource, /fetch\(/);
  assert.doesNotMatch(scheduleSource, /new Date\(/);
  assert.doesNotMatch(horizonSource, /fetch\(/);
  assert.doesNotMatch(horizonSource, /new Date\(/);
});
