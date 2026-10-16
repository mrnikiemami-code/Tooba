/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — آزمون متمرکز افق تحویلِ **تک‌گروهی** (سازگاری گذشته).
 *
 * سبد چندگروهی دیگر افق مشترک/تلاقی ندارد؛ افق مستقل هر گروه در
 * `storefront-shipping-group-schedule.test.ts` سنجیده می‌شود. اینجا فقط تضمین می‌شود که مسیر
 * تک‌گروهی عیناً رفتار قبلی را نگه می‌دارد و هیچ تاریخ/بازهٔ ساختگی در کلاینت ساخته نمی‌شود.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import {
  EMPTY_SHIPPING_DATE_HORIZON,
  isShippingDateHorizonComplete,
  pickEligibleDeliveryDate,
  pickEligibleDeliveryWindow,
  resolveSingleGroupHorizon,
} from "./storefront-shipping-group-horizon.ts";
import { mapShippingProjection } from "./storefront-shipping-api.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const modelSource = fs.readFileSync(path.join(here, "storefront-shipping-group-horizon.ts"), "utf8");

test("بدون projection: افق خالی و غیر-resolved است (بدون تاریخ جعلی)", () => {
  assert.deepEqual(resolveSingleGroupHorizon(null), EMPTY_SHIPPING_DATE_HORIZON);
  assert.equal(isShippingDateHorizonComplete(EMPTY_SHIPPING_DATE_HORIZON, "2026-10-19", "09-12"), false);
  // تا زمانی که پاسخ سرور نرسیده، انتخاب کاربر دست‌نخورده می‌ماند (ادامه غیرفعال است).
  assert.equal(pickEligibleDeliveryDate("2026-10-19", EMPTY_SHIPPING_DATE_HORIZON), "2026-10-19");
});

test("تک‌گروهی: افق دقیقاً فیلدهای همان projection سرور است", () => {
  const projection = mapShippingProjection({
    cartId: "c",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [
      { value: "2026-10-19", label: "چهارشنبه", subLabel: "۲۸ مهر", isEarliest: true },
      { value: "2026-10-20", label: "پنجشنبه", subLabel: "۲۹ مهر", isEarliest: false },
    ],
    deliveryTimeWindows: [
      { value: "09-12", label: "۹ تا ۱۲" },
      { value: "15-18", label: "۱۵ تا ۱۸" },
    ],
  });

  const horizon = resolveSingleGroupHorizon(projection);
  assert.equal(horizon.dates, projection.deliveryDates);
  assert.equal(horizon.timeWindows, projection.deliveryTimeWindows);
  assert.equal(horizon.minimumDeliveryDate, "2026-10-19");
  assert.equal(horizon.resolved, true);
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-19", "09-12"), true);
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-18", "09-12"), false);
  assert.equal(isShippingDateHorizonComplete(horizon, "2026-10-19", "18-21"), false);
});

test("انتخاب قبلی فقط وقتی نامعتبر شود بازچینی می‌شود", () => {
  const projection = mapShippingProjection({
    cartId: "c",
    minimumDeliveryDate: "2026-10-19",
    deliveryDates: [{ value: "2026-10-19", label: "چهارشنبه", subLabel: "۲۸ مهر", isEarliest: true }],
    deliveryTimeWindows: [{ value: "09-12", label: "۹ تا ۱۲" }],
  });
  const horizon = resolveSingleGroupHorizon(projection);

  assert.equal(pickEligibleDeliveryDate("2026-10-19", horizon), "2026-10-19");
  // تاریخ/بازهٔ نامعتبر فقط نزدیک‌ترین مقدار مجاز **همان سرور** می‌شود، نه یک مقدار ساخته‌شده.
  assert.equal(pickEligibleDeliveryDate("2026-10-20", horizon), "2026-10-19");
  assert.equal(pickEligibleDeliveryWindow("15-18", horizon), "");
  assert.equal(pickEligibleDeliveryWindow("09-12", horizon), "09-12");
});

test("ماژول تک‌گروهی: بدون fetch و بدون ساخت تاریخ در کلاینت", () => {
  assert.doesNotMatch(modelSource, /fetch\(/);
  assert.doesNotMatch(modelSource, /new Date\(/);
  // هیچ منطق تلاقی/دیرترین-حداقل سراسری باقی نمانده است.
  assert.doesNotMatch(modelSource, /deriveGroupDateHorizon|resolveShippingDateHorizon|distinctSelectedMethodCodes/);
});
