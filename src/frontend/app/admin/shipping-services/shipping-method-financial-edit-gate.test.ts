/**
 * TB-SHIPPING-METHOD-FINANCIAL-EDIT-156 — focused tests for the fail-closed monetary edit gate.
 *
 * The gate consumes the verified TB-155 status; these tests prove it never authors or overwrites a
 * commercial value on an unproven, ambiguous or legacy-shared basis, and that a stored zero stays a
 * data amount rather than an invented free mode.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  gateAllowsCreate,
  gateAllowsInPlaceEdit,
  gateBlocksWrites,
  isStoredZeroAmount,
  resolveFinancialEditGate,
  ZERO_AMOUNT_NOTE_FA,
} from "./shipping-method-financial-edit-gate.ts";
import type { ShippingMethodFinancialStatus } from "./shipping-method-financial-model.ts";

function status(overrides: Partial<ShippingMethodFinancialStatus> = {}): ShippingMethodFinancialStatus {
  return {
    serviceCode: "tipax",
    optionCode: "standard",
    state: "financially-configured",
    mode: "fixed-amount",
    currencyCode: "IRR",
    ruleCount: 1,
    conditionedRuleCount: 0,
    hasZeroChargeRule: false,
    activeServiceLevelRuleCount: 0,
    activeSiblingOptionRuleCount: 0,
    activeCoverageGrantCount: 1,
    rules: [],
    reasonCode: "shipping.financial.owned_rules_present",
    ...overrides,
  };
}

test("an unconfigured method allows authoring its first owned rule and is never called free", () => {
  const gate = resolveFinancialEditGate(status({ state: "needs-configuration", mode: "none", currencyCode: null, ruleCount: 0 }));
  assert.equal(gate.kind, "create-allowed");
  assert.equal(gateAllowsCreate(gate), true);
  assert.equal(gateAllowsInPlaceEdit(gate), false);
  assert.equal(gateBlocksWrites(gate), false);
  assert.match(gate.reasonFa, /رایگان محسوب نمی‌شود/);
});

test("a single verified owned rule allows in-place edit of that same rule", () => {
  const gate = resolveFinancialEditGate(status());
  assert.equal(gate.kind, "edit-allowed");
  assert.equal(gateAllowsInPlaceEdit(gate), true);
  assert.equal(gateAllowsCreate(gate), true);
  assert.equal(gateBlocksWrites(gate), false);
});

test("a legacy service-level tariff is never editable or inherited here", () => {
  const gate = resolveFinancialEditGate(status({
    state: "legacy-shared-unresolved",
    mode: "none",
    currencyCode: null,
    ruleCount: 0,
    activeServiceLevelRuleCount: 2,
    reasonCode: "shipping.financial.legacy_service_level_shared",
  }));
  assert.equal(gate.kind, "legacy-shared");
  // The shared rows are not edited; authoring this method's own rule remains the correct action.
  assert.equal(gateAllowsInPlaceEdit(gate), false);
  assert.equal(gateAllowsCreate(gate), true);
  assert.match(gate.reasonFa, /مالکیت این روش را اثبات نمی‌کند/);
  assert.match(gate.reasonFa, /قاعدهٔ اختصاصی تازه/);
});

test("competing owned rules block both create and edit instead of choosing one silently", () => {
  const gate = resolveFinancialEditGate(status({
    state: "conflicting",
    ruleCount: 2,
    reasonCode: "shipping.financial.ambiguous_owned_scope",
  }));
  assert.equal(gate.kind, "blocked-conflict");
  assert.equal(gateBlocksWrites(gate), true);
  assert.equal(gateAllowsCreate(gate), false);
  assert.equal(gateAllowsInPlaceEdit(gate), false);
  assert.match(gate.reasonFa, /غیرفعال یا حذف کنید/);
});

test("mixed currencies are reported as their own precise blocker", () => {
  const gate = resolveFinancialEditGate(status({
    state: "conflicting",
    currencyCode: null,
    ruleCount: 2,
    reasonCode: "shipping.financial.mixed_currency",
  }));
  assert.equal(gate.kind, "blocked-conflict");
  assert.match(gate.reasonFa, /بیش از یک ارز/);
});

test("an unverified or unknown status blocks every write", () => {
  const unreadable = resolveFinancialEditGate(null);
  assert.equal(unreadable.kind, "blocked-unverified");
  assert.equal(gateBlocksWrites(unreadable), true);
  assert.equal(gateAllowsCreate(unreadable), false);
  assert.equal(gateAllowsInPlaceEdit(unreadable), false);

  const unknown = resolveFinancialEditGate(status({ state: "unknown", mode: "unknown", currencyCode: null }));
  assert.equal(unknown.kind, "blocked-unverified");
  assert.equal(gateBlocksWrites(unknown), true);
});

test("a stored zero is a data amount, never an invented free mode", () => {
  assert.equal(isStoredZeroAmount("0"), true);
  assert.equal(isStoredZeroAmount("0.00"), true);
  assert.equal(isStoredZeroAmount(""), false);
  assert.equal(isStoredZeroAmount("  "), false);
  assert.equal(isStoredZeroAmount("25000"), false);
  assert.match(ZERO_AMOUNT_NOTE_FA, /نه حالت «رایگان به مشتری»/);
});
