/**
 * TB-SHIPPING-ONE-LEVEL-TABS-088 — pure model for tab 4 «برنامه کاری و ظرفیت» of the one-level shell.
 *
 * The schedule semantics themselves are NOT re-implemented: weekday identity, 24-hour parsing, the
 * half-open `[start,end)` rule, the `HH:mm:ss` wire shape, the IANA timezone requirement, capacity
 * parsing and every Persian refusal message are reused unchanged from the proven 072
 * `shipping-operating-schedule-model.ts`.
 *
 * What this file adds is the *one-level scoping* layer: a schedule is read or written only when it
 * belongs to the selected method's server-resolved legacy identity. A service-level schedule
 * (`optionCode = null`) is the documented fallback for optionless methods; it is never presented as
 * an independently selectable second level and never silently edited as another option's schedule.
 */
import type { ShippingOperatingScheduleAdminItem } from "./shipping-operating-schedule-api.ts";
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
 * آیا این برنامهٔ عملیاتی به پیوند انتخاب‌شده تعلق دارد؟
 *
 * تطبیق صریح است: `serviceCode` باید دقیقاً برابر باشد و `optionCode` باید دقیقاً برابر باشد
 * (تهی با تهی). هیچ fallback ضمنی به «همهٔ برنامه‌های خدمت» وجود ندارد، چون نمایش یا ویرایش برنامهٔ
 * گزینهٔ دیگر زیر نام این روش، همان خطای «سطح دوم پنهان» است.
 */
export function scheduleBelongsToBinding(
  schedule: Pick<ShippingOperatingScheduleAdminItem, "serviceCode" | "optionCode">,
  binding: OneLevelMethodBinding,
): boolean {
  if (schedule.serviceCode !== binding.serviceCode) return false;
  return (schedule.optionCode ?? null) === binding.optionCode;
}

/** برنامه‌های عملیاتی محدودشده به همان روش یک‌سطحی انتخاب‌شده. */
export function scopeSchedulesToBinding(
  schedules: readonly ShippingOperatingScheduleAdminItem[] | null,
  binding: OneLevelMethodBinding,
): ShippingOperatingScheduleAdminItem[] {
  if (!schedules) return [];
  return schedules.filter((schedule) => scheduleBelongsToBinding(schedule, binding));
}

/** آیا پاسخ برنامه‌ها واقعاً مربوط به همین خدمت است؟ پاسخ جابجا هرگز رندر یا ویرایش نمی‌شود. */
export function scheduleViewMatchesBindingService(
  view: { serviceCode: string } | null,
  binding: OneLevelMethodBinding,
): boolean {
  return view != null && view.serviceCode === binding.serviceCode;
}

/**
 * وضعیت قابل‌نمایش زبانهٔ برنامه کاری و ظرفیت.
 *
 * نبود برنامه یک وضعیت واقعی (`ready`) است — «هیچ ساعت صریحی ثبت نشده» یک پاسخ است، نه خطا — ولی
 * نبود پیوند هرگز `ready` نمی‌شود.
 */
export type OneLevelScheduleState =
  | "no-method"
  | "binding-loading"
  | "binding-unavailable"
  | "loading"
  | "denied"
  | "error"
  | "ready";

export function resolveScheduleState(input: {
  hasMethod: boolean;
  bindingLoading: boolean;
  bindingResolved: boolean;
  bindingKnown: boolean;
  schedulesLoading: boolean;
  schedulesState: "ok" | "denied" | "error" | null;
}): OneLevelScheduleState {
  if (!input.hasMethod) return "no-method";
  if (input.bindingLoading || !input.bindingKnown) return "binding-loading";
  if (!input.bindingResolved) return "binding-unavailable";
  if (input.schedulesLoading || input.schedulesState === null) return "loading";
  if (input.schedulesState === "denied") return "denied";
  if (input.schedulesState === "error") return "error";
  return "ready";
}

/**
 * مقصد نوشتن برنامه. تنها راه ساخت `serviceCode`/`optionCode` برای PUT است: هر دو از پیوند حل‌شدهٔ
 * سرور می‌آیند و ورودی کاربر هرگز در آن‌ها دخالت ندارد.
 */
export function scheduleWriteTarget(binding: OneLevelMethodBinding): {
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

/**
 * متن صریح ظرفیت. عمداً «ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)» است: در ۰۷۱/۰۷۲ هیچ شمارش، رزرو یا
 * قفل اتمی وجود ندارد، پس این عدد فقط پیکربندی است و نباید «باقی‌مانده» یا «رزرو‌شده» خوانده شود.
 */
export const ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA = "ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)";

/** تنها حالت اعمال ظرفیت در زمان اجرا؛ هیچ حالت دیگری وجود ندارد. */
export const ONE_LEVEL_CAPACITY_ENFORCEMENT_FA =
  "تنها حالت اعمال ظرفیت در زمان اجرا «اعمال نمی‌شود» (NotEnforced) است؛ این عدد رزرو نمی‌کند.";

/** مرز معنایی بازهٔ حامل: پنجرهٔ تحویل به حامل، نه وعدهٔ تحویل به مشتری. */
export const ONE_LEVEL_WINDOW_SCOPE_FA =
  "این بازه‌ها زمان «تحویل به حامل برای برداشت/ارسال» است، نه زمان تحویل وعده‌داده‌شده به مشتری؛ وعدهٔ مشتری دارایی Order است.";

/** توضیح صریح نبود/غیرفعال بودن برنامه: نه یک بلاک و نه ادعای ظرفیت. */
export const ONE_LEVEL_NO_SCHEDULE_FA =
  "برای این روش هیچ برنامهٔ عملیاتی ثبت نشده است؛ نبود برنامه یعنی «هیچ ساعت صریحی برای تحویل به حامل تعیین نشده» و هیچ رزرو ظرفیتی وجود ندارد.";

/** هشدار صریح دربارهٔ برنامه‌های غیرفعالِ نمایش‌داده‌شده. */
export const ONE_LEVEL_INACTIVE_SCHEDULE_FA =
  "برنامه‌های غیرفعال نمایش داده می‌شوند تا حقیقت پنهان نماند؛ غیرفعال بودن یعنی این ساعت‌ها در محاسبهٔ در دسترس بودن در نظر گرفته نمی‌شوند.";

/** خلاصهٔ خوانای ظرفیت تنظیم‌شده برای هدر بخش؛ تهی یعنی هیچ پنجره‌ای ظرفیت تعیین نکرده است. */
export function configuredCapacityFa(total: number | null): string {
  if (total == null) return `${ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA}: تعیین‌نشده`;
  return `${ONE_LEVEL_CAPACITY_CONFIG_ONLY_FA}: ${total.toLocaleString("fa-IR")}`;
}

/** شمارش صریح برنامه‌های غیرفعال، تا نمایش آن‌ها قابل‌اتکا باشد. */
export function inactiveScheduleCountFa(schedules: readonly ShippingOperatingScheduleAdminItem[]): string {
  const inactive = schedules.filter((schedule) => !schedule.isActive).length;
  return `برنامه‌های غیرفعال: ${inactive.toLocaleString("fa-IR")}`;
}
