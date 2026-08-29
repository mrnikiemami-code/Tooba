/**
 * نگاشت پایدار کد خطای Admin به پیام انسان‌خوان fa/en.
 * متن خام HTTP / Bad Request / stack هرگز به UI برنمی‌گردد.
 */

export type AdminErrorLocale = "fa" | "en";

const UNKNOWN_FA = "خطایی رخ داد. لطفاً دوباره تلاش کنید.";
const UNKNOWN_EN = "Something went wrong. Please try again.";

/** پیام‌های شناخته‌شده — کلید = errorCode پایدار Host. */
const ADMIN_ERROR_MESSAGES: Record<string, { fa: string; en: string }> = {
  "catalog.attribute.name.duplicate": {
    fa: "ویژگی‌ای با این نام قبلاً وجود دارد.",
    en: "An attribute with this name already exists.",
  },
  "catalog.attribute.code.duplicate": {
    fa: "این کد ویژگی قبلاً استفاده شده است.",
    en: "This attribute code is already in use.",
  },
  "catalog.attribute.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.category.attribute.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.category.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.category.slug.duplicate": {
    fa: "این نامک برای یک دسته‌بندی دیگر استفاده شده است. یک نامک متفاوت انتخاب کنید.",
    en: "This slug is already used by another category. Choose a different slug.",
  },
  "catalog.product.category.level.invalid": {
    fa: "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
    en: "A product must be assigned to a level-3 category.",
  },
  "workspace.product.category.level.invalid": {
    fa: "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
    en: "A product must be assigned to a level-3 category.",
  },
  "workspace.product.category.invalid": {
    fa: "ردهٔ انتخاب‌شده معتبر نیست.",
    en: "The selected category is not valid.",
  },
  "workspace.product.category.schema-impact": {
    fa: "تغییر دسته بر ویژگی‌ها یا تنوع‌ها اثر می‌گذارد؛ تأیید صریح لازم است.",
    en: "Changing category affects attributes or variants; explicit confirmation is required.",
  },
  "workspace.product.category.assign.rejected": {
    fa: "اختصاص دسته انجام نشد.",
    en: "Category assignment was rejected.",
  },
  "workspace.product.category-failed": {
    fa: "اختصاص دسته انجام نشد.",
    en: "Category assignment failed.",
  },
  "workspace.product.brand.invalid": {
    fa: "برند انتخاب‌شده معتبر نیست.",
    en: "The selected brand is not valid.",
  },
  "workspace.product.brand-failed": {
    fa: "انتساب برند انجام نشد.",
    en: "Brand assignment failed.",
  },
  "workspace.catalog.stale": {
    fa: "این مورد را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.",
    en: "Someone else changed this record. Reload the latest version.",
  },
  "workspace.permission.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "workspace.product.missing": {
    fa: "محصول یافت نشد.",
    en: "Product was not found.",
  },
  "workspace.host.unreachable": {
    fa: "اتصال به سرویس برقرار نیست. لطفاً دوباره تلاش کنید.",
    en: "Could not reach the service. Please try again.",
  },
  "workspace.product.create-failed": {
    fa: "ایجاد محصول ناموفق بود.",
    en: "Product creation failed.",
  },
  "workspace.product.core-failed": {
    fa: "ذخیرهٔ اطلاعات محصول ناموفق بود.",
    en: "Saving product details failed.",
  },
  "admin.authorization.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "host-unreachable": {
    fa: "اتصال به سرویس برقرار نیست. لطفاً دوباره تلاش کنید.",
    en: "Could not reach the service. Please try again.",
  },
  "seller.authorization.denied": {
    fa: "دسترسی مجاز نیست.",
    en: "You are not allowed to perform this action.",
  },
  "seller.identity.missing": {
    fa: "شناسه فروشنده مشخص نیست.",
    en: "Seller identity is missing.",
  },
};

const TECHNICAL_UI_PATTERNS = [
  /\bbad\s*request\b/i,
  /\binternal\s*server\s*error\b/i,
  /\bnot\s*found\b/i,
  /\bforbidden\b/i,
  /\bunauthorized\b/i,
  /\bHTTP\s*[0-9]{3}\b/i,
  /\bstatus\s*[:=]?\s*[0-9]{3}\b/i,
  /\bException\b/,
  /\bStackTrace\b/i,
  /\bat\s+\w+\.\w+/,
  /^\s*\{[\s\S]*\}\s*$/,
];

/** آیا متن برای نمایش عادی UI خطرناک/فنی است؟ */
export function isTechnicalAdminErrorText(raw: string): boolean {
  const t = raw.trim();
  if (!t) return true;
  if (/^admin\.http\.\d+$/i.test(t)) return true;
  if (/^host-grid-http-\d+$/i.test(t)) return true;
  if (/^host-.*-http-\d+$/i.test(t)) return true;
  return TECHNICAL_UI_PATTERNS.some((re) => re.test(t));
}

/**
 * استخراج کد پایدار از پیام/بدنه — اگر کد شناخته‌شده در متن باشد همان را برمی‌گرداند.
 */
export function extractAdminErrorCode(raw: string | null | undefined): string | null {
  if (!raw) return null;
  const trimmed = raw.trim();
  if (!trimmed) return null;
  if (ADMIN_ERROR_MESSAGES[trimmed]) return trimmed;
  const paren = trimmed.match(/\(([a-z0-9]+(?:\.[a-z0-9_-]+)+)\)\s*$/i);
  if (paren?.[1] && ADMIN_ERROR_MESSAGES[paren[1]]) return paren[1];
  const dotted = trimmed.match(/\b([a-z][a-z0-9_-]*(?:\.[a-z0-9_-]+)+)\b/i);
  if (dotted?.[1] && ADMIN_ERROR_MESSAGES[dotted[1]]) return dotted[1];
  if (trimmed === "host-unreachable" || trimmed.startsWith("workspace.host.")) {
    return "host-unreachable";
  }
  return null;
}

/**
 * نگاشت کد/پیام خام به متن کاربرپسند برای locale فعلی.
 * کد خام، Bad Request، HTTP status و stack هرگز برنمی‌گردند.
 */
export function mapAdminErrorMessage(
  raw: string | null | undefined,
  locale: AdminErrorLocale = "fa",
): string {
  const code = extractAdminErrorCode(raw);
  if (code && ADMIN_ERROR_MESSAGES[code]) {
    return ADMIN_ERROR_MESSAGES[code]![locale];
  }
  const text = (raw ?? "").trim();
  if (text && !isTechnicalAdminErrorText(text) && !/^[a-z0-9]+(?:\.[a-z0-9_-]+)+$/i.test(text)) {
    return text;
  }
  return locale === "en" ? UNKNOWN_EN : UNKNOWN_FA;
}

/** پیام ناشناختهٔ استاندارد. */
export function unknownAdminErrorMessage(locale: AdminErrorLocale = "fa"): string {
  return locale === "en" ? UNKNOWN_EN : UNKNOWN_FA;
}

/** فهرست کدهای شناخته‌شده (برای تست). */
export function listMappedAdminErrorCodes(): string[] {
  return Object.keys(ADMIN_ERROR_MESSAGES);
}
