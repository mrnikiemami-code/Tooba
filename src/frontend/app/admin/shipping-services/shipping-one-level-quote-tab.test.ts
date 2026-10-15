import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_CAPACITY_FA,
  ONE_LEVEL_QUOTE_INPUT_BOUNDARY_FA,
  ONE_LEVEL_QUOTE_READ_ONLY_FA,
  oneLevelQuotePayload,
  operationStillTargetsMethod,
  quoteMatchesRequestedIdentity,
  quoteTarget,
  resolveQuoteState,
  type OneLevelBindingState,
  type OneLevelMethodBinding,
} from "./shipping-one-level-quote-model.ts";
import { emptyQuoteForm } from "./shipping-operational-quote-model.ts";

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ONE-LEVEL-TABS-089 — tab 5 «بررسی هزینه».
 *
 * The gate is OFF and the one-level tables are unmigrated, so no live evaluation can be asserted.
 * These tests cover the decisions this tab makes: the option identity always comes from the server
 * binding, a reply belonging to another identity is refused, a stale evaluation is never shown under
 * a newly selected method, and the diagnostic boundary is stated explicitly.
 * ------------------------------------------------------------------------------------------- */

const POST_STANDARD: OneLevelMethodBinding = {
  methodCode: "post:standard",
  serviceCode: "post",
  optionCode: "standard",
  isActive: true,
};

const SCOPE = { countryId: "ir", administrativeAreaId: null, cityId: null, serviceAreaId: null };

test("the option identity of the evaluation comes only from the server binding", () => {
  const form = { ...emptyQuoteForm(), cartSubtotal: "500000", optionCode: "express" };

  // حتی اگر فرم کد گزینهٔ دیگری حمل کند، بدنهٔ ارسال فقط گزینهٔ پیوند را می‌فرستد.
  assert.equal(oneLevelQuotePayload(form, SCOPE, POST_STANDARD).optionCode, "standard");
  assert.equal(
    oneLevelQuotePayload(form, SCOPE, { methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true }).optionCode,
    null,
  );
  assert.equal(
    oneLevelQuotePayload(form, SCOPE, { methodCode: "tipax:express", serviceCode: "tipax", optionCode: "express", isActive: true }).optionCode,
    "express",
  );
});

test("the evaluation target always comes from the server binding", () => {
  assert.deepEqual(quoteTarget(POST_STANDARD), { serviceCode: "post", optionCode: "standard" });
  assert.deepEqual(
    quoteTarget({ methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true }),
    { serviceCode: "store_courier", optionCode: null },
  );
});

test("a reply belonging to another identity is refused instead of displayed", () => {
  assert.equal(quoteMatchesRequestedIdentity({ serviceCode: "post", optionCode: "standard" }, POST_STANDARD), true);
  // سرویس دیگر
  assert.equal(quoteMatchesRequestedIdentity({ serviceCode: "tipax", optionCode: "standard" }, POST_STANDARD), false);
  // گزینهٔ دیگر همان خدمت
  assert.equal(quoteMatchesRequestedIdentity({ serviceCode: "post", optionCode: "express" }, POST_STANDARD), false);
  // سطح خدمت در برابر گزینهٔ اختصاصی
  assert.equal(quoteMatchesRequestedIdentity({ serviceCode: "post", optionCode: null }, POST_STANDARD), false);
  // گزینه‌نداشتن با گزینه‌نداشتن تطبیق می‌کند
  assert.equal(
    quoteMatchesRequestedIdentity(
      { serviceCode: "store_courier", optionCode: null },
      { methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true },
    ),
    true,
  );
  assert.equal(quoteMatchesRequestedIdentity(null, POST_STANDARD), false);
});

test("an unmapped or inactive method has no binding to evaluate against", () => {
  // پیوند حل‌نشده ⇒ هیچ ارزیابی؛ هیچ گزینه‌ای حدس زده نمی‌شود.
  const states: OneLevelBindingState[] = [
    { state: "gate-off" },
    { state: "not-ready" },
    { state: "unresolved" },
    { state: "untrusted" },
  ];
  for (const binding of states) {
    assert.equal(
      resolveQuoteState({ hasMethod: true, bindingLoading: false, bindingResolved: binding.state === "resolved", bindingKnown: true }),
      "binding-unavailable",
      `${binding.state} must never be ready`,
    );
  }
  assert.equal(resolveQuoteState({ hasMethod: false, bindingLoading: false, bindingResolved: false, bindingKnown: false }), "no-method");
  assert.equal(resolveQuoteState({ hasMethod: true, bindingLoading: true, bindingResolved: false, bindingKnown: false }), "binding-loading");
  assert.equal(resolveQuoteState({ hasMethod: true, bindingLoading: false, bindingResolved: true, bindingKnown: true }), "ready");
});

test("a stale evaluation is never attributed to the newly selected method", () => {
  assert.equal(operationStillTargetsMethod("post:standard", "post:standard"), true);
  assert.equal(operationStillTargetsMethod("post:standard", "tipax:express"), false);
  assert.equal(operationStillTargetsMethod(null, "post:standard"), false);
});

test("the diagnostic boundary is stated explicitly and never claims a charge or settlement", () => {
  assert.equal(ONE_LEVEL_QUOTE_READ_ONLY_FA, "فقط بررسی هزینه؛ مبلغ سفارش یا پرداخت تغییر نمی‌کند.");
  assert.match(ONE_LEVEL_QUOTE_BOUNDARY_FA, /تشخیص خواندنی/);
  assert.match(ONE_LEVEL_QUOTE_BOUNDARY_FA, /هیچ سفارشی نوشته نمی‌شود/);
  assert.match(ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA, /نه مبلغی که از مشتری گرفته می‌شود/);
  assert.match(ONE_LEVEL_QUOTE_INPUT_BOUNDARY_FA, /اندازه‌گیری واقعی سفارش نیستند/);
  assert.equal(ONE_LEVEL_QUOTE_CAPACITY_FA, "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)");

  // هیچ ادعای وصول/تسویهٔ انجام‌شده یا رزرو در متن‌های مرزی نیست. متن‌ها فقط *نفی* می‌کنند
  // («نه مبلغی که ... وصول یا تسویه شده است»)، پس ادعای مثبت جست‌وجو می‌شود نه ریشهٔ واژه.
  for (const text of [ONE_LEVEL_QUOTE_READ_ONLY_FA, ONE_LEVEL_QUOTE_BOUNDARY_FA, ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA]) {
    assert.equal(/(وصول|تسویه|رزرو) شد\./.test(text), false, text);
    assert.equal(/(وصول|تسویه|رزرو) خواهد شد/.test(text), false, text);
    assert.equal(/مبلغ قابل پرداخت مشتری/.test(text), false, text);
  }
});

test("tab 5 renders no option picker, no provider hierarchy and no method-code splitting", () => {
  const source = fs
    .readFileSync(new URL("./shipping-one-level-quote-tab.tsx", import.meta.url), "utf8")
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/.*$/gm, "");

  assert.equal(/SERVICE_LEVEL_OPTION_VALUE|loadShippingMethodTree|shipping-quote-option/.test(source), false);
  assert.equal(/providerKind/.test(source), false);
  assert.match(source, /oneLevelQuotePayload\(form, buildScopeWrite\(selection\), target\)/);
  assert.equal(/methodCode\.split/.test(source), false);
  assert.equal(/\.split\(":"\)/.test(source), false);
  // هیچ ادعای نوشتن سفارش/پرداخت/تسویه یا محاسبهٔ دوبارهٔ قیمت در این کامپوننت نیست.
  assert.equal(/settle|Settlement|createPayment|updateOrder/.test(source), false);
});

test("tabs 1-4 remain intact and the shell still exposes the locked five labels", () => {
  const shell = fs.readFileSync(new URL("./shipping-one-level-tabs.tsx", import.meta.url), "utf8");
  assert.match(shell, /ShippingOneLevelRatesTab/);
  assert.match(shell, /ShippingOneLevelPackagingTab/);
  assert.match(shell, /ShippingOneLevelScheduleTab/);
  assert.match(shell, /ShippingOneLevelQuoteTab/);
  assert.match(shell, /role="tablist"/);
  assert.match(shell, /dir="rtl"/);
});
