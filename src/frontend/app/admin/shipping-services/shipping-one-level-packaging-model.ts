/**
 * TB-SHIPPING-ONE-LEVEL-TABS-087 — pure model for tab 3 «بسته‌بندی و کرایه» of the one-level shell.
 *
 * This file adds only the *scoping* layer. The money/validation/payer-mode semantics are reused
 * unchanged from the proven legacy packaging model (`shipping-packaging-model.ts`) and the
 * method→binding authority is reused unchanged from 086 (`shipping-one-level-rates-model.ts`):
 * a one-level method code is never split in the browser, and the legacy service/option pair always
 * comes from the server resolver.
 *
 * The one rule this layer enforces: a packaging policy is shown or written **only** when it belongs
 * to the selected method's resolved binding. A service-level policy (`optionCode = null`) is the
 * documented fallback for optionless methods; it is never presented as a second selectable level,
 * and it is never silently edited as if it were the option-specific policy of another method.
 */
import type { ShippingPackagingPolicyAdminItem } from "./shipping-packaging-api.ts";
import type { OneLevelMethodBinding } from "./shipping-one-level-rates-model.ts";

export type {
  OneLevelMethodBinding,
  OneLevelBindingState,
} from "./shipping-one-level-rates-model.ts";
export {
  bindingContextFa,
  bindingUnavailableFa,
  createBindingGenerationGuard,
  mapHostBinding,
  oneLevelBindingPath,
} from "./shipping-one-level-rates-model.ts";

/**
 * آیا این سیاست بسته‌بندی به پیوند انتخاب‌شده تعلق دارد؟
 *
 * تطبیق کاملاً صریح است: `serviceCode` باید دقیقاً برابر باشد و `optionCode` باید دقیقاً برابر
 * باشد (تهی با تهی). هیچ fallback ضمنی به «همهٔ سیاست‌های خدمت» انجام نمی‌شود، چون نمایش یا
 * ویرایش سیاست گزینهٔ دیگر زیر نام این روش، همان خطای «سطح دوم پنهان» است.
 */
export function packagingBelongsToBinding(
  policy: Pick<ShippingPackagingPolicyAdminItem, "serviceCode" | "optionCode">,
  binding: OneLevelMethodBinding,
): boolean {
  if (policy.serviceCode !== binding.serviceCode) return false;
  return (policy.optionCode ?? null) === binding.optionCode;
}

/** سیاست‌های بسته‌بندی محدودشده به همان روش یک‌سطحی انتخاب‌شده. */
export function scopePackagingToBinding(
  policies: readonly ShippingPackagingPolicyAdminItem[] | null,
  binding: OneLevelMethodBinding,
): ShippingPackagingPolicyAdminItem[] {
  if (!policies) return [];
  return policies.filter((policy) => packagingBelongsToBinding(policy, binding));
}

/**
 * آیا پاسخ بسته‌بندی واقعاً مربوط به همین خدمت است؟ پاسخ اشتباه/جابجا هرگز رندر نمی‌شود و
 * هرگز مبنای نوشتن قرار نمی‌گیرد، چون سیاست خدمت دیگری نباید زیر نام این روش دیده شود.
 */
export function packagingViewMatchesBindingService(
  view: { serviceCode: string } | null,
  binding: OneLevelMethodBinding,
): boolean {
  return view != null && view.serviceCode === binding.serviceCode;
}

/**
 * وضعیت قابل‌نمایش زبانهٔ بسته‌بندی و کرایه. هیچ حالت «موفقِ خالی» جعلی وجود ندارد:
 * نبود سیاست یک وضعیت واقعی (`ready`) است، ولی نبود پیوند هرگز `ready` نمی‌شود.
 */
export type OneLevelPackagingState =
  | "no-method"
  | "binding-loading"
  | "binding-unavailable"
  | "loading"
  | "denied"
  | "error"
  | "ready";

export function resolvePackagingState(input: {
  hasMethod: boolean;
  bindingLoading: boolean;
  bindingResolved: boolean;
  bindingKnown: boolean;
  policiesLoading: boolean;
  policiesState: "ok" | "denied" | "error" | null;
}): OneLevelPackagingState {
  if (!input.hasMethod) return "no-method";
  if (input.bindingLoading || !input.bindingKnown) return "binding-loading";
  if (!input.bindingResolved) return "binding-unavailable";
  if (input.policiesLoading || input.policiesState === null) return "loading";
  if (input.policiesState === "denied") return "denied";
  if (input.policiesState === "error") return "error";
  return "ready";
}

/**
 * مقصد نوشتن یک عملیات روی روش انتخاب‌شده.
 *
 * این تابع عمداً تنها راه ساخت `serviceCode`/`optionCode` برای فراخوانی API است: هر دو از پیوند
 * حل‌شدهٔ سرور می‌آیند و ورودی کاربر هرگز در آن‌ها دخالت ندارد.
 */
export function packagingWriteTarget(binding: OneLevelMethodBinding): {
  serviceCode: string;
  optionCode: string | null;
} {
  return { serviceCode: binding.serviceCode, optionCode: binding.optionCode };
}

/**
 * آیا عملیاتِ در راه هنوز به همان روش تعلق دارد؟ اگر کاربر وسط عملیات روش را عوض کرده باشد،
 * پاسخ نباید به روش جدید نسبت داده شود. (این تابع ادعای لغو یک درخواست شبکه‌ای را ندارد؛
 * فقط از نسبت‌دادن نتیجهٔ روش قبلی به روش جدید جلوگیری می‌کند.)
 */
export function operationStillTargetsMethod(
  operationMethodCode: string | null,
  currentMethodCode: string,
): boolean {
  return operationMethodCode != null && operationMethodCode === currentMethodCode;
}

/** برچسب فارسی وضعیت خالی زبانه، شامل معنای «صفر صریح» از مدل قدیمی. */
export const ONE_LEVEL_NO_PACKAGING_FA =
  "برای این روش سیاست بسته‌بندی ثبت نشده است؛ نبود سیاست یعنی هزینهٔ بسته‌بندی صفر و طرف پرداخت کرایه پیش‌کرایهٔ پیش‌فرض است.";

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ADMIN-METHOD-PACKAGING-176 — read-only guard and honest scope disclosures.
 * ------------------------------------------------------------------------------------------- */

/** حالت مشاهده: هیچ نوشتنی مجاز نیست (قبلاً زبانهٔ بسته‌بندی از دیالوگ مشاهده قابل نوشتن بود). */
export const ONE_LEVEL_PACKAGING_READ_ONLY_FA =
  "این روش در حالت مشاهده است؛ سیاست بسته‌بندی فقط خواندنی است و هیچ افزودن، ویرایش، ذخیره یا فعال‌سازی مجاز نیست.";

/**
 * تنها منبع تصمیم «آیا نوشتن مجاز است» برای این زبانه.
 *
 * یک تابع خالص تا هر مسیر نوشتن (افزودن، ویرایش، ذخیره، فعال‌سازی) و هر کنترل UI از همان یک
 * قاعده استفاده کند؛ نمی‌توان یک مسیر را جا انداخت و بقیه را بست.
 */
export function packagingWritesAllowed(readOnly: boolean | undefined): boolean {
  return readOnly !== true;
}

/**
 * اثر واقعی امروزِ سیاست بسته‌بندی بر مشتری.
 *
 * از ردیابی کد: `ShippingPackagingFeeResolver` فقط در ارزیابی عملیاتی Shipping مصرف می‌شود و
 * ناظر عملیاتی Order پیش‌فرض خاموش است؛ هیچ مسیری در Checkout این هزینه را به جمع مشتری اضافه
 * نمی‌کند. پس ادعای «تغییر مبلغ مشتری» ممنوع است.
 */
export const ONE_LEVEL_PACKAGING_CUSTOMER_EFFECT_FA =
  "این هزینه امروز در مبلغ پرداختی مشتری اعمال نمی‌شود: حل‌کنندهٔ بسته‌بندی فقط در ارزیابی عملیاتی Shipping استفاده می‌شود و سایهٔ عملیاتی Order پیش‌فرض خاموش است. ذخیرهٔ این سیاست هیچ مبلغ، کرایه یا سهم تسویه‌ای را در Checkout تغییر نمی‌دهد.";

/** مرز هویت: فقط ردیف همین (خدمت + گزینه) خوانده و نوشته می‌شود. */
export const ONE_LEVEL_PACKAGING_IDENTITY_ISOLATION_FA =
  "فقط سیاست همین هویت (کد خدمت + کد گزینهٔ همین روش) خوانده و نوشته می‌شود؛ سیاست گزینهٔ دیگر (مثلاً خواهر همان خدمت) نه نمایش داده می‌شود و نه تغییر می‌کند.";

/**
 * تفکیک ردیف سطح‌خدمت از ردیف همین روش.
 *
 * ردیف `optionCode = null` یک ردیف legacy/سطح‌خدمت است. حل‌کنندهٔ ارزیابی آن را به‌عنوان fallback
 * مستند برای روش گزینه‌دار به کار می‌برد، ولی این زبانه آن را به‌عنوان «سیاست این روش» نمایش یا
 * ویرایش نمی‌کند؛ بازنویسی آن یک تغییر در سطح کل خدمت است، نه این روش.
 */
export const ONE_LEVEL_PACKAGING_LEGACY_ROW_FA =
  "ردیف سطح‌خدمت (بدون کد گزینه) یک سیاست legacy است: در ارزیابی عملیاتی به‌عنوان fallback مستند همین خدمت به کار می‌رود، ولی این زبانه آن را به‌عنوان سیاست این روش نشان نمی‌دهد و تغییر نمی‌دهد؛ ویرایش آن یک تغییر سطح‌خدمت است، نه این روش.";
