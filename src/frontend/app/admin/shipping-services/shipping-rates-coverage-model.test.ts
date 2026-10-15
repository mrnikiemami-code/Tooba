/**
 * TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — focused guards for the rates & coverage section model:
 * money/lead-time validation, free-above-threshold empty semantics, scope labelling and the explicit
 * "NO COVERAGE" state that must never read as unlimited coverage.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  buildScopeWrite,
  clearDependents,
  coverageStateFa,
  formatFreeAboveThreshold,
  groupRatesByOption,
  rateFormFromPolicy,
  rateFormToPayload,
  rateOptionKey,
  rateOptionLabelFa,
  scopeLevelOf,
  scopeSummary,
  summarizeRatesCoverage,
  validateDestinationSelection,
  validateRateOption,
  validateShippingRateForm,
  EMPTY_DESTINATION_SELECTION,
} from "./shipping-rates-coverage-model.ts";
import type { ShippingRatesAndCoverageView } from "./shipping-rates-coverage-api.ts";

const validForm = {
  chargeAmount: "25000",
  currencyCode: "IRR",
  preparationDays: "2",
  leadDays: "3",
  freeAboveThreshold: "500000",
};

test("valid form produces a numeric payload with normalised currency", () => {
  assert.deepEqual(validateShippingRateForm(validForm), []);
  assert.deepEqual(rateFormToPayload(validForm), {
    chargeAmount: 25000,
    currencyCode: "IRR",
    preparationDays: 2,
    leadDays: 3,
    freeAboveThreshold: 500000,
  });
});

test("empty free-above threshold stays null instead of becoming zero", () => {
  const payload = rateFormToPayload({ ...validForm, freeAboveThreshold: "  " });
  assert.equal(payload.freeAboveThreshold, null);
  assert.equal(formatFreeAboveThreshold(null, "IRR"), "—");
});

test("negative, non-numeric and malformed entries are rejected before any API call", () => {
  assert.ok(validateShippingRateForm({ ...validForm, chargeAmount: "-1" }).length > 0);
  assert.ok(validateShippingRateForm({ ...validForm, chargeAmount: "abc" }).length > 0);
  assert.ok(validateShippingRateForm({ ...validForm, currencyCode: "IR" }).length > 0);
  assert.ok(validateShippingRateForm({ ...validForm, preparationDays: "2.5" }).length > 0);
  assert.ok(validateShippingRateForm({ ...validForm, leadDays: "-3" }).length > 0);
  assert.ok(validateShippingRateForm({ ...validForm, freeAboveThreshold: "-5" }).length > 0);
});

test("preparation and lead days stay separate values", () => {
  const payload = rateFormToPayload({ ...validForm, preparationDays: "1", leadDays: "7" });
  assert.equal(payload.preparationDays, 1);
  assert.equal(payload.leadDays, 7);
});

test("policy round-trips into the edit form without inventing values", () => {
  const form = rateFormFromPolicy({
    ratePolicyId: "r1",
    serviceCode: "tooba-post",
    optionCode: null,
    countryId: "11111111-1111-1111-1111-111111111111",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
    level: "country",
    chargeAmount: 31000,
    currencyCode: "IRR",
    preparationDays: 2,
    leadDays: 4,
    freeAboveThreshold: null,
    isActive: true,
    condition: null,
  });
  assert.deepEqual(form, {
    chargeAmount: "31000",
    currencyCode: "IRR",
    preparationDays: "2",
    leadDays: "4",
    freeAboveThreshold: "",
  });
});

test("an empty coverage set is reported as NO COVERAGE, never unlimited", () => {
  const view: ShippingRatesAndCoverageView = {
    serviceCode: "tooba-post",
    ratePolicies: [],
    coverageGrants: [],
  };
  assert.equal(summarizeRatesCoverage(view).hasCoverage, false);
  assert.match(coverageStateFa(view), /بدون پوشش/);
  assert.doesNotMatch(coverageStateFa(view), /نامحدود|unlimited/i);
});

test("an inactive grant is not coverage", () => {
  const view: ShippingRatesAndCoverageView = {
    serviceCode: "tooba-post",
    ratePolicies: [],
    coverageGrants: [{
      coverageGrantId: "g1",
      serviceCode: "tooba-post",
      countryId: "11111111-1111-1111-1111-111111111111",
      administrativeAreaId: null,
      cityId: null,
      serviceAreaId: null,
      level: "country",
      isActive: false,
    }],
  };
  const summary = summarizeRatesCoverage(view);
  assert.equal(summary.grantCount, 1);
  assert.equal(summary.activeGrantCount, 0);
  assert.equal(summary.hasCoverage, false);
});

test("scope summary exposes stable ids, not fabricated placeholders", () => {
  const summary = scopeSummary({
    level: "city",
    countryId: "11111111-1111-1111-1111-111111111111",
    administrativeAreaId: "22222222-2222-2222-2222-222222222222",
    cityId: "33333333-3333-3333-3333-333333333333",
    serviceAreaId: null,
  });
  assert.match(summary, /شهر/);
  assert.match(summary, /11111111/);
  assert.match(summary, /33333333/);
});

test("scope level follows the deepest real selection", () => {
  const base = { countryId: "c1", administrativeAreaId: null, cityId: null, serviceAreaId: null };
  assert.equal(scopeLevelOf(base), "country");
  assert.equal(scopeLevelOf({ ...base, administrativeAreaId: "a1" }), "administrativeArea");
  assert.equal(scopeLevelOf({ ...base, administrativeAreaId: "a1", cityId: "ci1" }), "city");
  assert.equal(scopeLevelOf({ ...base, administrativeAreaId: "a1", cityId: "ci1", serviceAreaId: "s1" }), "serviceArea");
  assert.equal(scopeLevelOf({ ...base, countryId: "" }), null);
});

test("scope payload drops ids deeper than the chosen level instead of inferring them", () => {
  const payload = buildScopeWrite({
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: "ci1",
    serviceAreaId: null,
  });
  assert.deepEqual(payload, {
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: "ci1",
    serviceAreaId: null,
  });

  // A leftover deeper id is never used: only the selected level is written.
  const countryWide = buildScopeWrite({
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
  });
  assert.deepEqual(countryWide, {
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
  });
});

test("an empty or inconsistent destination selection is refused before submit", () => {
  assert.deepEqual(validateDestinationSelection(EMPTY_DESTINATION_SELECTION), [
    "کشور مقصد را از فهرست انتخاب کنید.",
  ]);
  // ناحیهٔ خدمت بدون شهر مالک ترکیب ناهم‌بسته است و رد می‌شود.
  assert.ok(
    validateDestinationSelection({
      countryId: "c1",
      administrativeAreaId: "a1",
      cityId: null,
      serviceAreaId: "s1",
    }).length > 0,
  );
  // دامنهٔ شهر والدِ استان خود را حمل می‌کند؛ این ترکیب معتبر است و هیچ ایرادی ندارد.
  assert.deepEqual(
    validateDestinationSelection({
      countryId: "c1",
      administrativeAreaId: "a1",
      cityId: "ci1",
      serviceAreaId: null,
    }),
    [],
  );
  assert.deepEqual(
    validateDestinationSelection({ countryId: "c1", administrativeAreaId: null, cityId: null, serviceAreaId: null }),
    [],
  );
});

test("changing a parent clears dependent selections so no inconsistent id survives", () => {
  const selected = {
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: "ci1",
    serviceAreaId: "s1",
  };
  assert.deepEqual(clearDependents(selected, "country"), {
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
  });
  assert.deepEqual(clearDependents(selected, "area"), {
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: null,
    serviceAreaId: null,
  });
  assert.deepEqual(clearDependents(selected, "city"), {
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: "ci1",
    serviceAreaId: null,
  });
});

/* TB-SHIPPING-MVP-FINAL-CLOSURE-070 - option dimension of a rate policy. */

const tipaxOptions = [
  { code: "standard", labelFa: "استاندارد" },
  { code: "express", labelFa: "پیشتاز" },
];

test("service-level policy renders as the documented fallback, never as an option", () => {
  assert.equal(rateOptionLabelFa(null, tipaxOptions), "سطح خدمت (پیش‌فرض خدمت)");
  assert.equal(rateOptionLabelFa(null, []), "سطح خدمت (پیش‌فرض خدمت)");
  assert.equal(rateOptionKey(null), "");
});

test("an option policy shows its own immutable identity and never a fabricated label", () => {
  assert.equal(rateOptionLabelFa("express", tipaxOptions), "پیشتاز");
  assert.equal(rateOptionKey("express"), "express");
  // a stored code the current tree no longer lists stays visible as the raw code
  assert.equal(rateOptionLabelFa("drone", tipaxOptions), "drone");
});

test("only options really belonging to the service are accepted", () => {
  assert.deepEqual(validateRateOption(null, tipaxOptions), []);
  assert.deepEqual(validateRateOption("standard", tipaxOptions), []);
  assert.equal(validateRateOption("drone", tipaxOptions).length, 1);
  assert.equal(validateRateOption("standard", []).length, 1);
});

test("rates group by real option order with the service-level group first", () => {
  const groups = groupRatesByOption(
    [
      { optionCode: "express", id: "e" },
      { optionCode: null, id: "s" },
      { optionCode: "standard", id: "n" },
    ],
    tipaxOptions,
  );
  assert.deepEqual(groups.map((g) => g.optionCode), [null, "standard", "express"]);
  assert.deepEqual(groups.map((g) => g.rates.map((r) => r.id)), [["s"], ["n"], ["e"]]);
});

test("an orphaned option code keeps its own group instead of being merged or dropped", () => {
  const groups = groupRatesByOption([{ optionCode: null, id: "s" }, { optionCode: "drone", id: "d" }], tipaxOptions);
  assert.deepEqual(groups.map((g) => g.optionCode), [null, "drone"]);
});

test("no cross-option leakage: two options at one scope stay two separate rows", () => {
  const groups = groupRatesByOption(
    [
      { optionCode: "standard", id: "n" },
      { optionCode: "express", id: "e" },
    ],
    tipaxOptions,
  );
  assert.equal(groups.length, 2);
  assert.equal(groups.flatMap((g) => g.rates).length, 2);
});
