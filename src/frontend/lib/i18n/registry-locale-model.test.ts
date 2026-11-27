import assert from "node:assert/strict";
import test from "node:test";
import { parseRegistryLanguages, resolveRegistryLanguage, renderingFallback } from "./registry-locale-model.ts";
import { registryPanelSwitchPath } from "./routing.ts";

const rows = [
  { code: "fa-IR", urlPrefix: "persian", culture: "fa-IR", direction: "RTL", calendarDisplay: "Jalali", isActive: true, isDefault: true, nativeName: "فارسی" },
  { code: "tr-TR", urlPrefix: "turkish", culture: "tr-TR", direction: "LTR", calendarDisplay: "Gregorian", isActive: true, isDefault: false, nativeName: "Türkçe" },
  { code: "en-US", urlPrefix: "english", culture: "en-US", direction: "LTR", calendarDisplay: "Gregorian", isActive: false, isDefault: false },
];
test("registry—not two-letter guesses—owns enabled locale, prefix, direction and calendar", () => {
  const active = parseRegistryLanguages(rows);
  assert.equal(active.length, 2);
  assert.equal(resolveRegistryLanguage(active, "turkish")?.culture, "tr-TR");
  assert.equal(resolveRegistryLanguage(active, "turkish")?.direction, "ltr");
  assert.equal(resolveRegistryLanguage(active, "english")?.urlPrefix, "persian");
  assert.equal(resolveRegistryLanguage(active, "tr")?.calendar, "jalali");
  assert.equal(resolveRegistryLanguage([], "persian"), null);
  assert.equal(renderingFallback("fa").active, false);
});
test("malformed active registry rows do not become enabled fake languages", () => {
  assert.throws(() => parseRegistryLanguages([{ ...rows[0], culture: "" }]));
  assert.throws(() => parseRegistryLanguages([rows[0], { ...rows[1], urlPrefix: "persian" }]));
});

test("panel language switching preserves offer identity, seller context and tab", () => {
  assert.equal(registryPanelSwitchPath("turkish", "/persian/vendor-panel/products/abc", "?sellerPartyId=seller-a&tab=pricing", "#price"),
    "/turkish/vendor-panel/products/abc?sellerPartyId=seller-a&tab=pricing#price");
  assert.equal(registryPanelSwitchPath("english", "/admin/offers/new", "?tab=inventory"),
    "/english/admin/offers/new?tab=inventory");
  assert.equal(registryPanelSwitchPath("english", "/products/abc"), null);
  assert.equal(registryPanelSwitchPath("../bad", "/admin/offers"), null);
});
