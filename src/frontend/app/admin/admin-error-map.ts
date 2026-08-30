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
  "catalog.schema.invalid": {
    fa: "تنظیم ویژگی برای این دسته معتبر نیست.",
    en: "This attribute setting is not valid for the category.",
  },
  "catalog.facet.invalid": {
    fa: "تنظیم فیلتر معتبر نیست. ویژگی باید برای این دسته قابل فیلتر باشد.",
    en: "Filter settings are not valid. The attribute must be filterable for this category.",
  },
  "catalog.facet.missing": {
    fa: "تنظیم فیلتر یافت نشد.",
    en: "Filter settings were not found.",
  },
  "catalog.category.invalid": {
    fa: "اطلاعات واردشده معتبر نیست.",
    en: "The submitted information is not valid.",
  },
  "catalog.category.assignment.duplicate": {
    fa: "این دسته قبلاً به محصول اضافه شده است.",
    en: "This category is already assigned to the product.",
  },
  "catalog.category.assignment.duplicate_primary": {
    fa: "این دسته هم‌اکنون دسته اصلی محصول است.",
    en: "This category is already the product's primary category.",
  },
  "catalog.category.assignment.cannot_remove_primary": {
    fa: "دسته اصلی را نمی‌توان مستقیم حذف کرد؛ ابتدا دسته اصلی دیگری انتخاب کنید.",
    en: "The primary category cannot be removed directly; choose another primary first.",
  },
  "catalog.category.assignment.missing": {
    fa: "پیوند دسته برای این محصول یافت نشد.",
    en: "Category assignment was not found for this product.",
  },
  "catalog.category.assignment.invalid": {
    fa: "اختصاص دسته معتبر نیست.",
    en: "Category assignment is not valid.",
  },
  "catalog.category.assignment.stale": {
    fa: "این مورد را کاربر دیگری تغییر داده است. نسخهٔ تازه را بارگذاری کنید.",
    en: "Someone else changed this record. Reload the latest version.",
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
  "catalog.variant.invalid": {
    fa: "تنظیم تنوع معتبر نیست.",
    en: "Variant settings are not valid.",
  },
  "catalog.variant.preview.invalid": {
    fa: "پیش‌نمایش تنوع‌ها معتبر نیست.",
    en: "Variant preview is not valid.",
  },
  "catalog.variant.apply.invalid": {
    fa: "اعمال تنوع‌ها انجام نشد.",
    en: "Could not apply variant changes.",
  },
  "catalog.variant.readiness.invalid": {
    fa: "وضعیت آمادگی تنوع‌ها معتبر نیست.",
    en: "Variant readiness is not valid.",
  },
  "catalog.variant_axes.invalid": {
    fa: "ویژگی تنوع انتخاب‌شده معتبر نیست.",
    en: "The selected variant attribute is not valid.",
  },
  "catalog.variant.editor.parse": {
    fa: "بارگذاری ویرایشگر تنوع‌ها ناموفق بود.",
    en: "Could not load the variant editor.",
  },
  "catalog.variant.preview.parse": {
    fa: "پیش‌نمایش تنوع‌ها ناموفق بود.",
    en: "Could not preview variants.",
  },
  "catalog.variant.apply.parse": {
    fa: "ذخیره تنوع‌ها ناموفق بود.",
    en: "Could not save variants.",
  },
  "workspace.variant.axes.missing": {
    fa: "ویژگی تنوع انتخاب نشده است.",
    en: "No variant attribute is selected.",
  },
  "workspace.variant.create.rejected": {
    fa: "ساخت تنوع انجام نشد.",
    en: "Variant creation was rejected.",
  },
  "workspace.variant.missing": {
    fa: "تنوع یافت نشد.",
    en: "Variant was not found.",
  },
  "workspace.variant.status.invalid": {
    fa: "وضعیت تنوع معتبر نیست.",
    en: "Variant status is not valid.",
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
  "media.type.unsupported": {
    fa: "نوع فایل رسانه پشتیبانی نمی‌شود. فقط تصویر JPEG، PNG، WebP یا GIF مجاز است.",
    en: "This media file type is not supported. Only JPEG, PNG, WebP, or GIF images are allowed.",
  },
  "media.too_large": {
    fa: "حجم فایل از سقف مجاز بیشتر است.",
    en: "The file exceeds the allowed size limit.",
  },
  "media.upload.failed": {
    fa: "بارگذاری رسانه ناموفق بود. لطفاً دوباره تلاش کنید.",
    en: "Media upload failed. Please try again.",
  },
  "media.missing": {
    fa: "رسانه یافت نشد.",
    en: "Media was not found.",
  },
  "media.storage.unavailable": {
    fa: "ذخیره‌سازی رسانه در دسترس نیست. لطفاً بعداً دوباره تلاش کنید.",
    en: "Media storage is unavailable. Please try again later.",
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

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function textProp(item: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = item[key];
    if (typeof value === "string" && value.trim()) return value.trim();
  }
  return "";
}

/**
 * مسیر واحد نرمال‌سازی خطای Admin از payload پاسخ Host / fetch.
 * فقط کد پایدار یا نشانگر HTTP داخلی؛ هرگز title خام مثل Bad Request.
 */
export function parseAdminProblemErrorCode(payload: unknown, status: number): string {
  if (status === 401 || status === 403) return "admin.authorization.denied";
  const item = recordOf(payload);
  if (item) {
    const code = textProp(item, "errorCode", "ErrorCode", "code", "Code");
    if (code) return code;
    const extensions = recordOf(item.extensions ?? item.Extensions);
    if (extensions) {
      const nested = textProp(extensions, "errorCode", "ErrorCode", "code", "Code");
      if (nested) return nested;
    }
  }
  if (status <= 0) return "host-unreachable";
  return `admin.http.${status}`;
}

/**
 * نرمال‌سازی کامل برای toast/فرم: کد پایدار → پیام انسان‌خوان locale.
 */
export function normalizeAdminClientError(
  payload: unknown,
  status: number,
  locale: AdminErrorLocale = "fa",
): string {
  return mapAdminErrorMessage(parseAdminProblemErrorCode(payload, status), locale);
}
