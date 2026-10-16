/**
 * TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155 — focused frontend mapping/render tests for the
 * server-authoritative per-method financial status.
 *
 * These are pure mapping tests (no server, no React): they prove the Admin mapping cannot turn an
 * unconfigured, legacy-shared, ambiguous or unreadable status into a ready/free/zero claim, and that
 * sibling-option rows never become this method's money.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  financialCaveatsFa,
  financialMoneyFa,
  financialReasonFa,
  financialStateLabelFa,
  financialStateTone,
  hasProvenOwnedMoney,
  mapShippingMethodFinancialStatus,
  provenOwnedAmount,
} from "./shipping-method-financial-model.ts";
import { shippingMethodFinancialStatusPath } from "./shipping-method-financial-api.ts";

function payload(overrides: Record<string, unknown> = {}) {
  return {
    serviceCode: "tipax",
    optionCode: "standard",
    state: "FinanciallyConfigured",
    mode: "FixedAmount",
    currencyCode: "IRR",
    ruleCount: 1,
    conditionedRuleCount: 0,
    hasZeroChargeRule: false,
    activeServiceLevelRuleCount: 0,
    activeSiblingOptionRuleCount: 1,
    activeCoverageGrantCount: 1,
    rules: [
      {
        ratePolicyId: "11111111-1111-1111-1111-111111111111",
        scopeLevel: "City",
        chargeAmount: 41000,
        currencyCode: "IRR",
        preparationDays: 2,
        leadDays: 3,
        freeAboveThreshold: null,
        isConditioned: false,
      },
    ],
    reasonCode: "shipping.financial.owned_rules_present",
    ...overrides,
  };
}

test("a method-owned active rule maps to proven fixed money in its own currency", () => {
  const status = mapShippingMethodFinancialStatus(payload());
  assert.ok(status);
  assert.equal(status.state, "financially-configured");
  assert.equal(status.mode, "fixed-amount");
  assert.equal(hasProvenOwnedMoney(status), true);
  assert.equal(provenOwnedAmount(status), 41000);
  assert.match(financialMoneyFa(status) ?? "", /۴۱٬۰۰۰/);
  assert.equal(financialStateTone(status.state), "success");
});

test("an option with no owned rule maps to needs-configuration and never to free", () => {
  const status = mapShippingMethodFinancialStatus(payload({
    optionCode: "express",
    state: "NeedsConfiguration",
    mode: "None",
    currencyCode: null,
    ruleCount: 0,
    rules: [],
    reasonCode: "shipping.financial.no_owned_rules",
  }));
  assert.ok(status);
  assert.equal(hasProvenOwnedMoney(status), false);
  assert.equal(provenOwnedAmount(status), null);
  assert.equal(financialMoneyFa(status), null);
  assert.equal(financialStateTone(status.state), "warning");
  assert.match(financialReasonFa(status), /فعال‌بودن روش به‌معنای رایگان نبودن است/);
  // The sibling row is visible as a count only; it can never be this method's money.
  assert.match(financialCaveatsFa(status).join(" | "), /گزینه‌های دیگر همین سرویس/);
});

test("a service-level legacy tariff is never projected as the option method's money", () => {
  const status = mapShippingMethodFinancialStatus(payload({
    state: "LegacySharedUnresolved",
    mode: "None",
    currencyCode: null,
    ruleCount: 0,
    rules: [],
    activeServiceLevelRuleCount: 1,
    reasonCode: "shipping.financial.legacy_service_level_shared",
  }));
  assert.ok(status);
  assert.equal(hasProvenOwnedMoney(status), false);
  assert.equal(financialMoneyFa(status), null);
  assert.equal(financialStateLabelFa(status.state), "مالی: تعرفهٔ مشترک قدیمی");
  assert.match(financialCaveatsFa(status).join(" | "), /بدون مالکیت این روش/);
});

test("an ambiguous owned rule set maps to a conflict with no single amount", () => {
  const status = mapShippingMethodFinancialStatus(payload({
    state: "Conflicting",
    currencyCode: null,
    ruleCount: 2,
    rules: [
      { ratePolicyId: "a", scopeLevel: "City", chargeAmount: 20000, currencyCode: "IRR" },
      { ratePolicyId: "b", scopeLevel: "City", chargeAmount: 21000, currencyCode: "IRR" },
    ],
    reasonCode: "shipping.financial.ambiguous_owned_scope",
  }));
  assert.ok(status);
  assert.equal(status.mode, "fixed-amount");
  assert.equal(hasProvenOwnedMoney(status), false);
  assert.equal(provenOwnedAmount(status), null);
  assert.equal(financialStateTone(status.state), "danger");
  assert.match(financialReasonFa(status), /دامنه و شرط یکسان/);
});

test("a configured method without any coverage grant states that nothing is quotable yet", () => {
  const status = mapShippingMethodFinancialStatus(payload({ activeCoverageGrantCount: 0 }));
  assert.ok(status);
  assert.equal(hasProvenOwnedMoney(status), true);
  assert.match(financialCaveatsFa(status).join(" | "), /هیچ مجوز پوشش فعالی/);
});

test("a stored zero amount is surfaced as data and never as an approved free mode", () => {
  const status = mapShippingMethodFinancialStatus(payload({
    hasZeroChargeRule: true,
    rules: [{ ratePolicyId: "a", scopeLevel: "City", chargeAmount: 0, currencyCode: "IRR" }],
  }));
  assert.ok(status);
  // The zero is a data fact, not a proved money representation: no amount is shown for it.
  assert.equal(provenOwnedAmount(status), 0);
  assert.equal(financialMoneyFa(status), null);
  assert.match(financialCaveatsFa(status).join(" | "), /هیچ حالت «رایگان» صریحی ندارد/);
});

test("several scoped rules are a rule set, not one invented number", () => {
  const status = mapShippingMethodFinancialStatus(payload({
    ruleCount: 2,
    rules: [
      { ratePolicyId: "a", scopeLevel: "City", chargeAmount: 20000, currencyCode: "IRR" },
      { ratePolicyId: "b", scopeLevel: "AdministrativeArea", chargeAmount: 26000, currencyCode: "IRR" },
    ],
  }));
  assert.ok(status);
  assert.equal(provenOwnedAmount(status), null);
  assert.equal(financialMoneyFa(status), "مبلغ ثابت به‌ازای هر قاعده: 2 قاعده (ارز IRR)");
});

test("an unreadable or unknown server answer is mapped fail-closed, never as ready", () => {
  assert.equal(mapShippingMethodFinancialStatus(null), null);
  assert.equal(mapShippingMethodFinancialStatus({ serviceCode: "tipax" }), null);
  assert.equal(mapShippingMethodFinancialStatus({ reasonCode: "x" }), null);

  const unknown = mapShippingMethodFinancialStatus(payload({ state: "SomethingNew", mode: "ProviderQuote" }));
  assert.ok(unknown);
  assert.equal(unknown.state, "unknown");
  assert.equal(unknown.mode, "unknown");
  assert.equal(hasProvenOwnedMoney(unknown), false);
  assert.equal(financialStateTone(unknown.state), "neutral");
  assert.equal(financialStateLabelFa(unknown.state), "مالی: نامشخص");
});

test("the read path carries the authoritative identity and omits an absent option", () => {
  assert.equal(
    shippingMethodFinancialStatusPath("tipax", "standard"),
    "/v1/admin/shipping-services/tipax/financial-status?optionCode=standard",
  );
  assert.equal(
    shippingMethodFinancialStatusPath("store_courier", null),
    "/v1/admin/shipping-services/store_courier/financial-status",
  );
});
