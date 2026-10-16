/**
 * TB-SHIPPING-ONE-LEVEL-TABS-133 — آزمون متمرکز افق تاریخ/بازهٔ تحویل سبد چندفروشنده‌ای.
 *
 * این آزمون‌ها فقط منطق خالص مشتق‌شده از پاسخ سرور و سیم‌کشی حداقلی سطح ارسال را می‌سنجند:
 * - دو روش متفاوت ⇒ اشتراک واقعی تاریخ‌های سرور (تاریخ معتبر برای هر دو روش)
 * - هیچ تاریخ/بازهٔ جعلی ساخته نمی‌شود
 * - پاسخ کهنه نمی‌تواند انتخاب جدیدتر را بازنویسی کند
 * - مسیر تک‌فروشندهٔ قدیمی دست‌نخورده می‌ماند (افق = همان projection)
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  createLatestResponseGate,
  deriveGroupDateHorizon,
  EMPTY_SHIPPING_DATE_HORIZON,
  isShippingDateHorizonComplete,
  pickEligibleDeliveryDate,
  pickEligibleDeliveryWindow,
  resolveShippingDateHorizon,
} from "./storefront-shipping-group-horizon.ts";
import type {
  StorefrontDeliveryDateOption,
  StorefrontShippingProjection,
} from "./storefront-shipping-api.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const shippingSource = fs.readFileSync(path.join(here, "storefront-shipping.tsx"), "utf8");
const hookSource = fs.readFileSync(path.join(here, "use-storefront-group-shipping-horizon.ts"), "utf8");
const modelSource = fs.readFileSync(path.join(here, "storefront-shipping-group-horizon.ts"), "utf8");

function date(value: string, isEarliest = false): StorefrontDeliveryDateOption {
  return { value, label: value, subLabel: "", isEarliest };
}

const WINDOWS = [
  { value: "09-12", label: "۹ تا ۱۲" },
  { value: "12-15", label: "۱۲ تا ۱۵" },
  { value: "15-18", label: "۱۵ تا ۱۸" },
];

/** شکل واقعی پاسخ سرور برای یک روش، مثل آنچه TB-132 مشاهده شد. */
function projection(input: {
  methodCode: string;
  minimumDeliveryDate: string;
  deliveryDates: StorefrontDeliveryDateOption[];
  deliveryTimeWindows?: { value: string; label: string }[];
}): StorefrontShippingProjection {
  return {
    cartId: "cart-1",
    cartVersion: "1",
    methods: [
      {
        methodCode: input.methodCode,
        label: input.methodCode,
        serviceCode: "srv",
        optionCode: null,
        priceAmount: 200000,
        currency: "IRR",
        estimationLabel: "۲ روز",
        isEligible: true,
        ineligibilityReason: null,
      },
    ],
    selectedMethodCode: input.methodCode,
    selectedShippingAmount: 200000,
    subtotalExclusiveOfTax: 3640000,
    currency: "IRR",
    itemCount: 2,
    minimumDeliveryDate: input.minimumDeliveryDate,
    deliveryDates: input.deliveryDates,
    deliveryTimeWindows: input.deliveryTimeWindows ?? WINDOWS,
    draft: null,
    revalidationMessage: null,
    provinces: [],
  } as unknown as StorefrontShippingProjection;
}

test("گروه چندفروشنده: تاریخ مجاز اشتراک سرور برای هر دو روش متفاوت است", () => {
  const express = projection({
    methodCode: "post:express",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: ["2026-10-19", "2026-10-20", "2026-10-21"].map((v, i) => date(v, i === 0)),
  });
  const tipax = projection({
    methodCode: "tipax:standard",
    minimumDeliveryDate: "2026-10-20",
    deliveryDates: ["2026-10-20", "2026-10-21", "2026-10-22"].map((v, i) => date(v, i === 0)),
  });

  const horizon = deriveGroupDateHorizon([express, tipax]);

  assert.equal(horizon.resolved, true);
  assert.deepEqual(
    horizon.dates.map((d) => d.value),
    ["2026-10-20", "2026-10-21"],
  );
  // تاریخ انتخاب‌شده باید برای هر دو روش معتبر باشد، نه فقط آخرین کلیک.
  for (const value of horizon.dates.map((d) => d.value)) {
    assert.ok(express.deliveryDates.some((d) => d.value === value));
    assert.ok(tipax.deliveryDates.some((d) => d.value === value));
  }
  // حداقل تاریخ = دیرترین حداقل سرور (محافظه‌کارانه).
  assert.equal(horizon.minimumDeliveryDate, "2026-10-20");
  // برچسب «نزدیک‌ترین» روی اولین تاریخ مجاز مشترک بازچینی می‌شود.
  assert.equal(horizon.dates[0].isEarliest, true);
  assert.ok(horizon.dates.slice(1).every((d) => d.isEarliest === false));
});

test("گروه چندفروشنده: بازهٔ ساعت اشتراک پاسخ‌ها است و اشتراک خالی ⇒ فهرست خالی", () => {
  const a = projection({
    methodCode: "a",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [date("2026-10-19")],
    deliveryTimeWindows: [
      { value: "09-12", label: "۹" },
      { value: "15-18", label: "۱۵" },
    ],
  });
  const b = projection({
    methodCode: "b",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [date("2026-10-19")],
    deliveryTimeWindows: [{ value: "15-18", label: "۱۵" }],
  });

  assert.deepEqual(deriveGroupDateHorizon([a, b]).timeWindows.map((w) => w.value), ["15-18"]);

  const c = projection({
    methodCode: "c",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [date("2026-10-19")],
    deliveryTimeWindows: [{ value: "18-21", label: "۱۸" }],
  });
  const empty = deriveGroupDateHorizon([a, c]);
  assert.deepEqual(empty.timeWindows, []);
  assert.equal(isShippingDateHorizonComplete(empty, "2026-10-19", "09-12"), false);
});

test("گروه چندفروشنده: تاریخ جعلی ساخته نمی‌شود و ورودی تغییر نمی‌کند", () => {
  const a = projection({
    methodCode: "a",
    minimumDeliveryDate: "2026-10-20",
    deliveryDates: [date("2026-10-20"), date("2026-10-21")],
  });
  const b = projection({
    methodCode: "b",
    minimumDeliveryDate: "2026-10-22",
    deliveryDates: [date("2026-10-22"), date("2026-10-23")],
  });

  const horizon = deriveGroupDateHorizon([a, b]);
  assert.deepEqual(horizon.dates, []);
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-20", "09-12"), false);
  // هیچ تاریخی قبل از حداقل سرور نمی‌ماند و پاسخ اصلی دست‌نخورده است.
  assert.deepEqual(a.deliveryDates.map((d) => d.value), ["2026-10-20", "2026-10-21"]);
  assert.deepEqual(b.deliveryDates.map((d) => d.value), ["2026-10-22", "2026-10-23"]);
  assert.deepEqual(deriveGroupDateHorizon([]), EMPTY_SHIPPING_DATE_HORIZON);
});

test("تک‌فروشنده: افق دقیقاً همان projection است و رفتار قدیمی تغییر نمی‌کند", () => {
  const single = projection({
    methodCode: "post:express",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [date("2026-10-19", true), date("2026-10-20")],
  });

  const horizon = resolveShippingDateHorizon({
    isGroupedCart: false,
    projection: single,
    groupHorizon: EMPTY_SHIPPING_DATE_HORIZON,
  });

  assert.equal(horizon.dates, single.deliveryDates);
  assert.equal(horizon.timeWindows, single.deliveryTimeWindows);
  assert.equal(horizon.minimumDeliveryDate, "2026-10-19");
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-19", "09-12"), true);
});

test("انتخاب قبلی فقط وقتی نامعتبر شود پاک می‌شود", () => {
  const horizon = deriveGroupDateHorizon([
    projection({
      methodCode: "a",
      minimumDeliveryDate: "2026-10-19",
      deliveryDates: [date("2026-10-19"), date("2026-10-20")],
    }),
    projection({
      methodCode: "b",
      minimumDeliveryDate: "2026-10-20",
      deliveryDates: [date("2026-10-20"), date("2026-10-21")],
    }),
  ]);

  assert.equal(pickEligibleDeliveryDate("2026-10-20", horizon), "2026-10-20");
  // ۱۰-۱۹ دیگر برای روش دوم معتبر نیست ⇒ نزدیک‌ترین تاریخ مجاز مشترک انتخاب می‌شود.
  assert.equal(pickEligibleDeliveryDate("2026-10-19", horizon), "2026-10-20");
  assert.equal(pickEligibleDeliveryWindow("09-12", horizon), "09-12");
  assert.equal(pickEligibleDeliveryWindow("18-21", horizon), "");
  // تا زمانی که پاسخ سرور نرسیده، انتخاب کاربر دست‌نخورده می‌ماند (ادامه غیرفعال است).
  assert.equal(pickEligibleDeliveryDate("2026-10-19", EMPTY_SHIPPING_DATE_HORIZON), "2026-10-19");
});

test("پاسخ کهنهٔ سرور نمی‌تواند انتخاب جدیدتر را بازنویسی کند", () => {
  const gate = createLatestResponseGate();
  const first = gate.begin();
  assert.equal(gate.isCurrent(first), true);

  const second = gate.begin();
  assert.equal(gate.isCurrent(second), true);
  assert.equal(gate.isCurrent(first), false, "پاسخ کهنه باید رد شود");

  const third = gate.begin();
  assert.equal(gate.isCurrent(second), false);
  assert.equal(gate.isCurrent(third), true);
});

test("سیم‌کشی سطح ارسال: واکشی سرور-محور، dedup روش‌ها، و تاریخ سرور در رندر", () => {
  // سطح ارسال: هیچ تاریخ/محاسبه‌ای خودش نمی‌سازد و رندر فقط از افق مشتق‌شده تغذیه می‌شود.
  assert.match(shippingSource, /distinctSelectedMethodCodes\(groupSelections, new Set\(methodByCode\.keys\(\)\)\)/);
  assert.match(shippingSource, /useGroupShippingDateHorizon\(\{/);
  assert.match(shippingSource, /methodCodes: selectedGroupMethodCodes/);
  assert.match(shippingSource, /destination: destinationIdentifiers/);
  assert.match(shippingSource, /dateHorizon\.dates\.map/);
  assert.match(shippingSource, /dateHorizon\.timeWindows\.map/);
  assert.match(shippingSource, /!isGroupedCart \|\| isShippingDateHorizonComplete\(dateHorizon, deliveryDate, deliveryTime\)/);
  // انتخاب گروهی حفظ می‌شود: هیچ‌جا روش گروه‌ها با draft بازنویسی/حدس زده نمی‌شود.
  assert.match(shippingSource, /groupMethodBySeller\[group\.sellerPartyId\] \?\? ""/);
  // ارسال سفارش/commit در این مرحله اتفاق نمی‌افتد.
  assert.doesNotMatch(shippingSource, /shipping\/commit/);

  // hook: همان مسیر مجاز موجود، یک‌بار برای هر روش متمایز، با گارد پاسخ کهنه.
  assert.match(hookSource, /loadShippingProjection\(\{/);
  assert.match(hookSource, /methodCode: code/);
  assert.match(hookSource, /Promise\.allSettled\(/);
  assert.match(hookSource, /gateRef\.current\.isCurrent\(token\)/);
  assert.match(hookSource, /deriveGroupDateHorizon\(pages\)/);
  assert.match(hookSource, /\[enabled, cartId, methodKey, input\.methodCodes, provinceName, cityName, destination\]/);

  // ماژول خالص: بدون fetch، بدون ساخت تاریخ در کلاینت.
  assert.doesNotMatch(modelSource, /fetch\(/);
  assert.doesNotMatch(modelSource, /new Date\(/);
  assert.match(modelSource, /value >= minimumDeliveryDate|minimumDeliveryDate \? value >= minimumDeliveryDate : true/);
});
