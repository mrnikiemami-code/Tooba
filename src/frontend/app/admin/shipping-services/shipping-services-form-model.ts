/**
 * مدل خالص فرم «سرویس ارسال» ادمین.
 *
 * این ماژول هیچ وابستگی React/شبکه ندارد تا هم صفحهٔ `/admin/shipping-services` و هم
 * تست‌های کانونیکال بتوانند بدون شبیه‌سازی مرورگر از آن استفاده کنند.
 *
 * قاعدهٔ طراحی: هر ورودی نامعتبر باید **قبل** از فراخوانی Fulfillment API به پیام روشن تبدیل شود.
 * قبلاً فرم مقادیر ناقص را با `filter` بی‌صدا دور می‌ریخت (نام خالی، کد نوع خالی) و کد سرویس تکراری
 * را به خطای پایگاه‌داده می‌سپرد؛ نتیجه، ذخیرهٔ ناقص یا خطای مبهم بود.
 */
import type { ShippingServiceWriteRequest } from "../shipping-services-api.ts";

/** حداکثر طول کد سرویس/نوع مطابق ستون‌های `shipping_service_options.code` و `shipping_services.code`. */
export const SHIPPING_SERVICE_CODE_MAX_LENGTH = 64;

/** حداکثر طول نام نمایشی مطابق `shipping_service_translations.name`. */
export const SHIPPING_SERVICE_NAME_MAX_LENGTH = 128;

/** کدها lowercase و snake_case هستند: کد متد فروشگاهی از `service:option` ساخته می‌شود. */
const CODE_PATTERN = /^[a-z0-9_]+$/;

const CODE_REQUIRED_FA = "کد سرویس الزامی است.";
const CODE_FORMAT_FA =
  "کد سرویس فقط می‌تواند حروف کوچک انگلیسی، عدد و زیرخط (_) باشد و نباید «:» داشته باشد.";
const CODE_TOO_LONG_FA = `کد سرویس نمی‌تواند بیش از ${SHIPPING_SERVICE_CODE_MAX_LENGTH} کاراکتر باشد.`;
const CODE_DUPLICATE_FA = "این کد سرویس قبلاً ثبت شده است؛ کد دیگری انتخاب کنید.";
const SORT_ORDER_FA = "ترتیب باید یک عدد صحیح و نامنفی باشد.";
const SERVICE_NAME_FA =
  "نام نمایشی برای زبان فعال الزامی است؛ بدون آن نام سرویس در فروشگاه با کد آن نمایش داده می‌شود.";
const SERVICE_NAME_TOO_LONG_FA = `نام نمایشی نمی‌تواند بیش از ${SHIPPING_SERVICE_NAME_MAX_LENGTH} کاراکتر باشد.`;
const OPTION_CODE_FA = "کد نوع سرویس (سطح ۲) الزامی است.";
const OPTION_NAME_FA = "نام نوع سرویس (سطح ۲) برای زبان فعال الزامی است.";
const OPTION_CODE_DUPLICATE_FA = "کد این نوع سرویس تکراری است.";

/** فیلد فرمی که یک ایراد اعتبارسنجی به آن نسبت داده می‌شود. */
export type ShippingServiceFormIssueField =
  | "code"
  | "codeDuplicate"
  | "sortOrder"
  | "serviceName"
  | "optionCode"
  | "optionName";

/** یک ایراد اعتبارسنجی؛ `optionIndex` فقط برای ایرادهای سطح نوع سرویس پر می‌شود. */
export type ShippingServiceFormIssue = {
  field: ShippingServiceFormIssueField;
  message: string;
  optionIndex?: number;
};

export type ShippingServiceFormTranslation = {
  languageId: string;
  name: string;
  description?: string | null;
};

export type ShippingServiceFormOption = {
  shippingServiceOptionId?: string | null;
  code: string;
  isActive: boolean;
  sortOrder: number;
  translations: { languageId: string; name: string }[];
};

/** تمام مقادیر قابل ویرایش فرم. */
export type ShippingServiceFormFields = {
  code: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  isActive: boolean;
  sortOrder: string;
  translations: ShippingServiceFormTranslation[];
  options: ShippingServiceFormOption[];
};

/** زمینهٔ اعتبارسنجی: زبان فعال و کدهای سرویس دیگری که در فهرست وجود دارند. */
export type ShippingServiceFormContext = {
  activeLanguageId: string;
  otherServiceCodes: readonly string[];
};

/** کد سرویس/نوع را به شکل canonical بک‌اند (`Trim().ToLowerInvariant()`) نرمال می‌کند. */
export function normalizeShippingServiceCode(value: string | null | undefined): string {
  return (value ?? "").trim().toLowerCase();
}

/**
 * ترتیب را از ورودی متنی می‌خواند؛ مقدار نامعتبر `null` می‌دهد تا تبدیل بی‌صدای `|| 0` رخ ندهد.
 */
export function parseShippingServiceSortOrder(value: string | null | undefined): number | null {
  const trimmed = (value ?? "").trim();
  if (!/^\d+$/.test(trimmed)) {
    return null;
  }

  const parsed = Number(trimmed);
  return Number.isSafeInteger(parsed) ? parsed : null;
}

/** نام نمایشی زبان فعال را از لیست ترجمه‌ها بیرون می‌کشد. */
export function activeTranslationName(
  translations: readonly ShippingServiceFormTranslation[],
  languageId: string,
): string {
  const found = translations.find((row) => row.languageId === languageId);
  return (found?.name ?? "").trim();
}

/** نام نوع سرویس برای زبان فعال. */
export function activeOptionTranslationName(
  option: ShippingServiceFormOption,
  languageId: string,
): string {
  const found = option.translations.find((row) => row.languageId === languageId);
  return (found?.name ?? "").trim();
}

/**
 * فرم را پیش از ارسال بررسی می‌کند. خروجی خالی یعنی فرم معتبر است.
 * ترتیب ایرادها پایدار است (سرویس، سپس نوع‌های سرویس به ترتیب ردیف).
 */
export function validateShippingServiceForm(
  input: ShippingServiceFormFields,
  context: ShippingServiceFormContext,
): ShippingServiceFormIssue[] {
  const issues: ShippingServiceFormIssue[] = [];

  const code = normalizeShippingServiceCode(input.code);
  if (code.length === 0) {
    issues.push({ field: "code", message: CODE_REQUIRED_FA });
  } else if (code.length > SHIPPING_SERVICE_CODE_MAX_LENGTH) {
    issues.push({ field: "code", message: CODE_TOO_LONG_FA });
  } else if (!CODE_PATTERN.test(code)) {
    issues.push({ field: "code", message: CODE_FORMAT_FA });
  } else {
    const taken = (context.otherServiceCodes ?? []).map(normalizeShippingServiceCode);
    if (taken.includes(code)) {
      issues.push({ field: "codeDuplicate", message: CODE_DUPLICATE_FA });
    }
  }

  if (parseShippingServiceSortOrder(input.sortOrder) === null) {
    issues.push({ field: "sortOrder", message: SORT_ORDER_FA });
  }

  const serviceName = activeTranslationName(input.translations, context.activeLanguageId);
  if (serviceName.length === 0) {
    issues.push({ field: "serviceName", message: SERVICE_NAME_FA });
  } else if (serviceName.length > SHIPPING_SERVICE_NAME_MAX_LENGTH) {
    issues.push({ field: "serviceName", message: SERVICE_NAME_TOO_LONG_FA });
  }

  const seenOptionCodes = new Set<string>();
  input.options.forEach((option, optionIndex) => {
    const optionCode = normalizeShippingServiceCode(option.code);
    if (optionCode.length === 0) {
      issues.push({ field: "optionCode", optionIndex, message: OPTION_CODE_FA });
    } else if (!CODE_PATTERN.test(optionCode) || optionCode.length > SHIPPING_SERVICE_CODE_MAX_LENGTH) {
      issues.push({ field: "optionCode", optionIndex, message: CODE_FORMAT_FA });
    } else if (seenOptionCodes.has(optionCode)) {
      issues.push({ field: "optionCode", optionIndex, message: OPTION_CODE_DUPLICATE_FA });
    } else {
      seenOptionCodes.add(optionCode);
    }

    if (activeOptionTranslationName(option, context.activeLanguageId).length === 0) {
      issues.push({ field: "optionName", optionIndex, message: OPTION_NAME_FA });
    }
  });

  return issues;
}

/**
 * بدنهٔ ارسال به Fulfillment API را از فرم معتبر می‌سازد.
 * فقط ترجمه‌های بدون نام حذف می‌شوند (ترجمهٔ ناقص مجاز است)؛ این تابع پس از
 * <see cref="validateShippingServiceForm"/> صدا زده می‌شود، پس حذف بی‌صدای مقدار نامعتبر رخ نمی‌دهد.
 */
export function buildShippingServiceWriteRequest(
  input: ShippingServiceFormFields,
): ShippingServiceWriteRequest {
  return {
    code: normalizeShippingServiceCode(input.code),
    providerKind: input.providerKind.trim(),
    iconKey: input.iconKey.trim(),
    colorKey: input.colorKey.trim(),
    isActive: input.isActive,
    sortOrder: parseShippingServiceSortOrder(input.sortOrder) ?? 0,
    translations: input.translations
      .filter((row) => row.languageId)
      .map((row) => ({
        languageId: row.languageId,
        name: row.name.trim(),
        description: row.description?.trim() ? row.description.trim() : null,
      }))
      .filter((row) => row.name.length > 0),
    options: input.options.map((option) => ({
      shippingServiceOptionId: option.shippingServiceOptionId ?? null,
      code: normalizeShippingServiceCode(option.code),
      isActive: option.isActive,
      sortOrder: option.sortOrder,
      translations: option.translations
        .filter((row) => row.languageId)
        .map((row) => ({ languageId: row.languageId, name: row.name.trim() }))
        .filter((row) => row.name.length > 0),
    })),
  };
}

const KNOWN_FAILURE_FA: Record<string, string> = {
  "shipping_service.code_duplicate": CODE_DUPLICATE_FA,
  "shipping_service.not_found": "این سرویس ارسال دیگر وجود ندارد؛ فهرست را دوباره بارگذاری کنید.",
};

/**
 * کد خطای پایدار Fulfillment را به پیام fa کاربرپسند نگاشت می‌کند.
 * برای کد ناشناخته، `fallback` برگردانده می‌شود تا کد خام هرگز در UI دیده نشود.
 */
export function mapShippingServiceErrorFa(
  raw: string | null | undefined,
  fallback: string,
): string {
  const code = (raw ?? "").trim();
  return KNOWN_FAILURE_FA[code] ?? fallback;
}
