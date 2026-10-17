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
  ShippingCoverageMode,
  ShippingMethodCoverageAdminItem,
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
 * نتیجهٔ حل پیوند. حالت‌ها عمداً از هم جدا هستند تا «خاموش»، «مهاجرت‌نشده»، «روش ناشناخته»،
 * «دسترسی ردشده»، «سرور در دسترس نبود» و «پاسخ نامعتبر» با «دادهٔ معتبر» — و با یکدیگر — اشتباه
 * نشوند. تفکیک `denied`/`unreachable` از `untrusted` عمدی است: پیش‌تر همهٔ این‌ها به یک حالت
 * می‌رسیدند و UI دربارهٔ قطع ارتباط می‌گفت «پاسخ سرور معتبر نبود»، یعنی علت واقعی را پنهان
 * می‌کرد و کاربر را به اقدام اشتباه (تغییر داده) هدایت می‌کرد.
 */
export type OneLevelBindingState =
  | { state: "resolved"; binding: OneLevelMethodBinding }
  | { state: "gate-off" }
  | { state: "not-ready" }
  | { state: "unresolved" }
  | { state: "denied" }
  | { state: "unreachable" }
  | { state: "untrusted" };

/** آیا شکست پیوند گذرا است و «تلاش دوباره» معنادار دارد؟ */
export function isRetryableBindingFailure(state: OneLevelBindingState | null): boolean {
  if (!state) return true;
  return state.state === "denied" || state.state === "unreachable";
}

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

/** آیا این مجوز پوشش به همین روش (خدمت + گزینه) تعلق دارد؟ */
export function grantBelongsToBinding(
  grant: Pick<ShippingCoverageGrantAdminItem, "serviceCode" | "optionCode">,
  binding: OneLevelMethodBinding,
): boolean {
  // TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — identity is (service, option), exactly like the
  // server's persisted grant row. A grant of `post:express` is NOT this method's coverage, and a
  // service-level grant is not inherited by an option method: that inheritance is precisely the
  // sibling/provider approximation this task forbids. Optionless methods match only service-level.
  if (grant.serviceCode !== binding.serviceCode) return false;
  return (grant.optionCode ?? null) === binding.optionCode;
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
    case "denied":
      return "دسترسی به پیوند این روش تأیید نشد؛ پس از رفع مشکل دسترسی دوباره تلاش کنید.";
    case "unreachable":
      return "ارتباط با سرور برای خواندن پیوند این روش برقرار نشد؛ اتصال و اجرای سرور را بررسی و دوباره تلاش کنید.";
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

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — the geographic restriction mode of ONE method.
 *
 * The rule this section encodes: an ABSENT declaration means the documented Unrestricted default
 * ("the admin has not restricted this method"), NOT "this carrier covers everything" and NOT "the
 * method is active/priced". A declaration row whose mode cannot be read is untrusted and blocks
 * writes, because silently falling back to Unrestricted would widen a real restriction.
 * ------------------------------------------------------------------------------------------- */

/** حالت قابل‌نمایش محدودیت جغرافیایی یک روش، همراه با منبع قطعی آن. */
export type OneLevelCoverageModeState =
  /** هیچ ردیف اعلامی برای این روش نیست ⇒ پیش‌فرض مستند «بدون محدودیت مدیریتی». */
  | { state: "undeclared" }
  /** ردیف اعلامی سرور خوانده شد. */
  | { state: "declared"; mode: ShippingCoverageMode; coverage: ShippingMethodCoverageAdminItem }
  /** پاسخ سرور دربارهٔ اعلام محدودیت قابل‌اعتماد نیست ⇒ هیچ نوشتنی مجاز نیست. */
  | { state: "untrusted" };

/**
 * اعلام محدودیتِ همین روش را از نمای سرور استخراج می‌کند.
 *
 * تطبیق هویت دقیق است (خدمت + گزینه). ردیف گزینهٔ دیگر هرگز برای این روش خوانده نمی‌شود، و
 * نمای متعلق به خدمت دیگر یا نمای دارای ردیف ناخوانا `untrusted` است.
 */
export function resolveCoverageMode(
  view: ShippingRatesAndCoverageView | null,
  binding: OneLevelMethodBinding,
): OneLevelCoverageModeState {
  if (!view || view.serviceCode !== binding.serviceCode) return { state: "untrusted" };
  if (view.methodCoveragesUntrusted === true) return { state: "untrusted" };
  const rows = view.methodCoverages ?? [];
  const own = rows.find(
    (row) => row.serviceCode === binding.serviceCode && (row.optionCode ?? null) === binding.optionCode,
  );
  if (!own) return { state: "undeclared" };
  return { state: "declared", mode: own.mode, coverage: own };
}

/** متن صریح حالت محدودیت؛ «بدون محدودیت مدیریتی» هرگز ادعای پوشش حامل یا نرخ نیست. */
export function coverageModeFa(state: OneLevelCoverageModeState): string {
  if (state.state === "untrusted") {
    return "اعلام محدودیت جغرافیایی این روش از سرور قابل‌اعتماد خوانده نشد؛ برای احتیاط هیچ تغییری ذخیره نمی‌شود.";
  }
  if (state.state === "undeclared") {
    return "بدون محدودیت مدیریتی (پیش‌فرض): ادمین این روش را به مقصد خاصی محدود نکرده است.";
  }
  return state.mode === "Restricted"
    ? "محدود به مناطق منتخب: فقط مقصدهای مجاز همین روش."
    : "بدون محدودیت مدیریتی: ادمین محدودیتی برای این روش تعریف نکرده است.";
}

/**
 * یادآوری مرز معنایی: حالت محدودیت هیچ‌کدام از این‌ها نیست. متن جدا نگه داشته می‌شود تا اپراتور
 * «بدون محدودیت مدیریتی» را با «آمادهٔ فروش»، «فعال» یا «دارای نرخ» اشتباه نگیرد.
 */
export const COVERAGE_MODE_BOUNDARY_FA =
  "این حالت فقط محدودیت جغرافیایی را بیان می‌کند: نه فعال بودن روش، نه آماده بودن مالی/نرخ و نه تضمین پوشش حامل یا نوبت تحویل.";

/**
 * هشدار حالت Restricted با صفر مجوز فعال. این وضعیت «هیچ مقصدی» است و هرگز نباید به‌طور خاموش
 * به «بدون محدودیت» برگردد.
 */
export const RESTRICTED_EMPTY_NOTE_FA =
  "«محدود به مناطق منتخب» با صفر مقصد مجاز یعنی هیچ مقصدی؛ این وضعیت هرگز خودکار به «بدون محدودیت» تبدیل نمی‌شود.";

/** تعویض حالت، مجوزهای تاریخی را حذف نمی‌کند تا تصمیم‌ها قابل حسابرسی بمانند. */
export const COVERAGE_GRANTS_KEPT_FA =
  "تغییر حالت، مجوزهای پوشش ثبت‌شده را حذف نمی‌کند؛ تاریخچه برای حسابرسی می‌ماند و فقط اثر حالت روی تحدید عوض می‌شود.";

/**
 * آیا نوشتن اعلام محدودیت مجاز است؟ خواندن‌تنها، عملیات در جریان و پاسخ غیرقابل‌اعتماد هر سه
 * نوشتن را می‌بندند؛ مجوز واقعی همیشه سمت سرور (۴۰۱/۴۰۳) دوباره بررسی می‌شود.
 */
export function coverageModeWritable(input: {
  readOnly: boolean;
  busy: boolean;
  modeState: OneLevelCoverageModeState;
}): boolean {
  if (input.readOnly || input.busy) return false;
  return input.modeState.state !== "untrusted";
}

/**
 * آیا انتخاب Restricted باید همراه با ثبت یک مقصد مجاز باشد؟ فقط وقتی هیچ مجوز فعالی برای همین
 * روش وجود ندارد و کاربر مقصدی انتخاب نکرده است، ذخیره جلوی یک «تحدید کامل ناخواسته» را می‌گیرد.
 */
export function restrictedNeedsDestination(input: {
  targetMode: ShippingCoverageMode;
  activeOwnGrantCount: number;
  hasDestination: boolean;
}): boolean {
  if (input.targetMode !== "Restricted") return false;
  return input.activeOwnGrantCount === 0 && !input.hasDestination;
}
