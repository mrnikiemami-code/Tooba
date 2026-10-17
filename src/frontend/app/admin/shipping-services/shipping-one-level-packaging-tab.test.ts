import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  ONE_LEVEL_NO_PACKAGING_FA,
  ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA,
  ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA,
  ONE_LEVEL_PACKAGING_LEGACY_ROW_FA,
  ONE_LEVEL_PACKAGING_READ_ONLY_FA,
  operationStillTargetsMethod,
  packagingBelongsToBinding,
  packagingViewMatchesBindingService,
  packagingWritesAllowed,
  packagingWriteTarget,
  resolvePackagingState,
  scopePackagingToBinding,
  type OneLevelBindingState,
  type OneLevelMethodBinding,
} from "./shipping-one-level-packaging-model.ts";
import type { ShippingPackagingPolicyAdminItem } from "./shipping-packaging-api.ts";

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ONE-LEVEL-TABS-087 — tab 3 «بسته‌بندی و کرایه».
 *
 * With the gate OFF and the one-level tables unmigrated there is no successful live path to assert,
 * so these tests cover the decisions this tab makes: the binding is never guessed, only the selected
 * method's own policies are shown or written, a service-level fallback is never presented as a second
 * level, and a stale operation never lands on the newly selected method.
 * ------------------------------------------------------------------------------------------- */

const POST_STANDARD: OneLevelMethodBinding = {
  methodCode: "post:standard",
  serviceCode: "post",
  optionCode: "standard",
  isActive: true,
};

function policy(partial: Partial<ShippingPackagingPolicyAdminItem>): ShippingPackagingPolicyAdminItem {
  return {
    packagingPolicyId: "p1",
    serviceCode: "post",
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
    level: "country",
    feeAmount: 12000,
    currencyCode: "IRR",
    freightPayerMode: "Prepaid",
    isActive: true,
    createdAt: "2026-10-15T00:00:00Z",
    updatedAt: "2026-10-15T00:00:00Z",
    optionCode: "standard",
    ...partial,
  };
}

test("only the selected method's own option policies are shown", () => {
  const policies = [
    policy({ packagingPolicyId: "mine", optionCode: "standard" }),
    policy({ packagingPolicyId: "other-option", optionCode: "express" }),
    policy({ packagingPolicyId: "service-level", optionCode: null }),
    policy({ packagingPolicyId: "other-service", serviceCode: "tipax" }),
  ];

  const scoped = scopePackagingToBinding(policies, POST_STANDARD);
  assert.deepEqual(scoped.map((p) => p.packagingPolicyId), ["mine"]);

  // سیاست سطح خدمت زیر نام روش گزینه‌دار نمایش داده نمی‌شود؛ این همان «سطح دوم پنهان» است.
  assert.equal(packagingBelongsToBinding(policy({ optionCode: null }), POST_STANDARD), false);
  assert.equal(packagingBelongsToBinding(policy({ optionCode: "standard" }), POST_STANDARD), true);
  assert.equal(packagingBelongsToBinding(policy({ serviceCode: "tipax" }), POST_STANDARD), false);
});

test("an optionless method matches only the documented service-level fallback", () => {
  const storeCourier: OneLevelMethodBinding = {
    methodCode: "store_courier",
    serviceCode: "store_courier",
    optionCode: null,
    isActive: true,
  };
  const policies = [
    policy({ packagingPolicyId: "fallback", serviceCode: "store_courier", optionCode: null }),
    policy({ packagingPolicyId: "stray-option", serviceCode: "store_courier", optionCode: "express" }),
  ];

  assert.deepEqual(scopePackagingToBinding(policies, storeCourier).map((p) => p.packagingPolicyId), ["fallback"]);
  assert.deepEqual(scopePackagingToBinding(null, storeCourier), []);
});

test("tipax:express never borrows tipax:standard policies and an unmapped method shows nothing", () => {
  const tipaxExpress: OneLevelMethodBinding = {
    methodCode: "tipax:express",
    serviceCode: "tipax",
    optionCode: "express",
    isActive: true,
  };
  const policies = [
    policy({ packagingPolicyId: "tipax-standard", serviceCode: "tipax", optionCode: "standard" }),
    policy({ packagingPolicyId: "tipax-express", serviceCode: "tipax", optionCode: "express" }),
  ];

  assert.deepEqual(scopePackagingToBinding(policies, tipaxExpress).map((p) => p.packagingPolicyId), ["tipax-express"]);

  // روش بدون نگاشت: هیچ سیاستی از هیچ خدمتی قرض گرفته نمی‌شود (fail-closed).
  const unmapped: OneLevelMethodBinding = {
    methodCode: "in_person",
    serviceCode: "in_person",
    optionCode: null,
    isActive: false,
  };
  assert.deepEqual(scopePackagingToBinding(policies, unmapped), []);
});

test("the write target always comes from the server binding and never from the method text", () => {
  assert.deepEqual(packagingWriteTarget(POST_STANDARD), { serviceCode: "post", optionCode: "standard" });
  assert.deepEqual(
    packagingWriteTarget({ methodCode: "tipax:express", serviceCode: "tipax", optionCode: "express", isActive: true }),
    { serviceCode: "tipax", optionCode: "express" },
  );
  assert.deepEqual(
    packagingWriteTarget({ methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true }),
    { serviceCode: "store_courier", optionCode: null },
  );
});

test("a response belonging to another service is refused rather than rendered or edited", () => {
  assert.equal(packagingViewMatchesBindingService({ serviceCode: "post" }, POST_STANDARD), true);
  assert.equal(packagingViewMatchesBindingService({ serviceCode: "tipax" }, POST_STANDARD), false);
  assert.equal(packagingViewMatchesBindingService(null, POST_STANDARD), false);
});

test("the tab never reports ready without a resolved binding", () => {
  const base = {
    hasMethod: true,
    bindingLoading: false,
    bindingResolved: true,
    bindingKnown: true,
    policiesLoading: false,
    policiesState: "ok" as const,
  };

  assert.equal(resolvePackagingState({ ...base, hasMethod: false }), "no-method");
  assert.equal(resolvePackagingState({ ...base, bindingKnown: false }), "binding-loading");
  assert.equal(resolvePackagingState({ ...base, bindingLoading: true }), "binding-loading");
  assert.equal(resolvePackagingState({ ...base, bindingResolved: false }), "binding-unavailable");
  assert.equal(resolvePackagingState({ ...base, policiesState: null }), "loading");
  assert.equal(resolvePackagingState({ ...base, policiesState: "denied" }), "denied");
  assert.equal(resolvePackagingState({ ...base, policiesState: "error" }), "error");
  assert.equal(resolvePackagingState(base), "ready");

  // هر حالت غیرحل‌شدهٔ پیوند هرگز ready نمی‌شود، حتی اگر سیاست‌ها «موفق» خوانده شده باشند.
  for (const bindingState of [
    { state: "gate-off" as const },
    { state: "not-ready" as const },
    { state: "unresolved" as const },
    { state: "untrusted" as const },
  ] as OneLevelBindingState[]) {
    assert.equal(
      resolvePackagingState({ ...base, bindingResolved: bindingState.state === "resolved" }),
      "binding-unavailable",
      `${bindingState.state} must never be ready`,
    );
  }
});

test("a stale operation is never attributed to the newly selected method", () => {
  assert.equal(operationStillTargetsMethod("post:standard", "post:standard"), true);
  assert.equal(operationStillTargetsMethod("post:standard", "tipax:express"), false);
  assert.equal(operationStillTargetsMethod(null, "post:standard"), false);
});

test("the empty-state text states explicit zero rather than a hidden fee", () => {
  assert.match(ONE_LEVEL_NO_PACKAGING_FA, /صفر/);
  assert.match(ONE_LEVEL_NO_PACKAGING_FA, /پیش‌کرایه/);
});

test("TB-176 View mode blocks every packaging mutation route", () => {
  // یک منبع تصمیم خالص: فقط حالت مشاهده نوشتن را می‌بندد و حالت ویرایش باز می‌گذارد.
  assert.equal(packagingWritesAllowed(true), false);
  assert.equal(packagingWritesAllowed(false), true);
  assert.equal(packagingWritesAllowed(undefined), true);

  const source = fs
    .readFileSync(new URL("./shipping-one-level-packaging-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  // پرچم از دیالوگ می‌آید و تنها دروازهٔ نوشتن از همان تابع خالص ساخته می‌شود.
  assert.match(source, /readOnly\?: boolean/);
  assert.match(source, /readOnly = false/);
  assert.match(source, /const writesBlocked = !packagingWritesAllowed\(readOnly\)/);

  // هر سه مسیر نوشتن API در همین فایل به‌طور صریح و پیش از فراخوانی fail-closed می‌شوند.
  for (const fn of ["submitCreate", "submitEdit", "toggle", "startEdit"]) {
    const start = source.indexOf(`function ${fn}(`);
    assert.notEqual(start, -1, `${fn} must exist`);
    const body = source.slice(start, start + 400);
    assert.match(body, /if \(writesBlocked\)/, `${fn} must guard before writing`);
    assert.match(body, /ONE_LEVEL_PACKAGING_READ_ONLY_FA/, `${fn} must refuse with the stable read-only text`);
  }

  // ورود به فرم ویرایش هم مسیر نوشتن است: فقط از startEdit و با همان گارد.
  assert.equal((source.match(/setEditingId\(policy\.packagingPolicyId\)/g) || []).length, 1);
  assert.match(source, /onClick=\{\(\) => startEdit\(policy\)\}/);

  // هر کنترل تغییردهنده و دکمهٔ نوشتن با همان گارد غیرفعال می‌شود.
  assert.equal(/disabled=\{busy\}/.test(source), false);
  assert.match(source, /disabled=\{busy \|\| writesBlocked\}/);
  assert.match(source, /<ShippingDestinationPicker[^>]*disabled=\{busy \|\| writesBlocked\}/);
  assert.match(source, /data-testid="shipping-one-level-packaging-read-only"/);
});

test("TB-176 discloses provenance instead of implying a customer charge", () => {
  assert.match(ONE_LEVEL_PACKAGING_READ_ONLY_FA, /حالت مشاهده/);
  assert.match(ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA, /اعمال نمی‌شود/);
  assert.match(ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA, /سایهٔ عملیاتی Order پیش‌فرض خاموش است/);
  assert.match(ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA, /هیچ مبلغ، کرایه یا سهم تسویه‌ای را در Checkout تغییر نمی‌دهد/);

  assert.match(ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA, /کد گزینهٔ همین روش/);
  assert.match(ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA, /سیاست گزینهٔ دیگر/);

  assert.match(ONE_LEVEL_PACKAGING_LEGACY_ROW_FA, /سطح‌خدمت/);
  assert.match(ONE_LEVEL_PACKAGING_LEGACY_ROW_FA, /fallback مستند/);
  assert.match(ONE_LEVEL_PACKAGING_LEGACY_ROW_FA, /تغییر نمی‌دهد/);
});

test("tab 3 renders no option picker, no provider hierarchy and no method-code splitting", () => {
  const source = fs
    .readFileSync(new URL("./shipping-one-level-packaging-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  // هیچ انتخابگر گزینهٔ سطح دوم و هیچ گروه‌بندی خدمت والد.
  assert.equal(/createOptionCode|SERVICE_LEVEL_PACKAGING_VALUE/.test(source), false);
  assert.equal(/groupPackagingByOption|packagingOptionLabelFa/.test(source), false);
  assert.equal(/loadShippingMethodTree/.test(source), false);
  assert.equal(/providerKind/.test(source), false);
  // هویت گزینه/خدمت فقط از پیوند سرور می‌آید.
  assert.match(source, /packagingWriteTarget\(resolvedBinding\)/);
  assert.match(source, /optionCode: target\.optionCode/);
  assert.match(source, /optionCode: policy\.optionCode/);
  // هیچ شکستن کد روش و هیچ ادعای لغو/تسویه در این کامپوننت نیست.
  assert.equal(/methodCode\.split/.test(source), false);
  assert.equal(/\.split\(":"\)/.test(source), false);
  assert.equal(/settle|Settlement|Checkout/.test(source), false);
});
