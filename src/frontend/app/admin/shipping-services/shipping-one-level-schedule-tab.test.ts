import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  configuredCapacityFa,
  inactiveScheduleCountFa,
  ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA,
  ONE_LEVEL_CAPACITY_ENFORCEMENT_FA,
  ONE_LEVEL_INACTIVE_SCHEDULE_FA,
  ONE_LEVEL_NO_SCHEDULE_FA,
  ONE_LEVEL_SCHEDULE_AMBIGUOUS_FA,
  ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA,
  ONE_LEVEL_SCHEDULE_IDENTITY_ISOLATION_FA,
  ONE_LEVEL_SCHEDULE_INACTIVE_FA,
  ONE_LEVEL_SCHEDULE_READ_ONLY_FA,
  ONE_LEVEL_SCHEDULE_RESTRICTED_EMPTY_FA,
  ONE_LEVEL_SCHEDULE_RESTRICTED_FA,
  ONE_LEVEL_SCHEDULE_UNRESTRICTED_FA,
  ONE_LEVEL_WINDOW_SCOPE_FA,
  operationStillTargetsMethod,
  resolveScheduleCoverage,
  resolveScheduleState,
  scheduleBelongsToBinding,
  scheduleCoverageExplanationFa,
  scheduleCoverageLabelFa,
  scheduleViewMatchesBindingService,
  scheduleWriteTarget,
  scopeSchedulesToBinding,
  type OneLevelBindingState,
  type OneLevelMethodBinding,
} from "./shipping-one-level-schedule-model.ts";
import type { ShippingOperatingScheduleAdminItem } from "./shipping-operating-schedule-api.ts";

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ONE-LEVEL-TABS-088 — tab 4 «برنامه کاری و ظرفیت».
 *
 * The gate is OFF and the one-level tables are unmigrated, so there is no live path to assert. These
 * tests cover the decisions this tab makes: the binding is never guessed, only the selected method's
 * own schedule identity is read or written, and configured capacity is never presented as an
 * enforced or reserved quantity.
 * ------------------------------------------------------------------------------------------- */

const POST_STANDARD: OneLevelMethodBinding = {
  methodCode: "post:standard",
  serviceCode: "post",
  optionCode: "standard",
  isActive: true,
};

function schedule(partial: Partial<ShippingOperatingScheduleAdminItem>): ShippingOperatingScheduleAdminItem {
  return {
    scheduleId: "s1",
    serviceCode: "post",
    optionCode: "standard",
    timeZoneId: "Asia/Tehran",
    isActive: true,
    windows: [],
    configuredCapacityTotal: null,
    createdAt: "2026-10-15T00:00:00Z",
    updatedAt: "2026-10-15T00:00:00Z",
    ...partial,
  };
}

test("only the selected method's own schedule identity is shown", () => {
  const rows = [
    schedule({ scheduleId: "mine", optionCode: "standard" }),
    schedule({ scheduleId: "other-option", optionCode: "express" }),
    schedule({ scheduleId: "service-level", optionCode: null }),
    schedule({ scheduleId: "other-service", serviceCode: "tipax" }),
  ];

  assert.deepEqual(scopeSchedulesToBinding(rows, POST_STANDARD).map((s) => s.scheduleId), ["mine"]);
  // برنامهٔ سطح خدمت زیر نام روش گزینه‌دار نمایش داده نمی‌شود؛ این همان «سطح دوم پنهان» است.
  assert.equal(scheduleBelongsToBinding(schedule({ optionCode: null }), POST_STANDARD), false);
  assert.equal(scheduleBelongsToBinding(schedule({ serviceCode: "tipax" }), POST_STANDARD), false);
  assert.equal(scheduleBelongsToBinding(schedule({ optionCode: "standard" }), POST_STANDARD), true);
});

test("an optionless method matches only the documented service-level schedule", () => {
  const storeCourier: OneLevelMethodBinding = {
    methodCode: "store_courier",
    serviceCode: "store_courier",
    optionCode: null,
    isActive: true,
  };
  const rows = [
    schedule({ scheduleId: "fallback", serviceCode: "store_courier", optionCode: null }),
    schedule({ scheduleId: "stray-option", serviceCode: "store_courier", optionCode: "express" }),
  ];

  assert.deepEqual(scopeSchedulesToBinding(rows, storeCourier).map((s) => s.scheduleId), ["fallback"]);
  assert.deepEqual(scopeSchedulesToBinding(null, storeCourier), []);
});

test("tipax:express never borrows tipax:standard and an unmapped method shows nothing", () => {
  const tipaxExpress: OneLevelMethodBinding = {
    methodCode: "tipax:express",
    serviceCode: "tipax",
    optionCode: "express",
    isActive: true,
  };
  const rows = [
    schedule({ scheduleId: "tipax-standard", serviceCode: "tipax", optionCode: "standard" }),
    schedule({ scheduleId: "tipax-express", serviceCode: "tipax", optionCode: "express" }),
  ];

  assert.deepEqual(scopeSchedulesToBinding(rows, tipaxExpress).map((s) => s.scheduleId), ["tipax-express"]);

  // روش بدون نگاشت (fail-closed): هیچ برنامه‌ای از هیچ خدمتی قرض گرفته نمی‌شود.
  assert.deepEqual(
    scopeSchedulesToBinding(rows, { methodCode: "in_person", serviceCode: "in_person", optionCode: null, isActive: false }),
    [],
  );
});

test("the write target always comes from the server binding and never from the method text", () => {
  assert.deepEqual(scheduleWriteTarget(POST_STANDARD), { serviceCode: "post", optionCode: "standard" });
  assert.deepEqual(
    scheduleWriteTarget({ methodCode: "tipax:express", serviceCode: "tipax", optionCode: "express", isActive: true }),
    { serviceCode: "tipax", optionCode: "express" },
  );
  assert.deepEqual(
    scheduleWriteTarget({ methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true }),
    { serviceCode: "store_courier", optionCode: null },
  );
});

test("a response belonging to another service is refused rather than rendered or edited", () => {
  assert.equal(scheduleViewMatchesBindingService({ serviceCode: "post" }, POST_STANDARD), true);
  assert.equal(scheduleViewMatchesBindingService({ serviceCode: "tipax" }, POST_STANDARD), false);
  assert.equal(scheduleViewMatchesBindingService(null, POST_STANDARD), false);
});

test("the tab never reports ready without a resolved binding", () => {
  const base = {
    hasMethod: true,
    bindingLoading: false,
    bindingResolved: true,
    bindingKnown: true,
    schedulesLoading: false,
    schedulesState: "ok" as const,
  };

  assert.equal(resolveScheduleState({ ...base, hasMethod: false }), "no-method");
  assert.equal(resolveScheduleState({ ...base, bindingKnown: false }), "binding-loading");
  assert.equal(resolveScheduleState({ ...base, bindingLoading: true }), "binding-loading");
  assert.equal(resolveScheduleState({ ...base, bindingResolved: false }), "binding-unavailable");
  assert.equal(resolveScheduleState({ ...base, schedulesState: null }), "loading");
  assert.equal(resolveScheduleState({ ...base, schedulesState: "denied" }), "denied");
  assert.equal(resolveScheduleState({ ...base, schedulesState: "error" }), "error");
  assert.equal(resolveScheduleState(base), "ready");

  // هر حالت غیرحل‌شدهٔ پیوند هرگز ready نمی‌شود، حتی اگر برنامه‌ها «موفق» خوانده شده باشند.
  for (const bindingState of [
    { state: "gate-off" as const },
    { state: "not-ready" as const },
    { state: "unresolved" as const },
    { state: "untrusted" as const },
  ] as OneLevelBindingState[]) {
    assert.equal(
      resolveScheduleState({ ...base, bindingResolved: bindingState.state === "resolved" }),
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

test("configured capacity is labeled as configuration that is not enforced", () => {
  assert.equal(ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA, "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)");
  assert.match(ONE_LEVEL_CAPACITY_ENFORCEMENT_FA, /NotEnforced/);

  assert.equal(configuredCapacityFa(null), `${ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA}: تعیین‌نشده`);
  assert.match(configuredCapacityFa(500), /ظرفیت تنظیم‌شده \(هنوز اعمال نمی‌شود\)/);
  assert.match(configuredCapacityFa(500), /۵۰۰/);

  // هیچ‌جا ادعای «باقی‌مانده» یا «رزرو» نمی‌شود.
  for (const text of [ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA, ONE_LEVEL_CAPACITY_ENFORCEMENT_FA, configuredCapacityFa(1)]) {
    assert.equal(/باقی‌مانده|رزرو شد|رزرو‌شده/.test(text), false);
  }
});

test("carrier windows are dispatch availability, not a promised delivery", () => {
  assert.match(ONE_LEVEL_WINDOW_SCOPE_FA, /تحویل به حامل/);
  assert.match(ONE_LEVEL_WINDOW_SCOPE_FA, /نه زمان تحویل وعده‌داده‌شده به مشتری/);
});

test("the empty and inactive states are stated truthfully", () => {
  assert.match(ONE_LEVEL_NO_SCHEDULE_FA, /هیچ ساعت صریحی/);
  assert.match(ONE_LEVEL_INACTIVE_SCHEDULE_FA, /غیرفعال/);
  assert.equal(inactiveScheduleCountFa([schedule({ isActive: true }), schedule({ scheduleId: "s2", isActive: false })]), "برنامه‌های غیرفعال: ۱");
});

test("TB-175 coverage state mirrors the owner evaluator, including restricted-empty", () => {
  const withWindow = schedule({
    windows: [{
      windowId: "w1",
      weekday: 1,
      startTime: "09:00:00",
      endTime: "12:00:00",
      capacity: null,
    }],
  });

  // نبود ردیف = هیچ ساعت صریحی؛ نه ادعای در دسترس بودن.
  assert.equal(resolveScheduleCoverage([]), "unrestricted");
  assert.equal(scheduleCoverageLabelFa("unrestricted"), "بدون محدودیت زمانی صریح (پیش‌فرض)");
  assert.match(ONE_LEVEL_SCHEDULE_UNRESTRICTED_FA, /تضمین در دسترس بودن حامل/);

  // فقط ردیف غیرفعال: طبق قرارداد مالک محدودیتی اعمال نمی‌شود.
  assert.equal(resolveScheduleCoverage([schedule({ isActive: false })]), "inactive-only");
  assert.match(ONE_LEVEL_SCHEDULE_INACTIVE_FA, /محدودیتی اعمال نمی‌کند/);

  // دو برنامهٔ فعال هم‌هویت: ابهام داده، نه انتخاب «اولی».
  assert.equal(resolveScheduleCoverage([withWindow, schedule({ scheduleId: "s2" })]), "ambiguous");
  assert.match(ONE_LEVEL_SCHEDULE_AMBIGUOUS_FA, /نقص داده/);

  // فعال بدون بازه = محدود به هیچ بازه؛ هرگز «بدون محدودیت» خوانده نمی‌شود.
  assert.equal(resolveScheduleCoverage([schedule({ windows: [] })]), "restricted-empty");
  assert.match(ONE_LEVEL_SCHEDULE_RESTRICTED_EMPTY_FA, /هرگز به‌طور خودکار «بدون محدودیت» خوانده نمی‌شود/);

  assert.equal(resolveScheduleCoverage([withWindow]), "restricted");
  assert.match(ONE_LEVEL_SCHEDULE_RESTRICTED_FA, /خارج از آن‌ها/);

  // توضیح هر وضعیت همان متن وضعیت خودش است، نه متن یک وضعیت دیگر.
  for (const coverage of ["unrestricted", "inactive-only", "ambiguous", "restricted-empty", "restricted"] as const) {
    assert.equal(scheduleCoverageExplanationFa(coverage), {
      unrestricted: ONE_LEVEL_SCHEDULE_UNRESTRICTED_FA,
      "inactive-only": ONE_LEVEL_SCHEDULE_INACTIVE_FA,
      ambiguous: ONE_LEVEL_SCHEDULE_AMBIGUOUS_FA,
      "restricted-empty": ONE_LEVEL_SCHEDULE_RESTRICTED_EMPTY_FA,
      restricted: ONE_LEVEL_SCHEDULE_RESTRICTED_FA,
    }[coverage]);
  }
});

test("TB-175 states honestly that Admin schedule edits do not change customer promises today", () => {
  assert.match(ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA, /اعمال نمی‌شوند/);
  assert.match(ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA, /سایهٔ عملیاتی پیش‌فرض خاموش است/);
  assert.match(ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA, /هیچ وعدهٔ تحویلی به مشتری را تغییر نمی‌دهد/);
  // هیچ ادعای رزرو، ظرفیت باقی‌مانده یا نوبت تضمین‌شده در متن اثر مشتری نیست.
  assert.equal(/رزرو|باقی‌مانده|تضمین نوبت/.test(ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA), false);
  assert.match(ONE_LEVEL_SCHEDULE_IDENTITY_ISOLATION_FA, /سطح خدمت/);
  assert.match(ONE_LEVEL_SCHEDULE_IDENTITY_ISOLATION_FA, /گزینهٔ دیگر/);
});

test("TB-175 View mode cannot write the schedule", () => {
  const source = fs
    .readFileSync(new URL("./shipping-one-level-schedule-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  // پرچم فقط-خواندنی از دیالوگ می‌آید و همهٔ مسیرهای نوشتن را می‌بندد.
  assert.match(source, /readOnly\?: boolean/);
  assert.match(source, /readOnly = false/);
  assert.match(source, /const writesBlocked = readOnly === true/);
  assert.match(source, /if \(writesBlocked\)/);
  assert.match(source, /disabled=\{busy \|\| writesBlocked\}/);
  assert.match(source, /data-testid="shipping-one-level-schedule-read-only"/);
  assert.match(source, /ONE_LEVEL_SCHEDULE_READ_ONLY_FA/);

  const dialog = fs.readFileSync(new URL("./flat-method-detail-dialog.tsx", import.meta.url), "utf8");
  assert.match(dialog, /<ShippingOneLevelScheduleTab[^>]*readOnly=\{readOnly\}/);
});

test("tab 4 renders no option picker, no provider hierarchy and no method-code splitting", () => {  const source = fs
    .readFileSync(new URL("./shipping-one-level-schedule-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  assert.equal(/SERVICE_LEVEL_SCHEDULE_VALUE|loadShippingMethodTree|scheduleOptionLabelFa/.test(source), false);
  assert.equal(/providerKind/.test(source), false);
  assert.match(source, /scheduleWriteTarget\(resolvedBinding\)/);
  assert.match(source, /optionCode: target\.optionCode/);
  assert.equal(/methodCode\.split/.test(source), false);
  assert.equal(/\.split\(":"\)/.test(source), false);
  // هیچ ادعای رزرو اتمی، تسویه یا تغییر Checkout در این کامپوننت نیست.
  assert.equal(/reserve|reservation|settle|Settlement|Checkout/.test(source), false);
});
