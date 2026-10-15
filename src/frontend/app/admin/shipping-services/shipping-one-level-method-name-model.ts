/**
 * مدل ناب ویرایش نام روش یک‌سطحی — بدون وابستگی به React.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-094
 *
 * دامنهٔ این مدل عمداً فقط «نام نمایشی یک روش در یک زبان» است:
 *
 * - هویت روش همان `methodCode` canonical است و در هیچ مسیری از این مدل تغییر نمی‌کند؛
 * - هیچ سطح دوم (provider → option)، هیچ گروه‌بندی ارائه‌دهنده و هیچ ستون تعداد گزینه وجود ندارد؛
 * - فعال/غیرفعال بودن روش هرگز از این مسیر نوشته نمی‌شود؛
 * - برچسب فقط از دادهٔ واقعی ترجمه می‌آید و مقدار جعلی ساخته نمی‌شود.
 */

import { sanitizeLanguageId, sanitizeMethodCode, type OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** حداکثر طول نام، هم‌اندازهٔ ستون پایدار `shipping_method_translations.name` (varchar(128)). */
export const ONE_LEVEL_METHOD_NAME_MAX_LENGTH = 128;

/** مسیر تنها نوشتن مجاز این مسیر: نام یک روش در یک زبان. کد روش و زبان هر دو در URL هستند. */
export function oneLevelMethodTranslationPath(methodCode: string, languageId: string): string {
  const code = sanitizeMethodCode(methodCode);
  const language = sanitizeLanguageId(languageId);
  return `/v1/admin/shipping-services/shipping-methods/one-level/${encodeURIComponent(code)}/translations/${language}`;
}

/** ایراد اعتبارسنجی نام؛ `null` یعنی نام معتبر است. */
export type OneLevelMethodNameIssue = "required" | "too_long" | null;

/** نام را به‌صورت ناب بررسی می‌کند: خالی/فقط‌فاصله و بلندتر از عرض ستون هر دو رد می‌شوند. */
export function validateOneLevelMethodName(name: unknown): OneLevelMethodNameIssue {
  const trimmed = typeof name === "string" ? name.trim() : "";
  if (trimmed.length === 0) return "required";
  if (trimmed.length > ONE_LEVEL_METHOD_NAME_MAX_LENGTH) return "too_long";
  return null;
}

/** متن فارسی ایراد نام؛ فقط برای نمایش، نه برای هویت خطا. */
export function oneLevelMethodNameIssueFa(issue: OneLevelMethodNameIssue): string {
  if (issue === "required") return "نام روش نمی‌تواند خالی باشد.";
  if (issue === "too_long") return `نام روش حداکثر ${ONE_LEVEL_METHOD_NAME_MAX_LENGTH} نویسه است.`;
  return "";
}

/** مقدار پیش‌پر فرم برای یک روش/زبان، فقط از ترجمهٔ واقعی همان زبان؛ در نبود آن خالی. */
export function initialMethodName(method: OneLevelMethodRow | undefined, languageId: string): string {
  const language = sanitizeLanguageId(languageId);
  if (!method || !language) return "";
  return (method.translations.find((t) => t.languageId === language)?.name ?? "").trim();
}

/** روش را از فهرست سرور با کد canonical پیدا می‌کند. */
export function findMethodRow(
  methods: readonly OneLevelMethodRow[],
  methodCode: string,
): OneLevelMethodRow | undefined {
  const code = sanitizeMethodCode(methodCode);
  if (!code) return undefined;
  return methods.find((m) => sanitizeMethodCode(m.methodCode) === code);
}

/**
 * نام ذخیره‌شدهٔ تأییدشدهٔ سرور را روی همان ردیف/زبان اعمال می‌کند.
 *
 * قاعدهٔ سخت: هیچ ردیف تازه‌ای ساخته نمی‌شود و هیچ فیلد هویتی (کد روش، فعال‌بودن) تغییر نمی‌کند.
 * اگر ردیف یا زبان در فهرست فعلی نباشد، فهرست بی‌تغییر برگردانده می‌شود تا «موفقیت جعلی» نسازیم.
 */
export function applySavedMethodName(
  methods: readonly OneLevelMethodRow[],
  methodCode: string,
  languageId: string,
  name: string,
): readonly OneLevelMethodRow[] {
  const code = sanitizeMethodCode(methodCode);
  const language = sanitizeLanguageId(languageId);
  if (!code || !language) return methods;

  let touched = false;
  const next = methods.map((method) => {
    if (sanitizeMethodCode(method.methodCode) !== code) return method;
    touched = true;
    const others = method.translations.filter((t) => t.languageId !== language);
    return {
      ...method,
      translations: [...others, { languageId: language, name, description: null }],
    };
  });
  return touched ? next : methods;
}

/**
 * نگهبان نسل برای جلوگیری از «نشستن نتیجهٔ ذخیرهٔ روش قبلی روی انتخاب جدید».
 *
 * هر بار `begin` یک نسل تازه می‌سازد؛ تنها نسل آخر حق اعمال نتیجه را دارد. این همان الگوی
 * موجود در زبانهٔ «تعرفه و پوشش» است تا رفتار در کل شل یکسان بماند.
 */
export function createMethodNameGenerationGuard(): { begin: () => () => boolean } {
  let latest = 0;
  return {
    begin() {
      const generation = ++latest;
      return () => generation === latest;
    },
  };
}

/** کلید پایدار یک هدف ویرایش (روش + زبان) برای تشخیص «هدف عوض شده». */
export function methodNameTargetKey(methodCode: string, languageId: string): string {
  return `${sanitizeMethodCode(methodCode)}|${sanitizeLanguageId(languageId)}`;
}

/**
 * زبان مؤثر فرم ویرایش.
 *
 * فهرست زبان‌ها نامتقارن بارگذاری می‌شود: دیالوگ ممکن است پیش از رسیدن زبان‌ها mount شود یا
 * زبان پیش‌فرض سرور بعداً برسد. اگر زبان انتخاب‌شده در فهرست فعلی نباشد، زبان پیش‌فرض مؤثر
 * برمی‌گردد؛ اگر آن هم در فهرست نباشد، نخستین زبان معتبر فهرست. در فهرست خالی، خالی برمی‌گردد و
 * ویرایش غیرفعال می‌ماند (fail-closed) — هیچ زبان جعلی ساخته نمی‌شود.
 */
export function resolveEffectiveLanguageId(
  options: readonly { languageId: string }[],
  preferredLanguageId: string,
  selectedLanguageId: string,
): string {
  const valid = options.map((row) => sanitizeLanguageId(row.languageId)).filter((id) => id.length > 0);
  const selected = sanitizeLanguageId(selectedLanguageId);
  if (selected && valid.includes(selected)) return selected;
  const preferred = sanitizeLanguageId(preferredLanguageId);
  if (preferred && valid.includes(preferred)) return preferred;
  return valid[0] ?? "";
}
