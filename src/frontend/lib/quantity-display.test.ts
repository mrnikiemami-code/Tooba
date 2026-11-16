import assert from "node:assert/strict";
import test from "node:test";
import { formatQuantityDisplay, formatQuantityDisplayFa, parseQuantityInput } from "./quantity-display.ts";

test("formatQuantityDisplay strips storage zeros", () => {
  assert.equal(formatQuantityDisplay(2, 0), "2");
  assert.equal(formatQuantityDisplay(1.25, 2), "1.25");
  assert.equal(formatQuantityDisplay(1.250000, 6), "1.25");
});

test("formatQuantityDisplayFa prints Persian digits and the Persian decimal separator", () => {
  assert.equal(formatQuantityDisplayFa(2, 0), "۲");
  assert.equal(formatQuantityDisplayFa(1.25, 2), "۱٫۲۵");
  assert.equal(formatQuantityDisplayFa(12, 6), "۱۲");
  // No latin digit or latin decimal point survives into operator copy.
  assert.doesNotMatch(formatQuantityDisplayFa(1234.5, 6), /[0-9.]/);
});

test("parseQuantityInput accepts 1.25 and rejects parseInt loss", () => {
  assert.equal(parseQuantityInput("1.25"), 1.25);
  assert.equal(parseQuantityInput("1"), 1);
  assert.equal(parseQuantityInput("1,25"), 1.25);
  assert.equal(parseQuantityInput(""), null);
  assert.equal(parseQuantityInput("abc"), null);
});
