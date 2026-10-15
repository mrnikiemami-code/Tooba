/**
 * TB-SHIPPING-FULL-OPERATIONS-077 — pure presentation/validation model for the per-service
 * «هزینه بسته‌بندی و نحوه پرداخت کرایه» section. No React and no network here, so money parsing,
 * payer-mode wording and the Persian refusal messages stay testable without a server.
 *
 * Two boundaries are stated explicitly and never blurred:
 *
 * 1. **Payer mode is a quotation fact.** `Prepaid`/`CollectOnDelivery` says who owes the carrier
 *    freight and when. It creates no Payment transaction, and real collect-on-delivery money
 *    collection/settlement is **not** wired into Checkout or Payment by this screen.
 * 2. **Free shipping is a freight rule only.** A free-freight threshold waives carrier freight; it
 *    does not make the packaging fee free.
 */
import type {
  ShippingFreightPayerMode,
  ShippingPackagingPolicyAdminItem,
} from "./shipping-packaging-api.ts";
import type { ShippingServiceOptionChoice } from "./shipping-rates-coverage-model.ts";

/** آماده‌سازی فرم؛ همه مقادیر رشته‌اند تا ورودی انسانی دست‌نخورده بماند. */
export type ShippingPackagingFormValues = {
  feeAmount: string;
  currencyCode: string;
  payerMode: ShippingFreightPayerMode;
};

export type ShippingPackagingFormIssue = {
  field: keyof ShippingPackagingFormValues;
  messageFa: string;
};

/** سقف مبلغ بسته‌بندی؛ همان سقف دامنهٔ Shipping تا ورودی نامعتبر پیش از API رد شود. */
export const MAX_PACKAGING_FEE_AMOUNT = 1_000_000_000;

/** ISO 4217: کد ارز دقیقاً سه حرف لاتین است. */
const CURRENCY_PATTERN = /^[A-Za-z]{3}$/;

/** مقدار پیش‌فرض فرم برای ایجاد؛ ارز پیش‌فرض همان ارز بازار است. */
export function emptyPackagingForm(currencyCode = "IRR"): ShippingPackagingFormValues {
  return { feeAmount: "", currencyCode, payerMode: "Prepaid" };
}

/** مقدار اولیهٔ فرم از یک سیاست موجود (ویرایش). */
export function packagingFormFromPolicy(policy: ShippingPackagingPolicyAdminItem): ShippingPackagingFormValues {
  return {
    feeAmount: formatPackagingAmountInput(policy.feeAmount),
    currencyCode: policy.currencyCode,
    payerMode: policy.freightPayerMode,
  };
}

/**
 * مبلغ را برای ورودی فرم به رشتهٔ اعشاری دقیق می‌برد.
 *
 * `decimal(18,2)` بک‌اند است، پس نمایش ورودی هرگز از `toFixed` استفاده نمی‌کند: `12000.00` به
 * `12000` و `12000.50` به `12000.5` می‌رود تا ذخیرهٔ دوباره همان مقدار باشد و صفرهای اضافی
 * مقدار را تغییر ندهند.
 */
export function formatPackagingAmountInput(amount: number): string {
  if (!Number.isFinite(amount)) return "";
  if (Number.isInteger(amount)) return String(amount);
  return String(amount).replace(/0+$/, "").replace(/\.$/, "");
}

/**
 * مبلغ نامنفی با حداکثر دو رقم اعشار (هم‌شکل ستون `numeric(18,2)`).
 *
 * هر ورودی دقیق‌تر از سانتی/دینار (مثلاً `1.005`) رد می‌شود و گرد نمی‌شود؛ گرد کردن خاموش
 * یعنی مبلغی که کاربر تایپ نکرده به مشتری تحمیل می‌شود.
 */
export function parsePackagingFee(raw: string): number | null {
  const trimmed = raw.trim();
  if (trimmed === "") return null;
  const match = /^(\d+)(?:\.(\d{1,2}))?$/.exec(trimmed);
  if (!match) return null;
  const whole = Number(match[1]);
  if (!Number.isSafeInteger(whole)) return null;
  const fraction = (match[2] ?? "").padEnd(2, "0");
  const cents = whole * 100 + Number(fraction);
  if (!Number.isSafeInteger(cents)) return null;
  const value = cents / 100;
  return Number.isFinite(value) ? value : null;
}

/** ارز را به شکل canonical (`IRR`) نرمال می‌کند. */
export function normalizeCurrencyCode(raw: string): string {
  return raw.trim().toUpperCase();
}

/** اعتبارسنجی فرم پیش از هر فراخوانی API. خروجی خالی یعنی فرم معتبر است. */
export function validatePackagingForm(values: ShippingPackagingFormValues): ShippingPackagingFormIssue[] {
  const issues: ShippingPackagingFormIssue[] = [];

  const fee = parsePackagingFee(values.feeAmount);
  if (fee == null) {
    issues.push({
      field: "feeAmount",
      messageFa: "هزینهٔ بسته‌بندی باید عددی نامنفی با حداکثر دو رقم اعشار باشد (مثال: 12000 یا 12000.50).",
    });
  } else if (fee > MAX_PACKAGING_FEE_AMOUNT) {
    issues.push({
      field: "feeAmount",
      messageFa: `هزینهٔ بسته‌بندی نمی‌تواند از ${MAX_PACKAGING_FEE_AMOUNT.toLocaleString("fa-IR")} بیشتر باشد.`,
    });
  }

  if (!CURRENCY_PATTERN.test(values.currencyCode.trim())) {
    issues.push({ field: "currencyCode", messageFa: "کد ارز باید سه حرف لاتین باشد (مثال: IRR)." });
  }

  return issues;
}

/** بدنهٔ ارسال به API؛ فقط پس از اعتبارسنجی موفق صدا زده می‌شود. */
export function packagingFormToPayload(values: ShippingPackagingFormValues): {
  feeAmount: number;
  currencyCode: string;
  freightPayerMode: ShippingFreightPayerMode;
} {
  return {
    feeAmount: parsePackagingFee(values.feeAmount) ?? 0,
    currencyCode: normalizeCurrencyCode(values.currencyCode),
    freightPayerMode: values.payerMode,
  };
}

/** برچسب فارسی طرف پرداخت‌کنندهٔ کرایه؛ هرگز متن آزاد ذخیره نمی‌شود. */
export function payerModeLabelFa(mode: ShippingFreightPayerMode): string {
  return mode === "CollectOnDelivery" ? "پس‌کرایه" : "پیش‌کرایه";
}

/** برچسب فارسی وضعیت فعال. */
export function packagingActiveLabelFa(isActive: boolean): string {
  return isActive ? "فعال" : "غیرفعال";
}

/** متن اعلام معنای نبود سیاست: صفر صریح، نه هزینهٔ پنهان و نه پیش‌فرض مبهم. */
export const NO_PACKAGING_POLICY_FA =
  "بدون سیاست بسته‌بندی = هزینهٔ بسته‌بندی صفر و طرف پرداخت کرایه پیش‌کرایهٔ پیش‌فرض است؛ هیچ هزینهٔ پنهانی اضافه نمی‌شود.";

/** متن اعلام مرز مالکیت: طرف پرداخت یک واقعیت نرخ است، نه یک تراکنش پرداخت. */
export const PAYER_MODE_IS_QUOTE_FACT_FA =
  "نحوهٔ پرداخت کرایه یک «واقعیت نرخ» است: Shipping می‌گوید کرایه پیش‌کرایه است یا هنگام تحویل وصول می‌شود. وصول و تسویهٔ واقعی «پس‌کرایه» هنوز به Checkout/پرداخت متصل نشده است.";

/** متن اعلام اینکه ارسال رایگان هزینهٔ بسته‌بندی را رایگان نمی‌کند. */
export const FREE_SHIPPING_DOES_NOT_WAIVE_PACKAGING_FA =
  "ارسال رایگان، هزینهٔ بسته‌بندی را رایگان نمی‌کند؛ معافیت ارسال رایگان فقط به کرایهٔ حامل مربوط است.";

/** متن اعلام مرز دامنهٔ مقصد: شناسهٔ دستی پذیرفته نمی‌شود. */
export const PACKAGING_SCOPE_NOTE_FA =
  "دامنهٔ مقصد فقط از فهرست مرجع Geography انتخاب می‌شود؛ شناسهٔ دستی یا ساختگی پذیرفته نمی‌شود.";

/** متن اعلام معنای سیاست سطح خدمت به‌عنوان fallback مستند. */
export const SERVICE_LEVEL_PACKAGING_FA =
  "سیاست سطح خدمت — وقتی هیچ سیاست اختصاصی برای گزینه‌ای ثبت نشده باشد، این سیاست هویت پیش‌فرض خدمت است.";

/** مقدار نگه‌دارندهٔ سیاست سطح خدمت در `<select>`؛ معادل `optionCode = null` روی سیم. */
export const SERVICE_LEVEL_PACKAGING_VALUE = "";

/** کلید پایدار یک سیاست در UI (سطح خدمت مقدار تهی می‌گیرد). */
export function packagingOptionKey(optionCode: string | null): string {
  return optionCode ?? SERVICE_LEVEL_PACKAGING_VALUE;
}

/**
 * برچسب نمایش یک سیاست: گزینهٔ شناخته‌شده با نام فارسی خودش، سیاست سطح خدمت با متن صریح، و
 * یک کد ناشناخته با خود کد خام تا هویت تغییرناپذیرش پنهان نماند.
 */
export function packagingOptionLabelFa(
  optionCode: string | null,
  options: readonly ShippingServiceOptionChoice[],
): string {
  if (!optionCode) return "سطح خدمت (پیش‌فرض خدمت)";
  const found = options.find((option) => option.code === optionCode);
  return found && found.labelFa ? found.labelFa : optionCode;
}

/**
 * آیا گزینهٔ انتخابی واقعاً متعلق به همین خدمت است؟ کد ناشناخته پیش از فراخوانی API رد می‌شود تا
 * `shipping.service_option.unknown` از سمت مرورگر ساخته نشود.
 */
export function validatePackagingOption(
  optionCode: string | null,
  options: readonly ShippingServiceOptionChoice[],
): string[] {
  if (!optionCode) return [];
  if (options.length === 0) {
    return ["این خدمت هیچ گزینه‌ای ندارد؛ سیاست بسته‌بندی باید در سطح خدمت ثبت شود."];
  }
  if (!options.some((option) => option.code === optionCode)) {
    return ["گزینهٔ انتخابی به این خدمت تعلق ندارد (shipping.service_option.unknown)."];
  }
  return [];
}

/** مبلغ و ارز به‌شکل قابل خواندن؛ بدون تبدیل یا گرد کردن پنهان. */
export function formatPackagingFee(amount: number, currencyCode: string): string {
  const formatted = new Intl.NumberFormat("fa-IR").format(amount);
  const currency = normalizeCurrencyCode(currencyCode);
  return currency ? `${formatted} ${currency}` : formatted;
}

export type PackagingSummary = {
  policyCount: number;
  activeCount: number;
  collectOnDeliveryCount: number;
};

/** خلاصهٔ شمارشی نما برای هدر بخش. */
export function summarizePackagingPolicies(
  policies: readonly ShippingPackagingPolicyAdminItem[],
): PackagingSummary {
  return {
    policyCount: policies.length,
    activeCount: policies.filter((policy) => policy.isActive).length,
    collectOnDeliveryCount: policies.filter((policy) => policy.freightPayerMode === "CollectOnDelivery").length,
  };
}

/** متن صریح وضعیت سیاست‌های یک خدمت. */
export function packagingStateFa(policies: readonly ShippingPackagingPolicyAdminItem[]): string {
  const summary = summarizePackagingPolicies(policies);
  if (summary.policyCount === 0) {
    return NO_PACKAGING_POLICY_FA;
  }
  const active = summary.activeCount.toLocaleString("fa-IR");
  const total = summary.policyCount.toLocaleString("fa-IR");
  return `سیاست‌های بسته‌بندی فعال: ${active} از ${total}`;
}

/**
 * سیاست‌ها را بر اساس گزینه گروه‌بندی می‌کند: ابتدا سیاست‌های سطح خدمت، سپس هر گزینه به‌ترتیب
 * فهرست واقعی سرویس. هیچ سیاستی با حدس جابجا نمی‌شود.
 */
export function groupPackagingByOption<T extends { optionCode: string | null }>(
  policies: T[],
  options: readonly ShippingServiceOptionChoice[],
): Array<{ optionCode: string | null; policies: T[] }> {
  const groups: Array<{ optionCode: string | null; policies: T[] }> = [
    { optionCode: null, policies: policies.filter((policy) => !policy.optionCode) },
  ];
  for (const option of options) {
    const matching = policies.filter((policy) => policy.optionCode === option.code);
    if (matching.length > 0) groups.push({ optionCode: option.code, policies: matching });
  }
  const known = new Set(options.map((option) => option.code));
  const orphaned = policies.filter((policy) => policy.optionCode && !known.has(policy.optionCode));
  if (orphaned.length > 0) {
    const seen = new Set<string>();
    for (const policy of orphaned) {
      const code = policy.optionCode as string;
      if (seen.has(code)) continue;
      seen.add(code);
      groups.push({ optionCode: code, policies: policies.filter((row) => row.optionCode === code) });
    }
  }
  return groups.filter((group) => group.policies.length > 0);
}

/**
 * کدهای خطای پایدار Shipping → پیام fa مجاور فیلد. کد ناشناخته `fallback` می‌گیرد تا کد خام
 * هرگز در UI دیده نشود.
 */
const PACKAGING_FAILURE_FA: Record<string, string> = {
  "shipping.validation.packaging_fee_negative": "هزینهٔ بسته‌بندی نمی‌تواند منفی باشد.",
  "shipping.validation.packaging_fee_out_of_range": "هزینهٔ بسته‌بندی از سقف مجاز بیشتر است.",
  "shipping.validation.packaging_currency_invalid": "کد ارز باید سه حرف لاتین باشد (مثال: IRR).",
  "shipping.validation.packaging_freight_payer_mode_invalid":
    "طرف پرداخت کرایه خارج از قرارداد است؛ فقط «پیش‌کرایه» یا «پس‌کرایه» پذیرفته می‌شود.",
  "shipping.validation.service_code_required": "کد خدمت الزامی است.",
  "shipping.validation.service_code_too_long": "کد خدمت بیش از حد بلند است.",
  "shipping.validation.scope_required": "دامنهٔ مقصد الزامی است.",
  "shipping.validation.country_required": "کشور مقصد را از فهرست انتخاب کنید.",
  "shipping.validation.service_area_requires_city": "ناحیهٔ خدمت بدون شهر مالک معتبر نیست.",
  "shipping.validation.option_code_too_long": "کد گزینهٔ خدمت بیش از حد بلند است.",
  "shipping.validation.body_service_code_mismatch": "کد خدمت بدنه با کد خدمت مسیر یکسان نیست.",
  "shipping.validation.body_invalid": "بدنهٔ درخواست خوانا نبود؛ دوباره تلاش کنید.",
  "shipping.packaging.fee.negative": "هزینهٔ بسته‌بندی نمی‌تواند منفی باشد.",
  "shipping.packaging.fee.out_of_range": "هزینهٔ بسته‌بندی از سقف مجاز بیشتر است.",
  "shipping.packaging.currency.invalid": "کد ارز باید سه حرف لاتین باشد (مثال: IRR).",
  "shipping.packaging.freight_payer_mode.invalid":
    "طرف پرداخت کرایه خارج از قرارداد است؛ فقط «پیش‌کرایه» یا «پس‌کرایه» پذیرفته می‌شود.",
  "shipping.packaging.policy.not_found": "این سیاست بسته‌بندی پیدا نشد؛ فهرست را دوباره بارگذاری کنید.",
  "shipping.packaging.policy.scope_duplicate":
    "برای همین خدمت/گزینه/دامنه از قبل سیاست بسته‌بندی ثبت شده است؛ همان را ویرایش کنید.",
  "shipping.packaging.policy.option_mismatch": "این سیاست به هویت دیگری تعلق دارد و از این مسیر قابل تغییر نیست.",
  "shipping.packaging.policy.ambiguous":
    "بیش از یک سیاست فعال روی همین خدمت/گزینه/دامنه وجود دارد؛ داده مبهم است و مبلغی اعلام نمی‌شود. یکی را غیرفعال کنید.",
  "shipping.service_code.unknown": "این کد خدمت در کاتالوگ روش‌های ارسال وجود ندارد.",
  "shipping.service_option.unknown": "گزینهٔ انتخابی برای این خدمت شناخته‌شده نیست.",
  "shipping.destination_scope.invalid": "دامنهٔ مقصد نامعتبر است؛ کشور/استان/شهر را از فهرست مرجع انتخاب کنید.",
  "shipping.service_scope.mismatch": "این ردیف به خدمت دیگری تعلق دارد.",
  "admin.authorization.denied": "دسترسی لازم برای مدیریت هزینهٔ بسته‌بندی را ندارید.",
  "admin.actor.missing": "نشست ادمین شناسایی نشد؛ دوباره وارد شوید.",
  "host-unreachable": "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.",
};

export function mapPackagingErrorFa(raw: string | null | undefined, fallback: string): string {
  const code = (raw ?? "").trim();
  return PACKAGING_FAILURE_FA[code] ?? fallback;
}
