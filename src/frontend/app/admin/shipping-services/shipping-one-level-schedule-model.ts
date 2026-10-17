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

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175 — explicit coverage state of the method-owned schedule.
 *
 * The evaluator semantics are the authority (`ShippingScheduleAvailabilityEvaluator`):
 *   no schedule              -> NotConfigured          (nothing is restricted)
 *   only inactive schedule   -> InactiveNotEnforced    (nothing is restricted)
 *   one active, no windows   -> Unavailable everywhere (restricted-empty: deny)
 *   one active with windows  -> Available inside them / Unavailable outside
 *   two active identities    -> Ambiguous              (no availability claim)
 * This model mirrors those states verbatim; it never invents a fifth business meaning.
 * ------------------------------------------------------------------------------------------- */

/** وضعیت پوشش زمانیِ روش انتخاب‌شده، دقیقاً برگرفته از معنای ارزیاب مالک. */
export type OneLevelScheduleCoverage =
  | "unrestricted"
  | "inactive-only"
  | "ambiguous"
  | "restricted-empty"
  | "restricted";

/**
 * وضعیت پوشش را از ردیف‌های محدودشده به همین هویت می‌سازد.
 *
 * `ambiguous` یک نقص داده است، نه یک حالت تجاری: دو برنامهٔ فعال هم‌هویت هرگز با انتخاب «اولی»
 * به یک ادعای در دسترس بودن تبدیل نمی‌شود.
 */
export function resolveScheduleCoverage(
  schedules: readonly ShippingOperatingScheduleAdminItem[],
): OneLevelScheduleCoverage {
  if (schedules.length === 0) return "unrestricted";
  const active = schedules.filter((schedule) => schedule.isActive);
  if (active.length === 0) return "inactive-only";
  if (active.length > 1) return "ambiguous";
  return active[0]!.windows.length === 0 ? "restricted-empty" : "restricted";
}

/** برچسب کوتاه و صریح هر وضعیت پوشش زمانی برای هدر بخش. */
export function scheduleCoverageLabelFa(coverage: OneLevelScheduleCoverage): string {
  switch (coverage) {
    case "unrestricted":
      return "بدون محدودیت زمانی صریح (پیش‌فرض)";
    case "inactive-only":
      return "برنامهٔ ثبت‌شده غیرفعال است؛ محدودیتی اعمال نمی‌شود";
    case "ambiguous":
      return "ابهام داده: بیش از یک برنامهٔ فعال برای همین هویت";
    case "restricted-empty":
      return "محدود به بازه‌های ثبت‌شده — خالی (هیچ ساعتی مجاز نیست)";
    case "restricted":
      return "محدود به بازه‌های ثبت‌شدهٔ همین روش";
  }
}

/** توضیح صریح وضعیت: هر جمله فقط همان معنایی را می‌گوید که ارزیاب مالک پیاده کرده است. */
export function scheduleCoverageExplanationFa(coverage: OneLevelScheduleCoverage): string {
  switch (coverage) {
    case "unrestricted":
      return ONE_LEVEL_SCHEDULE_UNRESTRICTED_FA;
    case "inactive-only":
      return ONE_LEVEL_SCHEDULE_INACTIVE_FA;
    case "ambiguous":
      return ONE_LEVEL_SCHEDULE_AMBIGUOUS_FA;
    case "restricted-empty":
      return ONE_LEVEL_SCHEDULE_RESTRICTED_EMPTY_FA;
    case "restricted":
      return ONE_LEVEL_SCHEDULE_RESTRICTED_FA;
  }
}

/** نبود برنامهٔ فعال: نبود ساعت صریح، نه ادعای در دسترس بودن حامل. */
export const ONE_LEVEL_SCHEDULE_UNRESTRICTED_FA =
  "برای این روش هیچ برنامهٔ فعالی ثبت نشده است؛ یعنی هیچ ساعت صریحی برای تحویل به حامل تعیین نشده و محدودیت زمانی اعمال نمی‌شود. این وضعیت تضمین در دسترس بودن حامل یا نوبت تحویل نیست.";

/** برنامهٔ فعال با بازه: تحدید واقعی به همان بازه‌ها. */
export const ONE_LEVEL_SCHEDULE_RESTRICTED_FA =
  "برنامهٔ فعال این روش فقط بازه‌های ثبت‌شده را مجاز می‌داند؛ خارج از آن‌ها ارزیاب مالک «در دسترس نیست» می‌گوید.";

/** برنامهٔ فعال بدون بازه: تحدید کامل، نه بازگشت خودکار به «بدون محدودیت». */
export const ONE_LEVEL_SCHEDULE_RESTRICTED_EMPTY_FA =
  "برنامهٔ فعال این روش هیچ بازه‌ای ندارد؛ این «محدود به هیچ بازه» است و هرگز به‌طور خودکار «بدون محدودیت» خوانده نمی‌شود.";

/** برنامهٔ ثبت‌شدهٔ غیرفعال: طبق قرارداد مالک هیچ محدودیتی اعمال نمی‌کند. */
export const ONE_LEVEL_SCHEDULE_INACTIVE_FA =
  "برنامهٔ ثبت‌شدهٔ این روش غیرفعال است؛ طبق قرارداد مالک، برنامهٔ غیرفعال محدودیتی اعمال نمی‌کند و به‌معنای بسته‌بودن روش نیست.";

/** ابهام داده: هیچ ادعای در دسترس بودنی مجاز نیست. */
export const ONE_LEVEL_SCHEDULE_AMBIGUOUS_FA =
  "برای همین هویت بیش از یک برنامهٔ فعال وجود دارد؛ این نقص داده است و هیچ ادعای در دسترس بودنی ساخته نمی‌شود تا انسان آن را حل کند.";

/**
 * اثر واقعیِ امروزِ این برنامه بر مشتری.
 *
 * این جمله از ردیابی واقعی کد می‌آید: افق گروه‌های مشتری از روزهای حمل روش و آماده‌سازی فروشنده
 * ساخته می‌شود و ناظر عملیاتی فقط به‌صورت سایه و خاموش است. پس ذخیرهٔ این برنامه نباید به‌عنوان
 * «تغییر وعدهٔ تحویل مشتری» خوانده شود.
 */
export const ONE_LEVEL_SCHEDULE_CUSTOMER_EFFECT_FA =
  "این بازه‌ها امروز در تاریخ/بازهٔ تحویل مشتری اعمال نمی‌شوند: وعدهٔ فعلی مشتری فقط از روزهای حمل همین روش و آماده‌سازی فروشنده ساخته می‌شود و سایهٔ عملیاتی پیش‌فرض خاموش است. ذخیرهٔ این برنامه هیچ وعدهٔ تحویلی به مشتری را تغییر نمی‌دهد.";

/** حالت مشاهده: هیچ نوشتنی مجاز نیست. */
export const ONE_LEVEL_SCHEDULE_READ_ONLY_FA =
  "این روش در حالت مشاهده است؛ برنامه فقط خواندنی است و هیچ تغییر، فعال‌سازی یا ذخیره‌ای مجاز نیست.";

/** مرز هویت: ردیف سطح خدمت یا گزینهٔ دیگر هرگز زیر نام این روش خوانده یا نوشته نمی‌شود. */
export const ONE_LEVEL_SCHEDULE_IDENTITY_ISOLATION_FA =
  "فقط ردیف همین هویت (کد خدمت + کد گزینهٔ همین روش) خوانده و نوشته می‌شود؛ ردیف سطح خدمت یا برنامهٔ گزینهٔ دیگر نه نمایش داده می‌شود و نه تغییر می‌کند.";

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
