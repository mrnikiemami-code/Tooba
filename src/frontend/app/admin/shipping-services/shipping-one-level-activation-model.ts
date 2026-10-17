/**
 * مدل ناب + کلاینت تنها درز فعال‌سازی روش یک‌سطحی — بدون وابستگی به React.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-097 · Parent: TB-SHIPPING-ONE-LEVEL-TABS-096
 *
 * قواعد سخت این فایل:
 *
 * - تنها endpoint مجاز همان درز ۰۹۶ است:
 *   `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/activation`
 *   با بدنهٔ صرفاً `{ isActive }`. هیچ endpoint دیگری ساخته نمی‌شود و هیچ
 *   `serviceCode`/`optionCode`/UUID گزینه‌ای از مرورگر فرستاده نمی‌شود.
 * - هویت روش همان `methodCode` canonical است و تغییرناپذیر می‌ماند.
 * - «گزینه‌دار بودن» فقط از پیوند حل‌شدهٔ سرور (`GET …/binding`) می‌آید: `optionCode` غیرتهی.
 *   هیچ استنتاج متنی از کد روش انجام نمی‌شود و روش بدون گزینه هرگز واجد شرایط فعال‌سازی نیست،
 *   چون فعال‌سازی آن یعنی وضعیت سطح سرویس که هنوز مرجع مجاز ندارد.
 * - هیچ «موفقیت جعلی» و هیچ حالت خوش‌بینانهٔ محلی وجود ندارد: وضعیت نمایش‌داده‌شده فقط از
 *   حقیقت سرور می‌آید و پس از پاسخ تأییدشده هم منبع حقیقت دوباره خوانده می‌شود.
 * - 401/403 ⇒ `denied`؛ خطای سرور ⇒ `error` با کد خطای واقعی؛ پاسخ نامعتبر ⇒ `error` (fail-closed).
 */

import { adminHeaders, type AdminResult } from "../admin-api.ts";
import { loadOneLevelBinding } from "./one-level-binding-client.ts";
import {
  createBindingGenerationGuard,
  oneLevelBindingPath,
  type OneLevelBindingState,
  type OneLevelMethodBinding,
} from "./shipping-one-level-rates-model.ts";
import { sanitizeMethodCode } from "./shipping-one-level-tabs-model.ts";

/** آخرین بخش مسیر فعال‌سازی در سرور؛ تنها عملیات نوشتن مجاز این ویژگی. */
export const ONE_LEVEL_METHOD_ACTIVATION_SEGMENT = "activation";

/** متن صریح روش بدون گزینه؛ هرگز به‌جای آن سوئیچ ساختگی نشان داده نمی‌شود. */
export const ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA =
  "فعال‌سازی این نوع سرویس هنوز پشتیبانی نمی‌شود.";

/** مسیر سرور برای فعال/غیرفعال‌سازی یک روش گزینه‌دار؛ کد روش تنها هویت است. */
export function oneLevelMethodActivationPath(methodCode: string): string {
  const code = sanitizeMethodCode(methodCode);
  return `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(code)}/${ONE_LEVEL_METHOD_ACTIVATION_SEGMENT}`;
}

/** مسیر سرور برای حل پیوند قدیمی؛ تنها منبع مجاز «گزینه‌دار بودن» است. */
export function oneLevelMethodBindingPath(methodCode: string): string {
  return oneLevelBindingPath(sanitizeMethodCode(methodCode));
}

/**
 * واجد شرایط بودن فعال‌سازی برای یک روش. حالت‌ها عمداً از هم جدا هستند تا «بدون گزینه» با
 * «پیوند ناشناخته» و «دروازهٔ خاموش» اشتباه نشود؛ هر حالت غیرموفق هیچ دکمهٔ عملیاتی ندارد.
 */
export type OneLevelActivationEligibility =
  | { state: "supported"; methodCode: string; currentIsActive: boolean }
  | { state: "optionless"; methodCode: string }
  | {
    state: "unavailable";
    reason: "no-method" | "gate-off" | "not-ready" | "unresolved" | "denied" | "unreachable" | "untrusted";
  };

/**
 * واجد شرایط بودن را فقط از پیوند حل‌شدهٔ سرور تعیین می‌کند.
 *
 * `optionCode` تهی یعنی روش به سرویس بدون گزینه نگاشته شده است ⇒ هیچ سوئیچ کاربردی مجاز نیست.
 * هر حالت غیرحل‌شدهٔ پیوند (دروازه خاموش، کاتالوگ مهاجرت‌نشده، روش ناشناخته، پاسخ نامعتبر) هم
 * fail-closed است، چون در آن حالت «گزینه‌دار بودن» قابل‌اثبات نیست.
 */
export function resolveActivationEligibility(
  bindingState: OneLevelBindingState | null,
  methodCode: string,
): OneLevelActivationEligibility {
  const code = sanitizeMethodCode(methodCode);
  if (!code) return { state: "unavailable", reason: "no-method" };
  if (!bindingState) return { state: "unavailable", reason: "unresolved" };
  if (bindingState.state !== "resolved") {
    return { state: "unavailable", reason: bindingState.state };
  }
  if (bindingState.binding.optionCode === null) {
    return { state: "optionless", methodCode: code };
  }
  return {
    state: "supported",
    methodCode: code,
    currentIsActive: bindingState.binding.isActive === true,
  };
}

/** پاسخ تأییدشدهٔ سرور برای فعال‌سازی یک روش. */
export type OneLevelActivationSaved = {
  methodCode: string;
  isActive: boolean;
  changed: boolean;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function text(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

function pick(row: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (row[key] !== undefined) return row[key];
  }
  return undefined;
}

/**
 * پاسخ سرور را محافظه‌کارانه نگاشت می‌کند.
 *
 * `isActive` باید boolean واقعی باشد و `methodCode` باید canonical و همان کد درخواست باشد؛
 * در غیر این‌صورت `null` برمی‌گردد تا «موفقیت جعلی» ساخته نشود.
 */
export function mapActivationResponse(
  payload: unknown,
  requestedMethodCode: string,
): OneLevelActivationSaved | null {
  const row = asRecord(payload);
  if (!row) return null;
  const methodCode = sanitizeMethodCode(text(pick(row, "methodCode", "MethodCode")));
  const requested = sanitizeMethodCode(requestedMethodCode);
  if (!methodCode || methodCode !== requested) return null;
  const rawActive = pick(row, "isActive", "IsActive");
  if (typeof rawActive !== "boolean") return null;
  return {
    methodCode,
    isActive: rawActive,
    changed: (pick(row, "changed", "Changed") ?? false) === true,
  };
}

function errorCode(payload: unknown, fallback: string): string {
  const row = asRecord(payload);
  return text(row?.errorCode ?? row?.ErrorCode ?? row?.title ?? row?.Title) || fallback;
}

/**
 * فعال/غیرفعال‌سازی یک روش یک‌سطحی گزینه‌دار.
 *
 * بدنه فقط `{ isActive }` است. هیچ هویت سرویس/گزینه‌ای ارسال نمی‌شود و هیچ حالت خوش‌بینانهٔ
 * محلی ساخته نمی‌شود: فقط پاسخ تأییدشدهٔ سرور برگردانده می‌شود.
 */
export async function setOneLevelMethodActivation(
  methodCode: string,
  isActive: boolean,
): Promise<AdminResult<OneLevelActivationSaved>> {
  const code = sanitizeMethodCode(methodCode);
  if (!code) {
    return { state: "error", data: null, status: 0, message: "shipping_method.code.invalid" };
  }
  try {
    const response = await fetch(oneLevelMethodActivationPath(code), {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ isActive: isActive === true }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCode(payload, "fulfillment.rejected"),
      };
    }
    const mapped = mapActivationResponse(payload, code);
    if (!mapped) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_method_activation.invalid_response",
      };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** پیوند قدیمی روش را فقط-خواندنی می‌خواند؛ تنها منبع مجاز «گزینه‌دار بودن». */
export async function loadOneLevelMethodBinding(
  methodCode: string,
): Promise<OneLevelBindingState> {
  // همان transport مشترک همهٔ زبانه‌ها؛ این تابع فقط نام سازگار قبلی را نگه می‌دارد.
  return loadOneLevelBinding(methodCode);
}

/** حالت فعلی به فارسی؛ حقیقت نمایش‌داده‌شده فقط از سرور می‌آید. */
export function activationStatusFa(isActive: boolean): string {
  return isActive ? "فعال" : "غیرفعال";
}

/** عنوان عملیات متناظر با وضعیت فعلی. */
export function activationActionFa(isActive: boolean): string {
  return isActive ? "غیرفعال‌سازی" : "فعال‌سازی";
}

/** اثر صریح عملیات بر واجد شرایط بودن روش در فروشگاه. */
export function activationImpactFa(isActive: boolean): string {
  return isActive
    ? "غیرفعال‌سازی، این روش را از فهرست روش‌های قابل انتخاب فروشگاه خارج می‌کند و واجد شرایط بودن آن را تغییر می‌دهد."
    : "فعال‌سازی، این روش را برای انتخاب مشتری در فروشگاه واجد شرایط می‌کند.";
}

/** پرسش تأیید پیش از عملیات؛ هم برای فعال‌سازی و هم برای غیرفعال‌سازی صریح است. */
export function activationConfirmFa(isActive: boolean): string {
  return isActive
    ? "این روش غیرفعال می‌شود، از فهرست روش‌های قابل انتخاب فروشگاه خارج می‌شود و اثر آن بر واجد شرایط بودن فروشگاه است. ادامه می‌دهید؟"
    : "این روش فعال می‌شود و در فروشگاه برای انتخاب مشتری واجد شرایط می‌شود. ادامه می‌دهید؟";
}

/** متن صریح حالت‌های غیرقابل‌استفاده؛ هیچ‌کدام به سوئیچ ساختگی تبدیل نمی‌شوند. */
export function activationUnavailableFa(
  reason: Extract<OneLevelActivationEligibility, { state: "unavailable" }>["reason"],
): string {
  switch (reason) {
    case "no-method":
      return "برای فعال‌سازی، ابتدا یک روش را از فهرست انتخاب کنید.";
    case "gate-off":
      return "دروازهٔ کاتالوگ یک‌سطحی خاموش است؛ فعال‌سازی در دسترس نیست.";
    case "not-ready":
      return "کاتالوگ یک‌سطحی هنوز مهاجرت نشده است؛ واجد شرایط بودن این روش قابل‌اثبات نیست.";
    case "unresolved":
      return "پیوند سرویس/گزینهٔ این روش هنوز خوانده نشده است؛ برای احتیاط هیچ سوئیچی نشان داده نمی‌شود.";
    case "denied":
      return "دسترسی برای خواندن پیوند این روش تأیید نشد؛ پس از رفع مشکل دسترسی دوباره تلاش کنید.";
    case "unreachable":
      return "ارتباط با سرور برای خواندن پیوند این روش برقرار نشد؛ اتصال و اجرای سرور را بررسی و دوباره تلاش کنید.";
    default:
      return "پاسخ سرور برای پیوند این روش معتبر نبود؛ برای احتیاط هیچ سوئیچی نشان داده نمی‌شود.";
  }
}

/** متن خطای فارسی بر پایهٔ کد خطای واقعی سرور؛ هیچ خطای ناشناخته‌ای پنهان نمی‌شود. */
export function activationErrorFa(errorCodeOrMessage: string): string {
  switch (errorCodeOrMessage) {
    case "fulfillment.rejected":
      return "فعال‌سازی این روش در حال حاضر مجاز نیست (دروازهٔ کاتالوگ خاموش است یا کاتالوگ مهاجرت نشده است).";
    case "shipping_method.not_found":
      return "این روش دیگر در کاتالوگ یک‌سطحی وجود ندارد؛ وضعیت دوباره از سرور خوانده می‌شود.";
    case "shipping_service.not_found":
      return "سرویس قدیمی متناظر با این روش پیدا نشد؛ پیوند این روش قابل‌اعتماد نیست.";
    case "shipping_method.code.invalid":
      return "کد روش نامعتبر است؛ هیچ درخواستی ارسال نشد.";
    case "fulfillment.shipping_method.unsupported":
      return "این عملیات برای این روش پشتیبانی نمی‌شود؛ فعال‌سازی سطح سرویس بدون گزینه هنوز مجاز نیست.";
    case "host-unreachable":
      return "ارتباط با سرور برقرار نشد؛ هیچ تغییری اعمال نشده است.";
    default:
      return "تغییر وضعیت فعال‌بودن این روش ناموفق بود؛ هیچ تغییری اعمال نشد.";
  }
}

/** کلید پایدار یک هدف فعال‌سازی؛ برای تشخیص «هدف عوض شده» در پاسخ‌های در پرواز. */
export function activationTargetKey(methodCode: string): string {
  return sanitizeMethodCode(methodCode);
}

/**
 * نگهبان نسل: اگر کاربر وسط یک عملیات روش را عوض کند، نتیجهٔ در راه باطل می‌شود تا موفقیت یک روش
 * هرگز به‌عنوان موفقیت روش دیگری نمایش داده نشود.
 */
export function createActivationGenerationGuard(): { begin: () => () => boolean } {
  return createBindingGenerationGuard();
}

/** خلاصهٔ فارسی پیوند برای نمایش اینکه کدام روش/گزینه تحت تأثیر است. */
export function activationBindingContextFa(binding: OneLevelMethodBinding): string {
  const option = binding.optionCode ? binding.optionCode : "سطح سرویس (بدون گزینه)";
  return `روش یک‌سطحی ${binding.methodCode} → سرویس ${binding.serviceCode} · ${option}`;
}
