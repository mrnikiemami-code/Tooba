import assert from "node:assert/strict";
import test from "node:test";
import { fromPersianDigits, toPersianDigits } from "./persian-digits.ts";

test("toPersianDigits converts only the digits of a technical code", () => {
  assert.equal(toPersianDigits("TB-20261114151709-01-2d8281"), "TB-۲۰۲۶۱۱۱۴۱۵۱۷۰۹-۰۱-۲d۸۲۸۱");
  assert.equal(toPersianDigits("TRK-01A1DB2C"), "TRK-۰۱A۱DB۲C");
  // Non-digits are never touched, so a code keeps its separators and shape.
  assert.equal(toPersianDigits("A-B_C/D"), "A-B_C/D");
  assert.equal(toPersianDigits(0), "۰");
  assert.equal(toPersianDigits(""), "");
  assert.equal(toPersianDigits(null), "");
});

test("fromPersianDigits folds a pasted Persian code back to the stored Latin one", () => {
  const stored = "TB-20261114151709-01-2d8281";
  assert.equal(fromPersianDigits(toPersianDigits(stored)), stored);
  assert.equal(fromPersianDigits("۰۱۲۳۴۵۶۷۸۹"), "0123456789");
  // Arabic-Indic digits are folded too; letters and separators pass through untouched.
  assert.equal(fromPersianDigits("٠١٢٩"), "0129");
  assert.equal(fromPersianDigits("TB-اکتبر"), "TB-اکتبر");
});
