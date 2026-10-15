/**
 * TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — pure presentation/validation helpers for the per-service
 * rates & coverage section. No fetching here, so the money/lead-time/coverage semantics stay testable
 * without a server.
 */
import type {
  ShippingCoverageGrantAdminItem,
  ShippingRatePolicyAdminItem,
  ShippingRatesAndCoverageView,
} from "./shipping-rates-coverage-api.ts";

/** آماده‌سازی نرخ برای فرم؛ همه مقادیر رشته‌اند تا ورودی انسانی دست‌نخورده بماند. */
export type ShippingRateFormValues = {
  chargeAmount: string;
  currencyCode: string;
  preparationDays: string;
  leadDays: string;
  freeAboveThreshold: string;
};

export type ShippingRateFormIssue = {
  field: keyof ShippingRateFormValues;
  messageFa: string;
};

/** مقدار اولیهٔ فرم از یک سیاست موجود (ویرایش)، یا تکمیل پیش‌فرض. */
export function rateFormFromPolicy(policy: ShippingRatePolicyAdminItem | null): ShippingRateFormValues {
  if (!policy) {
    return {
      chargeAmount: "",
      currencyCode: "IRR",
      preparationDays: "",
      leadDays: "",
      freeAboveThreshold: "",
    };
  }
  return {
    chargeAmount: String(policy.chargeAmount),
    currencyCode: policy.currencyCode,
    preparationDays: String(policy.preparationDays),
    leadDays: String(policy.leadDays),
    freeAboveThreshold: policy.freeAboveThreshold == null ? "" : String(policy.freeAboveThreshold),
  };
}

function parseNonNegativeInt(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+$/.test(trimmed)) return null;
  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

function parseNonNegativeDecimal(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d+)?$/.test(trimmed)) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

/**
 * اعتبارسنجی ورودی نرخ پیش از هر فراخوانی API. روزهای آماده‌سازی و حمل جدا نگه داشته می‌شوند تا
 * یک «زمان کل» مبهم جایگزین دو معنی متفاوت نشود.
 */
export function validateShippingRateForm(values: ShippingRateFormValues): ShippingRateFormIssue[] {
  const issues: ShippingRateFormIssue[] = [];
  const charge = parseNonNegativeDecimal(values.chargeAmount);
  if (charge == null) {
    issues.push({ field: "chargeAmount", messageFa: "مبلغ هزینه باید عددی صفر یا بزرگ‌تر باشد." });
  }
  if (!/^[A-Za-z]{3}$/.test(values.currencyCode.trim())) {
    issues.push({ field: "currencyCode", messageFa: "کد ارز باید سه حرف لاتین باشد." });
  }
  if (parseNonNegativeInt(values.preparationDays) == null) {
    issues.push({ field: "preparationDays", messageFa: "روزهای آماده‌سازی باید عددی صحیح و نامنفی باشد." });
  }
  if (parseNonNegativeInt(values.leadDays) == null) {
    issues.push({ field: "leadDays", messageFa: "روزهای حمل باید عددی صحیح و نامنفی باشد." });
  }
  const threshold = values.freeAboveThreshold.trim();
  if (threshold !== "" && parseNonNegativeDecimal(threshold) == null) {
    issues.push({ field: "freeAboveThreshold", messageFa: "آستانهٔ ارسال رایگان باید عددی نامنفی باشد یا خالی بماند." });
  }
  return issues;
}

/** خروجی validateshape را به payload عددی تبدیل می‌کند؛ فقط پس از اعتبارسنجی موفق صدا زده شود. */
export function rateFormToPayload(values: ShippingRateFormValues): {
  chargeAmount: number;
  currencyCode: string;
  preparationDays: number;
  leadDays: number;
  freeAboveThreshold: number | null;
} {
  const threshold = values.freeAboveThreshold.trim();
  return {
    chargeAmount: parseNonNegativeDecimal(values.chargeAmount) ?? 0,
    currencyCode: values.currencyCode.trim().toUpperCase(),
    preparationDays: parseNonNegativeInt(values.preparationDays) ?? 0,
    leadDays: parseNonNegativeInt(values.leadDays) ?? 0,
    freeAboveThreshold: threshold === "" ? null : parseNonNegativeDecimal(threshold),
  };
}

const STORED_LEVEL_FA: Record<string, string> = {
  country: "کشور",
  administrativearea: "استان",
  city: "شهر",
  servicearea: "ناحیهٔ خدمت",
};

function levelFa(level: string): string {
  const key = level.trim().toLowerCase().replace(/[_\s-]/g, "");
  return STORED_LEVEL_FA[key] ?? (level.trim() || "نامشخص");
}

/** برچسب فارسی سطح دامنه برای نمایش؛ مستقل از متن خام سرویس. */
export function scopeLevelFa(level: string): string {
  return levelFa(level);
}

/** خلاصهٔ خوانا و لاتین‌محور یک دامنه: شناسه‌های پایدار نشان داده می‌شوند، نه برچسب ساختگی. */
export function scopeSummary(item: {
  level: string;
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
}): string {
  const parts = [`${levelFa(item.level)} · ${shortId(item.countryId)}`];
  if (item.administrativeAreaId) parts.push(`استان ${shortId(item.administrativeAreaId)}`);
  if (item.cityId) parts.push(`شهر ${shortId(item.cityId)}`);
  if (item.serviceAreaId) parts.push(`ناحیه ${shortId(item.serviceAreaId)}`);
  return parts.join(" / ");
}

function shortId(id: string): string {
  return id.length <= 8 ? id : `${id.slice(0, 8)}…`;
}

/** مبلغ و ارز به‌شکل قابل خواندن؛ بدون تبدیل یا گرد کردن پنهان. */
export function formatCharge(amount: number, currencyCode: string): string {
  const formatted = new Intl.NumberFormat("fa-IR").format(amount);
  return `${formatted} ${currencyCode.trim().toUpperCase()}`;
}

/** آستانهٔ ارسال رایگان: خالی یعنی «بدون آستانه»، نه صفر. */
export function formatFreeAboveThreshold(value: number | null, currencyCode: string): string {
  return value == null ? "—" : formatCharge(value, currencyCode);
}

export type RatesCoverageSummary = {
  rateCount: number;
  activeRateCount: number;
  grantCount: number;
  activeGrantCount: number;
  /** نبود مجوز یعنی «بدون پوشش»؛ هرگز به «بدون محدودیت» تعبیر نمی‌شود. */
  hasCoverage: boolean;
};

/** خلاصهٔ شمارشی نما برای هدر بخش. */
export function summarizeRatesCoverage(view: ShippingRatesAndCoverageView | null): RatesCoverageSummary {  const rates = view?.ratePolicies ?? [];
  const grants = view?.coverageGrants ?? [];
  const activeGrants = grants.filter((g) => g.isActive).length;
  return {
    rateCount: rates.length,
    activeRateCount: rates.filter((r) => r.isActive).length,
    grantCount: grants.length,
    activeGrantCount: activeGrants,
    hasCoverage: activeGrants > 0,
  };
}

/** متن صریح وضعیت پوشش برای نمایش در UI. */
export function coverageStateFa(view: ShippingRatesAndCoverageView | null): string {
  const summary = summarizeRatesCoverage(view);
  if (summary.hasCoverage) {
    return `پوشش فعال: ${summary.activeGrantCount.toLocaleString("fa-IR")} از ${summary.grantCount.toLocaleString("fa-IR")}`;
  }
  return "بدون پوشش — تا زمانی که مجوز فعال ثبت نشود، این خدمت مقصدی را پوشش نمی‌دهد.";
}

/** آیا این سرویس برای نویسندگی نرخ/پوشش آماده است؟ (کد خدمت باید موجود باشد) */
export function canEditRatesFor(serviceCode: string): boolean {
  return serviceCode.trim().length > 0;
}

/** ترتیب نمایش پایدار: فعال‌ها اول، سپس بر اساس شناسهٔ سطح. */
export function sortGrants(
  grants: ShippingCoverageGrantAdminItem[],
): ShippingCoverageGrantAdminItem[] {
  return [...grants].sort((left, right) => {
    if (left.isActive !== right.isActive) return left.isActive ? -1 : 1;
    return left.level.localeCompare(right.level, "en");
  });
}

/** اعلام صریح وضعیت: شناسهٔ دستی یا ساختگی پذیرفته نمی‌شود (خواندن مجاز Geography فعال است). */
export const GEOGRAPHY_ID_LOOKUP_BLOCKED_FA =
  "دامنهٔ مقصد فقط از فهرست مرجع Geography انتخاب می‌شود؛ شناسهٔ دستی یا ساختگی پذیرفته نمی‌شود.";

/** انتخاب دامنهٔ مقصد از فهرست مرجع Geography؛ همه مقادیر شناسهٔ پایدار یا تهی‌اند. */
export type DestinationSelection = {
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
};

/** انتخاب خالی؛ هیچ شناسه‌ای پیش‌فرض حدس زده نمی‌شود. */
export const EMPTY_DESTINATION_SELECTION: DestinationSelection = {
  countryId: "",
  administrativeAreaId: null,
  cityId: null,
  serviceAreaId: null,
};

export type DestinationScopeLevel = "country" | "administrativeArea" | "city" | "serviceArea";

/** سطح دامنه از عمیق‌ترین انتخابی که کاربر واقعاً انجام داده است. */
export function scopeLevelOf(selection: DestinationSelection): DestinationScopeLevel | null {
  if (!selection.countryId) return null;
  if (selection.serviceAreaId) return "serviceArea";
  if (selection.cityId) return "city";
  if (selection.administrativeAreaId) return "administrativeArea";
  return "country";
}

/**
 * تبدیل انتخاب به payload دامنه. شناسه‌های عمیق‌تر از سطح انتخاب‌شده حذف می‌شوند تا ناحیه/شهر
 * از روی نام یا ترتیب حدس زده نشود؛ هر مقدار یا شناسهٔ واقعی برگشتی است یا تهی.
 */
export function buildScopeWrite(selection: DestinationSelection): {
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
} {
  const level = scopeLevelOf(selection);
  return {
    countryId: selection.countryId,
    administrativeAreaId:
      level === "administrativeArea" || level === "city" || level === "serviceArea"
        ? selection.administrativeAreaId
        : null,
    cityId: level === "city" || level === "serviceArea" ? selection.cityId : null,
    serviceAreaId: level === "serviceArea" ? selection.serviceAreaId : null,
  };
}

/**
 * آیا دامنه برای نوشتن آماده است؟ (کشور الزامی است و ناحیهٔ خدمت بدون شهر پذیرفته نمی‌شود)
 *
 * یک دامنهٔ شهر، استان مالک خود را نیز به‌عنوان والد حمل می‌کند؛ قرارداد دامنه می‌گوید هر شیء
 * دامنه‌دار باید همیشه همهٔ والدها را داشته باشد. پس «استان + شهر» یک ترکیب نامعتبر نیست، بلکه
 * همان دامنهٔ سطح شهر با والد درست است و بک‌اند آن را می‌پذیرد. تنها ترکیب نامعتبر، ناحیهٔ خدمت
 * بدون شهر مالک است.
 */
export function validateDestinationSelection(selection: DestinationSelection): string[] {
  const issues: string[] = [];
  if (!selection.countryId) {
    issues.push("کشور مقصد را از فهرست انتخاب کنید.");
  }
  if (selection.serviceAreaId && !selection.cityId) {
    issues.push("ناحیهٔ خدمت بدون شهر مالک معتبر نیست.");
  }
  return issues;
}

/** تغییر والد، انتخاب‌های وابسته را پاک می‌کند تا شناسهٔ ناهم‌بسته باقی نماند. */
export function clearDependents(
  selection: DestinationSelection,
  changed: "country" | "area" | "city",
): DestinationSelection {
  if (changed === "country") {
    return { countryId: selection.countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null };
  }
  if (changed === "area") {
    return { ...selection, cityId: null, serviceAreaId: null };
  }
  return { ...selection, serviceAreaId: null };
}

/* ---------------------------------------------------------------------------------------------
 * TB-SHIPPING-MVP-FINAL-CLOSURE-070 — option dimension of a rate policy.
 *
 * A multi-option service (for example tipax with `standard` / `express`) needs one rate per
 * option. The option list is never invented here: it comes from the real Fulfillment service
 * tree (`GET /v1/admin/shipping-methods`) and only the codes that tree returns are offered.
 * The empty selection means the service-level policy — the documented fallback that already
 * applies to single-option and no-option services.
 * ------------------------------------------------------------------------------------------- */

/** One selectable option of a shipping service, exactly as the Fulfillment service tree returns it. */
export type ShippingServiceOptionChoice = {
  code: string;
  labelFa: string;
};

/** مقدار نگه‌دارندهٔ سیاست سطح خدمت در `<select>`؛ معادل `optionCode = null` روی سیم. */
export const SERVICE_LEVEL_OPTION_VALUE = "";

/** رشتهٔ قابل استفاده در کلید React برای یک سیاست (سطح خدمت مقدار تهی می‌گیرد). */
export function rateOptionKey(optionCode: string | null): string {
  return optionCode ?? SERVICE_LEVEL_OPTION_VALUE;
}

/**
 * برچسب نمایش یک سیاست: گزینهٔ شناخته‌شده با نام فارسی خودش، سیاست سطح خدمت با متن صریح
 * «سطح خدمت»، و یک کد ناشناخته با خود کد خام نمایش داده می‌شود تا هویت تغییرناپذیرش پنهان نماند.
 */
export function rateOptionLabelFa(
  optionCode: string | null,
  options: ShippingServiceOptionChoice[],
): string {
  if (!optionCode) return "سطح خدمت (پیش‌فرض خدمت)";
  const found = options.find((option) => option.code === optionCode);
  return found && found.labelFa ? found.labelFa : optionCode;
}

/** متن اعلام اینکه یک نرخ روی سطح خدمت است و نه روی یک گزینه. */
export const SERVICE_LEVEL_RATE_FA = "سطح خدمت — برای همهٔ گزینه‌های این خدمت اعمال می‌شود تا زمانی که نرخ اختصاصی گزینه موجود باشد.";

/**
 * آیا گزینهٔ انتخابی واقعاً متعلق به همین خدمت است؟ کد ناشناخته پیش از هر فراخوانی API رد
 * می‌شود تا `shipping.service_option.unknown` هرگز از سمت مرورگر ساخته نشود.
 */
export function validateRateOption(
  optionCode: string | null,
  options: ShippingServiceOptionChoice[],
): string[] {
  if (!optionCode) return [];
  if (options.length === 0) {
    return ["این خدمت هیچ گزینه‌ای ندارد؛ نرخ باید روی سطح خدمت ثبت شود."];
  }
  if (!options.some((option) => option.code === optionCode)) {
    return ["گزینهٔ انتخابی به این خدمت تعلق ندارد (shipping.service_option.unknown)."];
  }
  return [];
}

/**
 * نرخ‌های یک خدمت را بر اساس گزینه گروه‌بندی می‌کند: ابتدا سیاست‌های سطح خدمت، سپس هر گزینه
 * بهترتیب فهرست واقعی سرویس. هیچ نرخی با حدس جابجا نمی‌شود.
 */
export function groupRatesByOption<T extends { optionCode: string | null }>(
  rates: T[],
  options: ShippingServiceOptionChoice[],
): Array<{ optionCode: string | null; rates: T[] }> {
  const groups: Array<{ optionCode: string | null; rates: T[] }> = [
    { optionCode: null, rates: rates.filter((rate) => !rate.optionCode) },
  ];
  for (const option of options) {
    const matching = rates.filter((rate) => rate.optionCode === option.code);
    if (matching.length > 0) groups.push({ optionCode: option.code, rates: matching });
  }
  const known = new Set(options.map((option) => option.code));
  const orphaned = rates.filter((rate) => rate.optionCode && !known.has(rate.optionCode));
  if (orphaned.length > 0) {
    const seen = new Set<string>();
    for (const rate of orphaned) {
      const code = rate.optionCode as string;
      if (seen.has(code)) continue;
      seen.add(code);
      groups.push({ optionCode: code, rates: rates.filter((r) => r.optionCode === code) });
    }
  }
  return groups.filter((group) => group.rates.length > 0);
}

/** برچسب فارسی خطای گزینهٔ ناشناخته که بک‌اند نیز با همان کد ثابت برمی‌گرداند. */
export const SERVICE_OPTION_UNKNOWN_CODE = "shipping.service_option.unknown";


