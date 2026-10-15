/**
 * TB-SHIPPING-ONE-LEVEL-TABS-086 — pure model for tab 2 «تعرفه و پوشش» of the one-level shell.
 *
 * The hard rule this file exists to enforce: a one-level method code is **never** split in the
 * browser into a service/option pair. `post:standard`, `tipax:express` and the optionless
 * `store_courier` all look alike textually, and the historical composite/optionless codes make any
 * textual inference wrong. The only legitimate source of the legacy binding is the server-owned
 * resolver (`GET …/shipping-methods/one-level/{methodCode}/binding`), and this model refuses to
 * continue when that resolver does not answer with a resolved binding.
 *
 * Everything here is React-free so the scoping and fail-closed behavior is testable without a server.
 */
import type {
  ShippingCoverageGrantAdminItem,
  ShippingRatePolicyAdminItem,
  ShippingRatesAndCoverageView,
} from "./shipping-rates-coverage-api.ts";

/** مسیر سرور برای حل پیوند قدیمی یک روش؛ تنها منبع مجاز نگاشت کد روش به سرویس/گزینه. */
export function oneLevelBindingPath(methodCode: string): string {
  return `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(methodCode)}/binding`;
}

/**
 * پیوند حل‌شدهٔ یک روش. `serviceCode` الزامی است؛ `optionCode` تهی یعنی روش به سرویس بدون گزینه
 * نگاشته شده و سیاست‌های سطح سرویس همان‌هایی هستند که اعمال می‌شوند (fallback مستند).
 */
export type OneLevelMethodBinding = {
  methodCode: string;
  serviceCode: string;
  optionCode: string | null;
  isActive: boolean;
};

/**
 * نتیجهٔ حل پیوند. حالت‌ها عمداً از هم جدا هستند تا «خاموش»، «مهاجرت‌نشده» و «روش ناشناخته» با
 * «داده‌ی معتبر» اشتباه نشوند.
 */
export type OneLevelBindingState =
  | { state: "resolved"; binding: OneLevelMethodBinding }
  | { state: "gate-off" }
  | { state: "not-ready" }
  | { state: "unresolved" }
  | { state: "untrusted" };

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

/**
 * نگاشت پاسخ سرور به وضعیت پیوند — fail-closed.
 *
 * `resolved` فقط وقتی برمی‌گردد که سرور صریحاً `gate === "Enabled"`، `ready === true`,
 * `resolved === true` و یک `serviceCode` غیرتهی داده باشد. هر چیز دیگر — پاسخ نامعتبر،
 * دروازهٔ خاموش، کاتالوگ مهاجرت‌نشده، روش ناشناخته — به حالت غیرقابل‌استفاده نگاشت می‌شود و
 * هرگز به پیوند حدسی تبدیل نمی‌شود.
 */
export function mapHostBinding(payload: unknown): OneLevelBindingState {
  const row = asRecord(payload);
  if (!row) return { state: "untrusted" };

  const gate = text(row.gate ?? row.Gate);
  if (gate !== "Enabled") return { state: "gate-off" };

  const ready = row.ready ?? row.Ready;
  if (ready !== true) return { state: "not-ready" };

  if ((row.resolved ?? row.Resolved) !== true) return { state: "unresolved" };

  const methodCode = text(row.methodCode ?? row.MethodCode).trim();
  const serviceCode = text(row.serviceCode ?? row.ServiceCode).trim();
  if (!methodCode || !serviceCode) return { state: "untrusted" };

  const rawOption = text(row.optionCode ?? row.OptionCode).trim();
  return {
    state: "resolved",
    binding: {
      methodCode,
      serviceCode,
      optionCode: rawOption ? rawOption : null,
      isActive: (row.isActive ?? row.IsActive) === true,
    },
  };
}

/**
 * آیا این ردیف نرخ به پیوند انتخاب‌شده تعلق دارد؟
 *
 * تطبیق کاملاً صریح است: `serviceCode` باید دقیقاً برابر باشد و `optionCode` باید دقیقاً برابر
 * باشد (تهی با تهی). هیچ fallback ضمنی به «همهٔ نرخ‌های خدمت» انجام نمی‌شود، چون نمایش نرخ یک
 * گزینه زیر نام گزینهٔ دیگر، همان خطای «سطح دوم پنهان» است.
 */
export function rateBelongsToBinding(
  rate: Pick<ShippingRatePolicyAdminItem, "serviceCode" | "optionCode">,
  binding: OneLevelMethodBinding,
): boolean {
  if (rate.serviceCode !== binding.serviceCode) return false;
  const rateOption = rate.optionCode ?? null;
  return rateOption === binding.optionCode;
}

/** آیا این مجوز پوشش به خدمت پیوند تعلق دارد؟ پوشش در دامنهٔ خدمت است، نه گزینه. */
export function grantBelongsToBinding(
  grant: Pick<ShippingCoverageGrantAdminItem, "serviceCode">,
  binding: OneLevelMethodBinding,
): boolean {
  return grant.serviceCode === binding.serviceCode;
}

/** نمای نرخ/پوشش محدودشده به همان روش یک‌سطحی انتخاب‌شده. */
export function scopeRatesToBinding(
  view: ShippingRatesAndCoverageView | null,
  binding: OneLevelMethodBinding,
): { rates: ShippingRatePolicyAdminItem[]; grants: ShippingCoverageGrantAdminItem[] } {
  if (!view) return { rates: [], grants: [] };
  return {
    rates: view.ratePolicies.filter((rate) => rateBelongsToBinding(rate, binding)),
    grants: view.coverageGrants.filter((grant) => grantBelongsToBinding(grant, binding)),
  };
}

/**
 * آیا پاسخ نرخ‌ها واقعاً مربوط به همین خدمت است؟ پاسخ اشتباه/جابجا هرگز رندر نمی‌شود، چون نرخ
 * خدمت دیگری نباید زیر نام این روش نمایش داده شود.
 */
export function viewMatchesBindingService(
  view: ShippingRatesAndCoverageView | null,
  binding: OneLevelMethodBinding,
): boolean {
  return view != null && view.serviceCode === binding.serviceCode;
}

/**
 * وضعیت قابل‌نمایش زبانهٔ تعرفه و پوشش. هیچ حالت «موفقِ خالی» جعلی وجود ندارد:
 * خالی‌بودن نرخ‌ها یک وضعیت واقعی (`ready`) است، ولی نبود پیوند هرگز `ready` نمی‌شود.
 */
export type OneLevelRatesState =
  | "no-method"
  | "binding-loading"
  | "binding-unavailable"
  | "loading"
  | "denied"
  | "error"
  | "ready";

export function resolveRatesState(input: {
  hasMethod: boolean;
  bindingLoading: boolean;
  bindingState: OneLevelBindingState | null;
  ratesLoading: boolean;
  ratesState: "ok" | "denied" | "error" | null;
}): OneLevelRatesState {
  if (!input.hasMethod) return "no-method";
  if (input.bindingLoading || !input.bindingState) return "binding-loading";
  if (input.bindingState.state !== "resolved") return "binding-unavailable";
  if (input.ratesLoading || input.ratesState === null) return "loading";
  if (input.ratesState === "denied") return "denied";
  if (input.ratesState === "error") return "error";
  return "ready";
}

/** متن صریح وضعیت غیرقابل‌استفادهٔ پیوند؛ کد روش را نشان می‌دهد تا کاربر بداند کدام روش مشکل دارد. */
export function bindingUnavailableFa(state: OneLevelBindingState | null, methodCode: string): string {
  if (!state) return `پیوند روش «${methodCode}» هنوز خوانده نشده است.`;
  switch (state.state) {
    case "gate-off":
      return "دروازهٔ کاتالوگ یک‌سطحی خاموش است؛ تعرفه و پوشش این روش در دسترس نیست.";
    case "not-ready":
      return "کاتالوگ یک‌سطحی هنوز مهاجرت نشده است؛ پیوند سرویس/گزینه این روش قابل‌اعتماد نیست.";
    case "unresolved":
      return `پیوند سرویس/گزینه برای روش «${methodCode}» یافت نشد؛ هیچ نگاشت حدسی انجام نمی‌شود.`;
    default:
      return "پاسخ سرور برای پیوند این روش معتبر نبود؛ برای احتیاط هیچ نرخی نمایش داده نمی‌شود.";
  }
}

/**
 * نگهبان نسل انتخاب: اگر کاربر وسط یک عملیات روش را عوض کند، پاسخ‌های در راه باید باطل شوند تا
 * نتیجهٔ روش قبلی روی روش جدید ذخیره/نمایش داده نشود.
 */
export function createBindingGenerationGuard(): { begin: () => () => boolean } {
  let latest = 0;
  return {
    begin() {
      const generation = ++latest;
      return () => generation === latest;
    },
  };
}

/** خلاصهٔ فارسی پیوند برای نمایش «کدام روش یک‌سطحی در حال ویرایش است». */
export function bindingContextFa(binding: OneLevelMethodBinding): string {
  const option = binding.optionCode ? binding.optionCode : "سطح سرویس (بدون گزینه)";
  return `روش یک‌سطحی ${binding.methodCode} → سرویس ${binding.serviceCode} · ${option}`;
}
