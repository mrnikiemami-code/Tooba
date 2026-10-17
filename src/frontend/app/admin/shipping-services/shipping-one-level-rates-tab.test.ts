import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  bindingContextFa,
  bindingUnavailableFa,
  createBindingGenerationGuard,
  grantBelongsToBinding,
  isRetryableBindingFailure,
  mapHostBinding,
  oneLevelBindingPath,
  rateBelongsToBinding,
  resolveRatesState,
  scopeRatesToBinding,
  viewMatchesBindingService,
  type OneLevelMethodBinding,
} from "./shipping-one-level-rates-model.ts";
import type {
  ShippingCoverageGrantAdminItem,
  ShippingRatePolicyAdminItem,
  ShippingRatesAndCoverageView,
} from "./shipping-rates-coverage-api.ts";

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ONE-LEVEL-TABS-086 — tab 2 «تعرفه و پوشش».
 *
 * The tests below are deliberately about the *decisions* this tab makes, not about a running
 * server: with the gate OFF (its real Development state) there is no successful live UI/DB path to
 * assert, and pretending otherwise would be a fake PASS. What is asserted here is exactly what can
 * be asserted truthfully:
 *   - a method code is never split into a service/option pair in the browser;
 *   - an unresolved/off/unmigrated binding never degrades into a guessed binding;
 *   - only the rates/grants of the resolved binding are ever shown or written;
 *   - the selected method is immutable while an operation is pending;
 *   - the tab stays inside the locked five-tab URL/context model and adds no second level.
 * ------------------------------------------------------------------------------------------- */

const BINDING: OneLevelMethodBinding = {
  methodCode: "post:standard",
  serviceCode: "post",
  optionCode: "standard",
  isActive: true,
};

function rate(partial: Partial<ShippingRatePolicyAdminItem>): ShippingRatePolicyAdminItem {
  return {
    ratePolicyId: "r1",
    serviceCode: "post",
    optionCode: "standard",
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
    level: "country",
    chargeAmount: 100,
    currencyCode: "IRR",
    preparationDays: 1,
    leadDays: 2,
    freeAboveThreshold: null,
    isActive: true,
    condition: null,
    ...partial,
  };
}

function grant(partial: Partial<ShippingCoverageGrantAdminItem>): ShippingCoverageGrantAdminItem {
  return {
    coverageGrantId: "g1",
    serviceCode: "post",
    optionCode: null,
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
    level: "country",
    isActive: true,
    ...partial,
  };
}

test("the binding path is derived from the method code only and stays on the authorized admin route", () => {
  assert.equal(
    oneLevelBindingPath("post:standard"),
    "/v1/admin/shipping-services/shipping-methods/one-level/post%3Astandard/binding",
  );
  assert.equal(
    oneLevelBindingPath("store_courier"),
    "/v1/admin/shipping-services/shipping-methods/one-level/store_courier/binding",
  );
  // کد روش هرگز به سرویس/گزینه شکسته نمی‌شود؛ فقط به‌عنوان یک شناسهٔ یکپارچه encode می‌شود.
  const path = oneLevelBindingPath("tipax:express");
  assert.equal(path.includes("serviceCode"), false);
  assert.equal(path.includes("optionCode"), false);
  assert.match(path, /tipax%3Aexpress/);
});

test("a resolved binding requires Enabled + ready + resolved + a real service code", () => {
  const resolved = mapHostBinding({
    gate: "Enabled",
    ready: true,
    resolved: true,
    methodCode: "post:standard",
    serviceCode: "post",
    optionCode: "standard",
    isActive: true,
  });
  assert.deepEqual(resolved, { state: "resolved", binding: BINDING });

  // روش بدون گزینه: optionCode تهی یعنی سیاست سطح خدمت، نه گزینهٔ حدسی.
  assert.deepEqual(
    mapHostBinding({
      gate: "Enabled",
      ready: true,
      resolved: true,
      methodCode: "store_courier",
      serviceCode: "store_courier",
      optionCode: null,
      isActive: true,
    }),
    {
      state: "resolved",
      binding: { methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true },
    },
  );

  // پاسخ PascalCase سرور نیز پذیرفته می‌شود (قرارداد سیم فقط یک شکل دارد).
  assert.equal(
    (mapHostBinding({ Gate: "Enabled", Ready: true, Resolved: true, MethodCode: "tipax:express", ServiceCode: "tipax", OptionCode: "express" }) as { state: string }).state,
    "resolved",
  );
});

test("gate-off, unmigrated, unknown and malformed binding answers never become a guessed binding", () => {
  assert.deepEqual(mapHostBinding({ gate: "Disabled", ready: false, resolved: false }), { state: "gate-off" });
  assert.deepEqual(mapHostBinding({ gate: "Enabled", ready: false, resolved: false }), { state: "not-ready" });
  assert.deepEqual(mapHostBinding({ gate: "Enabled", ready: true, resolved: false }), { state: "unresolved" });
  // resolved=true ولی بدون serviceCode معتبر ⇒ نامعتبر، نه پیوند حدسی.
  assert.deepEqual(
    mapHostBinding({ gate: "Enabled", ready: true, resolved: true, methodCode: "post:standard", serviceCode: "  " }),
    { state: "untrusted" },
  );
  assert.deepEqual(mapHostBinding(null), { state: "untrusted" });
  assert.deepEqual(mapHostBinding("nope"), { state: "untrusted" });
  // دروازهٔ ناموجود/ناشناخته هرگز Enabled فرض نمی‌شود.
  assert.deepEqual(mapHostBinding({ ready: true, resolved: true, serviceCode: "post" }), { state: "gate-off" });
});

test("only the resolved binding's own option rates and its own option grants are shown", () => {
  const view: ShippingRatesAndCoverageView = {
    serviceCode: "post",
    ratePolicies: [
      rate({ ratePolicyId: "mine", optionCode: "standard" }),
      rate({ ratePolicyId: "other-option", optionCode: "express" }),
      rate({ ratePolicyId: "service-level", optionCode: null }),
      rate({ ratePolicyId: "other-service", serviceCode: "tipax" }),
    ],
    coverageGrants: [
      grant({ coverageGrantId: "mine-grant", optionCode: "standard" }),
      grant({ coverageGrantId: "other-option-grant", optionCode: "express" }),
      grant({ coverageGrantId: "service-level-grant", optionCode: null }),
      grant({ coverageGrantId: "other-service-grant", serviceCode: "tipax" }),
    ],
    methodCoverages: [],
  };

  const scoped = scopeRatesToBinding(view, BINDING);
  assert.deepEqual(scoped.rates.map((r) => r.ratePolicyId), ["mine"]);
  assert.deepEqual(scoped.grants.map((g) => g.coverageGrantId), ["mine-grant"]);

  // سیاست سطح خدمت (optionCode=null) زیر نام روش گزینه‌دار نمایش داده نمی‌شود؛ این همان
  // «سطح دوم پنهان» است که این تسک اجازهٔ ساختنش را ندارد.
  assert.equal(rateBelongsToBinding(rate({ optionCode: null }), BINDING), false);
  assert.equal(rateBelongsToBinding(rate({ optionCode: "standard" }), BINDING), true);
  assert.equal(rateBelongsToBinding(rate({ serviceCode: "tipax" }), BINDING), false);
  // TB-173: هویت مجوز هم (خدمت + گزینه) است؛ مجوز گزینهٔ هم‌خانواده یا سطح سرویس به این روش
  // به ارث نمی‌رسد (همان تقریب provider-level که تسک ممنوع کرده است).
  assert.equal(grantBelongsToBinding(grant({ optionCode: "standard" }), BINDING), true);
  assert.equal(grantBelongsToBinding(grant({ optionCode: "express" }), BINDING), false);
  assert.equal(grantBelongsToBinding(grant({ optionCode: null }), BINDING), false);
  assert.equal(grantBelongsToBinding(grant({ serviceCode: "tipax", optionCode: "standard" }), BINDING), false);
  // روش بدون گزینه فقط مجوز سطح سرویس خودش را می‌بیند.
  assert.equal(
    grantBelongsToBinding(grant({ serviceCode: "store_courier", optionCode: null }), {
      methodCode: "store_courier",
      serviceCode: "store_courier",
      optionCode: null,
      isActive: true,
    }),
    true,
  );
});

test("a methodless binding matches only service-level rates, and an empty view stays empty", () => {
  const optionless: OneLevelMethodBinding = {
    methodCode: "store_courier",
    serviceCode: "store_courier",
    optionCode: null,
    isActive: true,
  };
  const view: ShippingRatesAndCoverageView = {
    serviceCode: "store_courier",
    ratePolicies: [rate({ serviceCode: "store_courier", optionCode: null }), rate({ serviceCode: "store_courier", optionCode: "express" })],
    coverageGrants: [grant({ serviceCode: "store_courier", optionCode: null })],
    methodCoverages: [],
  };
  assert.deepEqual(scopeRatesToBinding(view, optionless).rates.map((r) => r.ratePolicyId), ["r1"]);
  assert.equal(scopeRatesToBinding(view, optionless).rates.length, 1);
  assert.deepEqual(scopeRatesToBinding(null, optionless), { rates: [], grants: [] });
});

test("a response belonging to another service is refused rather than rendered under this method", () => {
  assert.equal(viewMatchesBindingService({ serviceCode: "post", ratePolicies: [], coverageGrants: [], methodCoverages: [] }, BINDING), true);
  assert.equal(viewMatchesBindingService({ serviceCode: "tipax", ratePolicies: [], coverageGrants: [], methodCoverages: [] }, BINDING), false);
  assert.equal(viewMatchesBindingService(null, BINDING), false);
});

test("the tab state machine never reports ready without a resolved binding", () => {
  const resolvedState = { state: "resolved" as const, binding: BINDING };

  assert.equal(
    resolveRatesState({ hasMethod: false, bindingLoading: false, bindingState: null, ratesLoading: false, ratesState: null }),
    "no-method",
  );
  assert.equal(
    resolveRatesState({ hasMethod: true, bindingLoading: true, bindingState: null, ratesLoading: false, ratesState: null }),
    "binding-loading",
  );
  for (const state of [
    { state: "gate-off" as const },
    { state: "not-ready" as const },
    { state: "unresolved" as const },
    { state: "untrusted" as const },
  ]) {
    assert.equal(
      resolveRatesState({ hasMethod: true, bindingLoading: false, bindingState: state, ratesLoading: false, ratesState: "ok" }),
      "binding-unavailable",
      `${state.state} must never be ready`,
    );
  }
  assert.equal(
    resolveRatesState({ hasMethod: true, bindingLoading: false, bindingState: resolvedState, ratesLoading: false, ratesState: null }),
    "loading",
  );
  assert.equal(
    resolveRatesState({ hasMethod: true, bindingLoading: false, bindingState: resolvedState, ratesLoading: false, ratesState: "denied" }),
    "denied",
  );
  assert.equal(
    resolveRatesState({ hasMethod: true, bindingLoading: false, bindingState: resolvedState, ratesLoading: false, ratesState: "error" }),
    "error",
  );
  assert.equal(
    resolveRatesState({ hasMethod: true, bindingLoading: false, bindingState: resolvedState, ratesLoading: false, ratesState: "ok" }),
    "ready",
  );
});

test("binding unavailability text is explicit, Persian and never suggests a fallback", () => {
  assert.match(bindingUnavailableFa({ state: "gate-off" }, "post:standard"), /دروازه/);
  assert.match(bindingUnavailableFa({ state: "not-ready" }, "post:standard"), /مهاجرت/);
  assert.match(bindingUnavailableFa({ state: "unresolved" }, "post:standard"), /post:standard/);
  assert.match(bindingUnavailableFa({ state: "untrusted" }, "post:standard"), /معتبر نبود/);
  // قطع ارتباط و رد دسترسی نباید با «پاسخ نامعتبر» یکی شوند؛ هر کدام علت و اقدام خودش را دارد.
  const unreachable = bindingUnavailableFa({ state: "unreachable" }, "post:standard");
  assert.match(unreachable, /ارتباط با سرور/);
  assert.doesNotMatch(unreachable, /معتبر نبود/);
  const denied = bindingUnavailableFa({ state: "denied" }, "post:standard");
  assert.match(denied, /دسترسی/);
  assert.doesNotMatch(denied, /معتبر نبود/);
  // فقط شکست‌های گذرا «تلاش دوباره» معنادار دارند؛ دادهٔ نامعتبر یا دروازهٔ بسته نه.
  assert.equal(isRetryableBindingFailure({ state: "unreachable" }), true);
  assert.equal(isRetryableBindingFailure({ state: "denied" }), true);
  assert.equal(isRetryableBindingFailure({ state: "untrusted" }), false);
  assert.equal(isRetryableBindingFailure({ state: "resolved", binding: BINDING }), false);
  assert.match(bindingUnavailableFa(null, "post:standard"), /post:standard/);
});

test("the generation guard invalidates an in-flight operation when the method changes", () => {
  const guard = createBindingGenerationGuard();
  const first = guard.begin();
  assert.equal(first(), true);
  const second = guard.begin();
  // پاسخ روش قبلی هرگز روی روش جدید اعمال نمی‌شود.
  assert.equal(first(), false);
  assert.equal(second(), true);
});

test("the binding context names the selected one-level method and its resolved legacy pair", () => {
  const text = bindingContextFa(BINDING);
  assert.match(text, /post:standard/);
  assert.match(text, /post/);
  assert.match(text, /standard/);
  assert.match(bindingContextFa({ ...BINDING, optionCode: null }), /سطح سرویس/);
});

test("tab 2 renders no second-level option picker and no editable parent-service grouping", () => {
  const source = fs
    .readFileSync(new URL("./shipping-one-level-rates-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  // هیچ انتخابگر گزینهٔ سطح دوم، هیچ ستون گزینه و هیچ گروه‌بندی خدمت والد وجود ندارد.
  assert.equal(/<select/.test(source), false);
  assert.equal(/groupRatesByOption/.test(source), false);
  assert.equal(/rateOptionLabelFa/.test(source), false);
  assert.equal(/createOptionCode/.test(source), false);
  // هویت گزینه فقط از پیوند سرور می‌آید، نه از ورودی کاربر.
  assert.match(source, /optionCode: resolvedBinding\.optionCode/);
  assert.match(source, /optionCode: policy\.optionCode/);
  // هیچ شکستن کد روش در مرورگر انجام نمی‌شود.
  assert.equal(/methodCode\.split/.test(source), false);
  assert.equal(/\.split\(":"\)/.test(source), false);
});
