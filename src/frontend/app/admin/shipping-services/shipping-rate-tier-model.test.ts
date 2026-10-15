/**
 * TB-SHIPPING-FULL-OPERATIONS-074 — focused guards for the «شرایط مرسوله» form model: exact
 * kilogram↔gram conversion (no floating point drift at 1999/2000), half-open validation, lossless
 * round-trip of a persisted condition, and unconditioned-by-default behaviour.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  conditionFormFromPolicy,
  conditionFormToWrite,
  conditionSummaryFa,
  describeConditionFa,
  EMPTY_CONDITION_FORM,
  formatGramsAsKilograms,
  isConditioned,
  parseItemCount,
  parseKilogramsToGrams,
  tierErrorFa,
  UNCONDITIONED_FA,
  validateConditionForm,
  type ShippingRateConditionForm,
} from "./shipping-rate-tier-model.ts";

/* ---------------------------------------------------------------------------------------------
 * Exact conversion: the 1999/2000 boundary must survive the human↔wire round trip untouched.
 * ------------------------------------------------------------------------------------------- */

test("kilograms convert to exact integer grams without floating point drift", () => {
  assert.equal(parseKilogramsToGrams("2"), 2000);
  assert.equal(parseKilogramsToGrams("1.999"), 1999);
  assert.equal(parseKilogramsToGrams("2.001"), 2001);
  assert.equal(parseKilogramsToGrams("0"), 0);
  assert.equal(parseKilogramsToGrams("0.001"), 1);
  assert.equal(parseKilogramsToGrams("1000"), 1_000_000);
  // The classic float trap: 0.1 + 0.2 style arithmetic never enters this path.
  assert.equal(parseKilogramsToGrams("1.1"), 1100);
  assert.equal(parseKilogramsToGrams("2.3"), 2300);
});

test("finer-than-gram and malformed weight input is refused rather than rounded", () => {
  assert.equal(parseKilogramsToGrams("1.0005"), null);
  assert.equal(parseKilogramsToGrams("-1"), null);
  assert.equal(parseKilogramsToGrams("abc"), null);
  assert.equal(parseKilogramsToGrams(""), null);
  assert.equal(parseKilogramsToGrams("1,5"), null);
  assert.equal(parseKilogramsToGrams("1."), null);
});

test("grams render back as trimmed kilogram text, exactly", () => {
  assert.equal(formatGramsAsKilograms(2000), "2");
  assert.equal(formatGramsAsKilograms(1999), "1.999");
  assert.equal(formatGramsAsKilograms(2001), "2.001");
  assert.equal(formatGramsAsKilograms(0), "0");
  assert.equal(formatGramsAsKilograms(1), "0.001");
  assert.equal(formatGramsAsKilograms(1_500_000), "1500");
});

test("item count is a plain non-negative integer in items", () => {
  assert.equal(parseItemCount("5"), 5);
  assert.equal(parseItemCount("0"), 0);
  assert.equal(parseItemCount("1.5"), null);
  assert.equal(parseItemCount("-1"), null);
  assert.equal(parseItemCount(""), null);
});

/* ---------------------------------------------------------------------------------------------
 * Default and lossless round trip: saving an existing unconditioned rate must not restrict it.
 * ------------------------------------------------------------------------------------------- */

test("an absent or empty condition maps to the untouched form and writes back as null", () => {
  assert.deepEqual(conditionFormFromPolicy(null), EMPTY_CONDITION_FORM);
  assert.deepEqual(conditionFormFromPolicy(undefined), EMPTY_CONDITION_FORM);
  assert.deepEqual(conditionFormFromPolicy({}), EMPTY_CONDITION_FORM);
  assert.equal(conditionFormToWrite(EMPTY_CONDITION_FORM), null);
  assert.equal(isConditioned(null), false);
});

test("a persisted weight tier round-trips losslessly through the form", () => {
  const stored = { weightGrams: { min: 0, max: 2000 } };
  const form = conditionFormFromPolicy(stored);
  assert.equal(form.weight.enabled, true);
  assert.equal(form.weight.min, "0");
  assert.equal(form.weight.max, "2");
  assert.equal(form.count.enabled, false);
  assert.deepEqual(conditionFormToWrite(form), { weightGrams: { min: 0, max: 2000 }, itemCount: null });
});

test("the adjacent 2000-gram tier round-trips to exactly 2000, not 1999 or 2001", () => {
  const form = conditionFormFromPolicy({ weightGrams: { min: 2000, max: 5000 } });
  assert.deepEqual(conditionFormToWrite(form), { weightGrams: { min: 2000, max: 5000 }, itemCount: null });
});

test("an open upper bound survives as null instead of collapsing to zero", () => {
  const form = conditionFormFromPolicy({ weightGrams: { min: 5000, max: null } });
  assert.equal(form.weight.max, "");
  assert.deepEqual(conditionFormToWrite(form), { weightGrams: { min: 5000, max: null }, itemCount: null });
});

test("a two-axis condition round-trips as an intersection, both axes preserved", () => {
  const form = conditionFormFromPolicy({
    weightGrams: { min: 2000, max: 5000 },
    itemCount: { min: 6, max: 11 },
  });
  assert.deepEqual(conditionFormToWrite(form), {
    weightGrams: { min: 2000, max: 5000 },
    itemCount: { min: 6, max: 11 },
  });
});

test("disabling an axis drops it entirely rather than writing a zero-width tier", () => {
  const form: ShippingRateConditionForm = {
    weight: { enabled: false, min: "0", max: "2" },
    count: { enabled: true, min: "1", max: "6" },
  };
  assert.deepEqual(conditionFormToWrite(form), { weightGrams: null, itemCount: { min: 1, max: 6 } });
});

/* ---------------------------------------------------------------------------------------------
 * Validation: half-open boundaries, reversed/zero-width ranges and ceilings.
 * ------------------------------------------------------------------------------------------- */

test("a well-formed half-open range passes with no issues", () => {
  assert.deepEqual(
    validateConditionForm({
      weight: { enabled: true, min: "0", max: "2" },
      count: { enabled: false, min: "", max: "" },
    }),
    [],
  );
  assert.deepEqual(
    validateConditionForm({
      weight: { enabled: true, min: "2", max: "" },
      count: { enabled: true, min: "1", max: "6" },
    }),
    [],
  );
});

test("a zero-width or reversed range is refused, matching tier_range_invalid", () => {
  assert.ok(validateConditionForm({ weight: { enabled: true, min: "2", max: "2" }, count: { enabled: false, min: "", max: "" } }).length > 0);
  assert.ok(validateConditionForm({ weight: { enabled: true, min: "3", max: "2" }, count: { enabled: false, min: "", max: "" } }).length > 0);
  assert.ok(validateConditionForm({ weight: { enabled: false, min: "", max: "" }, count: { enabled: true, min: "5", max: "5" } }).length > 0);
});

test("a missing or malformed lower bound is refused", () => {
  assert.ok(validateConditionForm({ weight: { enabled: true, min: "", max: "2" }, count: { enabled: false, min: "", max: "" } }).length > 0);
  assert.ok(validateConditionForm({ weight: { enabled: true, min: "-1", max: "2" }, count: { enabled: false, min: "", max: "" } }).length > 0);
  assert.ok(validateConditionForm({ weight: { enabled: false, min: "", max: "" }, count: { enabled: true, min: "1.5", max: "6" } }).length > 0);
});

test("bounds above the backend ceilings are refused before any API call", () => {
  assert.ok(validateConditionForm({ weight: { enabled: true, min: "1000001", max: "" }, count: { enabled: false, min: "", max: "" } }).length > 0);
  assert.ok(validateConditionForm({ weight: { enabled: false, min: "", max: "" }, count: { enabled: true, min: "0", max: "1000001" } }).length > 0);
  assert.deepEqual(validateConditionForm({ weight: { enabled: true, min: "1000000", max: "" }, count: { enabled: false, min: "", max: "" } }), []);
});

/* ---------------------------------------------------------------------------------------------
 * Presentation: half-open wording, intersection wording, and adjacent error guidance.
 * ------------------------------------------------------------------------------------------- */

test("the weight description states half-open boundaries in both grams and kilograms", () => {
  const text = describeConditionFa({ weightGrams: { min: 0, max: 2000 } });
  assert.match(text, /شامل/);
  assert.match(text, /غیرشامل/);
  assert.match(text, /۲٬۰۰۰/);
  assert.match(text, /2 کیلوگرم/);
  // The 1999 boundary itself renders exactly, not rounded to 2 kg.
  const boundary = describeConditionFa({ weightGrams: { min: 1999, max: null } });
  assert.match(boundary, /۱٬۹۹۹/);
  assert.match(boundary, /1\.999 کیلوگرم/);
});

test("an open upper bound is described as having no upper limit", () => {
  assert.match(describeConditionFa({ weightGrams: { min: 5000, max: null } }), /بدون کران بالا/);
});

test("two axes are described as an intersection, never as either axis alone", () => {
  const text = describeConditionFa({ weightGrams: { min: 0, max: 2000 }, itemCount: { min: 1, max: 6 } });
  assert.match(text, /وزن/);
  assert.match(text, /تعداد اقلام/);
  assert.match(text, /همزمان/);
});

test("an unconditioned rate says so explicitly and is never rendered as a zero interval", () => {
  assert.equal(describeConditionFa(null), UNCONDITIONED_FA);
  assert.doesNotMatch(describeConditionFa(null), /\[0/);
  assert.equal(conditionSummaryFa(null), UNCONDITIONED_FA);
});

test("the compact summary is an intersection marker, not a union", () => {
  const text = conditionSummaryFa({ weightGrams: { min: 2000, max: 5000 }, itemCount: { min: 6, max: 11 } });
  assert.match(text, /∩/);
  assert.match(text, /\[2، 5\)kg/);
  assert.match(text, /\[6، 11\)/);
});

test("stable tier error codes map to adjacent Persian guidance", () => {
  assert.match(tierErrorFa("shipping.rate_policy.tier_overlap") ?? "", /همپوشانی/);
  assert.match(tierErrorFa("shipping.rate_policy.scope_duplicate") ?? "", /از قبل/);
  assert.match(tierErrorFa("shipping.validation.tier_range_invalid") ?? "", /بزرگتر/);
  assert.match(tierErrorFa("shipping.validation.tier_min_negative") ?? "", /منفی/);
  assert.match(tierErrorFa("shipping.validation.tier_out_of_range") ?? "", /سقف/);
  assert.equal(tierErrorFa("shipping.something.else"), null);
  assert.equal(tierErrorFa(undefined), null);
});
