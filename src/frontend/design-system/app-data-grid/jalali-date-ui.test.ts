import assert from "node:assert/strict";
import test from "node:test";
import { filterChipLabel } from "./ag-filter-mapper.ts";
import { jalaliInputToIso, formatJalaliDate } from "./jalali.ts";

test("jalaliInputToIso converts Persian date input to ISO", () => {
  const iso = jalaliInputToIso("1404/01/01");
  assert.ok(iso);
  assert.match(formatJalaliDate(iso!, "fa"), /^1404\/01\/01$/);
});

test("filterChipLabel renders Jalali date without raw ISO tokens", () => {
  const iso = jalaliInputToIso("1404/06/15")!;
  const label = filterChipLabel(
    "updatedAt",
    "به‌روزرسانی",
    { kind: "date", operator: "between", iso, isoTo: jalaliInputToIso("1404/07/01")! },
    "fa",
  );
  assert.match(label, /به‌روزرسانی:/);
  assert.match(label, /1404\/06\/15/);
  assert.match(label, /1404\/07\/01/);
  assert.doesNotMatch(label, /greaterThan/);
  assert.doesNotMatch(label, /2026-/);
});

test("filterChipLabel maps enum values to human labels", () => {
  const label = filterChipLabel(
    "status",
    "وضعیت",
    { kind: "status", operator: "in", values: ["Published"] },
    "fa",
    { enumLabels: { Published: "منتشر شده" } },
  );
  assert.equal(label, "وضعیت: منتشر شده");
});
