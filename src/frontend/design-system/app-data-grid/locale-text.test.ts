import assert from "node:assert/strict";
import test from "node:test";
import { buildAgGridLocaleText, faAgFilterOperatorLabels } from "./locale-text.ts";

test("fa AG Grid locale maps number operators to Persian without English leftovers", () => {
  const locale = buildAgGridLocaleText("fa");
  assert.equal(locale.equals, "برابر");
  assert.equal(locale.notEqual, "نابرابر");
  assert.equal(locale.lessThan, "کمتر از");
  assert.equal(locale.lessThanOrEqual, "کمتر یا مساوی");
  assert.equal(locale.greaterThan, "بیشتر از");
  assert.equal(locale.greaterThanOrEqual, "بیشتر یا مساوی");
  assert.equal(locale.inRange, "بین");
  assert.equal(locale.blank, "خالی");
  assert.equal(locale.notBlank, "غیرخالی");
  assert.equal(locale.contains, "شامل");
  assert.equal(locale.notContains, "شامل نمی‌شود");
  assert.equal(locale.startsWith, "شروع با");
  assert.equal(locale.endsWith, "پایان با");
  assert.equal(locale.filterOoo, "مقدار فیلتر");
  assert.equal(locale.applyFilter, "اعمال");
  assert.equal(locale.andCondition, "و");
  assert.equal(locale.orCondition, "یا");
});

test("fa locale exposes no raw English operator labels", () => {
  const labels = faAgFilterOperatorLabels();
  const english = [
    "Greater than or equal to",
    "Less than or equal to",
    "Between",
    "Blank",
    "Not blank",
    "Greater than",
    "Less than",
    "Equals",
    "Not equal",
  ];
  for (const value of Object.values(labels)) {
    for (const bad of english) {
      assert.notEqual(value, bad, `unexpected English label: ${bad}`);
    }
  }
});
