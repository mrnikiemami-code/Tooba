import assert from "node:assert/strict";
import test from "node:test";

import { formatExactAmountText } from "./storefront-shipping-format.ts";

/**
 * TB-230-R1 — نمایش مبلغ capture‌شده.
 *
 * توکن خام هرگز تغییر نمی‌کند؛ این تست فقط قرارداد **نمایش** را ثابت می‌کند: ریال ایران ارز صحیح است،
 * اعشار تمام‌صفر حذف می‌شود، اعشار غیرصفر عیناً می‌ماند و ارز دیگر همان متن اعشاری خودش را نگه می‌دارد.
 */
test("IRR whole amounts drop the all-zero fraction for display only", () => {
  const raw = "120000.00";
  const display = formatExactAmountText(raw, "IRR");
  assert.equal(display, "۱۲۰,۰۰۰ ریال");
  // توکن خام دست‌نخورده است (فرمت‌کننده ورودی را بازنویسی نمی‌کند).
  assert.equal(raw, "120000.00");
});

test("IRR explicit zero stays a valid zero", () => {
  assert.equal(formatExactAmountText("0.00", "IRR"), "۰ ریال");
  assert.equal(formatExactAmountText("0", "IRR"), "۰ ریال");
});

test("IRR unexpected nonzero fractions are preserved exactly, never rounded or truncated", () => {
  assert.equal(formatExactAmountText("120000.50", "IRR"), "۱۲۰,۰۰۰.۵۰ ریال");
  assert.equal(formatExactAmountText("120000.05", "IRR"), "۱۲۰,۰۰۰.۰۵ ریال");
  assert.equal(formatExactAmountText("1234.000001", "IRR"), "۱,۲۳۴.۰۰۰۰۰۱ ریال");
});

test("other currencies keep their existing decimal text", () => {
  assert.equal(formatExactAmountText("120000.00", "USD"), "۱۲۰,۰۰۰.۰۰ USD");
  assert.equal(formatExactAmountText("120000.00", null), "۱۲۰,۰۰۰.۰۰");
  assert.equal(formatExactAmountText(null, "IRR"), null);
  // نامعتبر همان‌طور نامعتبر می‌ماند (هیچ عدد ساختگی ساخته نمی‌شود).
  assert.equal(formatExactAmountText("12,34", "IRR"), null);
});
