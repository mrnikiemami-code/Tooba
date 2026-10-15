/**
 * TB-SHIPPING-FULL-OPERATIONS-078 — pure-model guards for the diagnostic quote inspector: exact
 * kilogram → gram conversion, explicit IANA timezone resolution (including DST ambiguity), nullable
 * money rendering, and the honesty wording that must never be softened.
 *
 * No server and no React are involved, so these rules are testable in isolation.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  availabilityLabelFa,
  CAPACITY_CONFIG_ONLY_QUOTE_FA,
  COLLECT_ON_DELIVERY_EXPLANATION_FA,
  conditionMatchFa,
  currencyMatchesRequest,
  emptyQuoteForm,
  formatAmountFa,
  isQuotedResult,
  NOT_DECLARED_FA,
  NOT_EVALUATED_FA,
  outcomeLabelFa,
  parseMoneyAmount,
  parseOptionalItemCount,
  parseOptionalWeightGrams,
  payerModeLabelFa,
  PICKUP_WINDOW_IS_NOT_PROMISE_FA,
  PREPAID_EXPLANATION_FA,
  quoteFormToPayload,
  quoteReasonFa,
  rawAmount,
  resolvePickupToUtc,
  scheduleSummaryFa,
  validateQuoteForm,
} from "./shipping-operational-quote-model.ts";
import { mapOperationalQuote } from "./shipping-operational-quote-api.ts";

const COUNTRY = "e3451206-e503-51a8-91ad-afa9ab70e330";
const AREA = "1f8050a8-c325-59fa-959f-82d76b5b2bc5";

test("schedule availability wording never upgrades NotEvaluated to available", () => {
  assert.equal(availabilityLabelFa("NotEvaluated").includes("ارزیابی‌نشده"), true);
  assert.equal(availabilityLabelFa("NotEvaluated").includes("داخل بازه"), false);
  assert.equal(availabilityLabelFa("InactiveNotEnforced").includes("غیرفعال"), true);
  assert.equal(availabilityLabelFa("Unavailable").includes("خارج"), true);
  assert.equal(availabilityLabelFa("Ambiguous").includes("مبهم"), true);

  const summary = scheduleSummaryFa({
    availability: "Available",
    reasonCode: "shipping.schedule.available",
    timeZoneId: "Asia/Tehran",
    localWeekday: 7,
    localTime: "13:30:00",
    matchedWindowId: "w1",
    configuredCapacityTotal: 25,
    capacityEnforcement: "NotEnforced",
    isConstraintSatisfied: true,
  });
  assert.equal(summary.includes("یکشنبه"), true);
  assert.equal(summary.includes("13:30:00"), true);
  assert.equal(summary.includes("۲۵"), true);
  assert.equal(scheduleSummaryFa(null), NOT_DECLARED_FA);
});

test("capacity, promise and payer explanations are explicit non-negotiable contracts", () => {
  assert.equal(CAPACITY_CONFIG_ONLY_QUOTE_FA.includes("فقط تنظیم شده"), true);
  assert.equal(CAPACITY_CONFIG_ONLY_QUOTE_FA.includes("رزرو/اجرای ظرفیت وجود ندارد"), true);
  assert.equal(PICKUP_WINDOW_IS_NOT_PROMISE_FA.includes("نه زمان تحویل وعده‌داده‌شده"), true);
  assert.equal(NOT_EVALUATED_FA.includes("قابل رزرو"), true);
  assert.equal(PREPAID_EXPLANATION_FA.includes("پیش‌کرایه"), true);
  assert.equal(COLLECT_ON_DELIVERY_EXPLANATION_FA.includes("متصل نشده است"), true);
  assert.equal(payerModeLabelFa("Prepaid"), "پیش‌کرایه");
  assert.equal(payerModeLabelFa("CollectOnDelivery"), "پس‌کرایه");
  assert.equal(outcomeLabelFa("CurrencyMismatch").includes("ارز"), true);
  assert.equal(quoteReasonFa("shipping.schedule.not_configured").includes("ثبت نشده"), true);
  assert.equal(quoteReasonFa("unknown.code"), "unknown.code");
});

test("exact kilogram → gram conversion and optional inputs never round silently", () => {
  assert.deepEqual(parseOptionalWeightGrams(""), { value: null, invalid: false });
  assert.deepEqual(parseOptionalWeightGrams("1.999"), { value: 1999, invalid: false });
  assert.deepEqual(parseOptionalWeightGrams("2"), { value: 2000, invalid: false });
  assert.equal(parseOptionalWeightGrams("1.0005").invalid, true, "دقت بیشتر از گرم باید رد شود، نه گرد");
  assert.equal(parseOptionalWeightGrams("-1").invalid, true);
  assert.equal(parseOptionalWeightGrams("۱").invalid, true, "رقم فارسی روی سیم پذیرفته نمی‌شود");

  assert.deepEqual(parseOptionalItemCount(""), { value: null, invalid: false });
  assert.deepEqual(parseOptionalItemCount("2"), { value: 2, invalid: false });
  assert.equal(parseOptionalItemCount("0").invalid, true, "صفر یک مرسولهٔ خالی است، نه یک اندازه");

  assert.equal(parseMoneyAmount("500000"), 500000);
  assert.equal(parseMoneyAmount("500000.50"), 500000.5);
  assert.equal(parseMoneyAmount("1.005"), null, "گرد کردن خاموش ممنوع است");
  assert.equal(parseMoneyAmount(""), null);
});

test("timezone resolution is explicit: offset, ambiguity and nonexistent local times are handled", () => {
  const resolved = resolvePickupToUtc("2026-10-18T13:30", "Asia/Tehran");
  assert.equal(resolved.kind, "resolved");
  if (resolved.kind === "resolved") {
    // Iran has had no DST since 2022: 13:30 local is exactly 10:00 UTC, offset UTC+03:30.
    assert.equal(resolved.utcIso, "2026-10-18T10:00:00.000Z");
    assert.equal(resolved.offsetLabel, "UTC+03:30");
  }

  // The same wall time in a zone with a DST fall-back occurs twice and is refused, not guessed.
  assert.equal(resolvePickupToUtc("2026-11-01T01:30", "America/New_York").kind, "ambiguous");

  // A wall time skipped by a DST spring-forward does not exist and is refused.
  assert.equal(resolvePickupToUtc("2026-03-08T02:30", "America/New_York").kind, "nonexistent");

  // An unknown zone or an impossible calendar date is refused rather than silently defaulted.
  assert.equal(resolvePickupToUtc("2026-10-18T13:30", "Not/AZone").kind, "invalid");
  assert.equal(resolvePickupToUtc("2026-02-30T13:30", "UTC").kind, "invalid");
  assert.equal(resolvePickupToUtc("2026-10-18 13:30", "UTC").kind, "invalid");
  assert.equal(resolvePickupToUtc("", "UTC").kind, "empty");
});

test("form validation is field-adjacent and the payload is canonical", () => {
  const good = {
    ...emptyQuoteForm(),
    cartSubtotal: "500000",
    weightKilograms: "1.999",
    itemCount: "2",
    pickupLocal: "2026-10-18T13:30",
  };
  assert.deepEqual(validateQuoteForm(good), []);

  const bad = { ...good, cartSubtotal: "-1", weightKilograms: "1.0005", itemCount: "0" };
  const issues = validateQuoteForm(bad);
  assert.deepEqual(issues.map((i) => i.field).sort(), ["cartSubtotal", "itemCount", "weightKilograms"]);

  const payload = quoteFormToPayload(good, {
    countryId: COUNTRY,
    administrativeAreaId: AREA,
    cityId: null,
    serviceAreaId: null,
  });
  assert.deepEqual(payload, {
    scope: { countryId: COUNTRY, administrativeAreaId: AREA, cityId: null, serviceAreaId: null },
    cartSubtotalExclusive: 500000,
    currencyCode: "IRR",
    optionCode: null,
    weightGrams: 1999,
    itemCount: 2,
    requestedPickupAtUtc: "2026-10-18T10:00:00.000Z",
  });
});

test("currency mismatch is visible rather than silently normalized", () => {
  const mapped = mapOperationalQuote({
    outcome: "Quoted",
    serviceCode: "tipax",
    currencyCode: "USD",
    reasonCode: "shipping.operational_quote.quoted",
    isQuoted: true,
  })!;
  assert.equal(currencyMatchesRequest("IRR", mapped), false);
  assert.equal(currencyMatchesRequest("USD", mapped), true);
  assert.equal(
    currencyMatchesRequest("IRR", mapOperationalQuote({
      outcome: "Quoted",
      serviceCode: "tipax",
      currencyCode: null,
      reasonCode: "shipping.operational_quote.quoted",
      isQuoted: true,
    })!),
    true,
  );
});

test("the matched tier description stays descriptive and never computes a price", () => {
  assert.equal(conditionMatchFa(null).includes("بدون پلهٔ اندازهٔ منطبق"), true);
  assert.equal(conditionMatchFa({ weight: { min: 1000, max: 2000 }, count: null }).includes("1kg"), true);
  assert.equal(conditionMatchFa({ weight: { min: 1000, max: 2000 }, count: null }).includes("2kg"), true);
  assert.equal(conditionMatchFa({ weight: null, count: { min: 2, max: null } }).includes("اقلام ≥ 2"), true);
});

test("quoted vs eligible is never conflated, and a null amount is never a zero", () => {
  const quoted = mapOperationalQuote({
    outcome: "Quoted",
    serviceCode: "tipax",
    reasonCode: "shipping.operational_quote.quoted",
    freightAmount: 260000,
    packagingAmount: 0,
    isQuoted: true,
    schedule: { availability: "Unavailable", reasonCode: "shipping.schedule.outside_windows", isConstraintSatisfied: false },
  })!;
  assert.equal(isQuotedResult(quoted), true);
  // Quoted but the carrier window constraint is not satisfied: priced, yet not operationally eligible.
  assert.equal(quoted.isOperationallyEligible, false);
  assert.equal(formatAmountFa(quoted.packagingAmount, "IRR").includes("IRR"), true);

  const notQuoted = mapOperationalQuote({
    outcome: "PackagingAmbiguous",
    serviceCode: "tipax",
    reasonCode: "shipping.operational_quote.packaging_ambiguous",
    packagingAmount: null,
    isQuoted: false,
  })!;
  assert.equal(isQuotedResult(notQuoted), false);
  assert.equal(formatAmountFa(notQuoted.packagingAmount, "IRR"), NOT_DECLARED_FA);
  assert.equal(rawAmount(null), NOT_DECLARED_FA);
  assert.notEqual(formatAmountFa(null, "IRR"), formatAmountFa(0, "IRR"));
});
