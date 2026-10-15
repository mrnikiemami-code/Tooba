/**
 * TB-SHIPPING-ONE-LEVEL-TABS-089 — pure model for tab 5 «بررسی هزینه» of the one-level shell.
 *
 * The quotation itself is NOT re-implemented here: money parsing, grams conversion, the local-time →
 * UTC resolution (including the refusal of ambiguous/nonexistent local times), currency
 * normalization, every Persian label and every failure/reason code map come unchanged from the
 * proven 078 `shipping-operational-quote-model.ts`.
 *
 * What this file adds is the *one-level scoping* layer:
 *
 * - the evaluation target is the selected method's server-resolved legacy identity, and the
 *   `optionCode` sent to the endpoint comes only from that binding (including `null` for optionless
 *   methods) — never from a client option selector and never from splitting `methodCode`;
 * - the answer is checked against the identity that was requested, so a reply belonging to another
 *   service/option is refused instead of displayed;
 * - the diagnostic boundary is stated explicitly: this is a read-only inspection, not a customer
 *   charge, not a COD settlement, not a delivery promise and not a Checkout cutover.
 */
import type {
  ShippingOperationalQuoteEvaluationRequest,
  ShippingOperationalQuoteResult,
} from "./shipping-operational-quote-api.ts";
import type {
  ShippingOperationalQuoteFormValues,
} from "./shipping-operational-quote-model.ts";
import { quoteFormToPayload } from "./shipping-operational-quote-model.ts";
import type { OneLevelMethodBinding } from "./shipping-one-level-rates-model.ts";

export type { OneLevelMethodBinding, OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
export {
  bindingContextFa,
  bindingUnavailableFa,
  createBindingGenerationGuard,
  mapHostBinding,
  oneLevelBindingPath,
} from "./shipping-one-level-rates-model.ts";

/**
 * بدنهٔ ارزیابی برای یک روش یک‌سطحی.
 *
 * `optionCode` عمداً از `form` خوانده نمی‌شود: مقدار فرم بازنویسی می‌شود تا هویت گزینه فقط از پیوند
 * حل‌شدهٔ سرور بیاید. هیچ ورودی کاربر نمی‌تواند ارزیابی را به سرویس/گزینهٔ دیگری ببرد.
 */
export function oneLevelQuotePayload(
  form: ShippingOperationalQuoteFormValues,
  scope: {
    countryId: string;
    administrativeAreaId: string | null;
    cityId: string | null;
    serviceAreaId: string | null;
  },
  binding: OneLevelMethodBinding,
): ShippingOperationalQuoteEvaluationRequest {
  return quoteFormToPayload({ ...form, optionCode: binding.optionCode }, scope);
}

/**
 * آیا پاسخ برگشتی همان هویتی است که درخواست شده بود؟
 *
 * پاسخ ناهم‌خوان هرگز نمایش داده نمی‌شود، چون نمایش عدد سرویس دیگر زیر نام این روش، دقیقاً همان
 * خطای «مبنای اشتباه» است. `optionCode` تهی با تهی مقایسه می‌شود.
 */
export function quoteMatchesRequestedIdentity(
  result: Pick<ShippingOperationalQuoteResult, "serviceCode" | "optionCode"> | null,
  binding: OneLevelMethodBinding,
): boolean {
  if (!result) return false;
  if (result.serviceCode !== binding.serviceCode) return false;
  return (result.optionCode ?? null) === binding.optionCode;
}

/** مقصد ارزیابی؛ تنها راه ساخت `serviceCode`/`optionCode` برای فراخوانی API. */
export function quoteTarget(binding: OneLevelMethodBinding): {
  serviceCode: string;
  optionCode: string | null;
} {
  return { serviceCode: binding.serviceCode, optionCode: binding.optionCode };
}

/** آیا عملیاتِ در راه هنوز به همان روش تعلق دارد (پاسخ روش قبلی به روش جدید نسبت داده نمی‌شود). */
export function operationStillTargetsMethod(
  operationMethodCode: string | null,
  currentMethodCode: string,
): boolean {
  return operationMethodCode != null && operationMethodCode === currentMethodCode;
}

/** وضعیت قابل‌نمایش زبانهٔ بررسی هزینه. */
export type OneLevelQuoteState =
  | "no-method"
  | "binding-loading"
  | "binding-unavailable"
  | "ready";

export function resolveQuoteState(input: {
  hasMethod: boolean;
  bindingLoading: boolean;
  bindingResolved: boolean;
  bindingKnown: boolean;
}): OneLevelQuoteState {
  if (!input.hasMethod) return "no-method";
  if (input.bindingLoading || !input.bindingKnown) return "binding-loading";
  if (!input.bindingResolved) return "binding-unavailable";
  return "ready";
}

/**
 * مرز صریح این زبانه. این متن باید همیشه دیده شود، حتی وقتی ارزیابی موفق است: خروجی یک تشخیص
 * درون‌ادمین است و هیچ سفارش، پرداخت، وصول یا ظرفیتی را تغییر نمی‌دهد.
 */
export const ONE_LEVEL_QUOTE_READ_ONLY_FA =
  "فقط بررسی هزینه؛ مبلغ سفارش یا پرداخت تغییر نمی‌کند.";

/** مرز تفصیلی: نه قیمت مشتری، نه پرداخت، نه وصول، نه کوت‌اور Checkout. */
export const ONE_LEVEL_QUOTE_BOUNDARY_FA =
  "این یک تشخیص خواندنی است، نه قیمت مشتری و نه تراکنش: هیچ سفارشی نوشته نمی‌شود، هیچ پرداخت یا وصول پس‌کرایه‌ای ساخته نمی‌شود و Checkout روی مرجع قیمت قدیمی خود می‌ماند.";

/** مرز معنایی «قابل پرداخت هنگام تسویه» و «وصول هنگام تحویل» به‌عنوان تشخیص. */
export const ONE_LEVEL_QUOTE_AMOUNT_BOUNDARY_FA =
  "«قابل پرداخت هنگام تسویه» و «وصول هنگام تحویل» در این جدول فقط تفکیک تشخیصی همان ارزیابی‌اند؛ نه مبلغی که از مشتری گرفته می‌شود و نه مبلغی که وصول یا تسویه شده است.";

/** هشدار صریح دربارهٔ ورودی‌های تشخیصی. */
export const ONE_LEVEL_QUOTE_INPUT_BOUNDARY_FA =
  "وزن و تعداد اقلام در این فرم ورودی تشخیصی دستی است؛ این مقادیر اندازه‌گیری واقعی سفارش نیستند و از هیچ سفارشی خوانده نمی‌شوند.";

/** برچسب ظرفیت در نتیجهٔ ارزیابی؛ ظرفیت همیشه فقط پیکربندی است. */
export const ONE_LEVEL_QUOTE_CAPACITY_FA = "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)";
