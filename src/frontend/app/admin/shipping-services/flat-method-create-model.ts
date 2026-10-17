/**
 * مدل خالص فرم «سرویس جدید» برای کاتالوگ canonical یک‌سطحی.
 *
 * TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167
 *
 * قواعد سخت (بدون React، بدون fetch):
 * - تنها هویت روش، کد canonical است؛ UI هرگز `serviceCode`/`optionCode` نمی‌سازد و پیوند قدیمی را
 *   از متن کد تقسیم نمی‌کند. پیوند در سرور از همان کد مشتق و اعتبارسنجی می‌شود.
 * - نام‌ها فقط برای زبان‌هایی جمع می‌شوند که رجیستری واقعی سرور برگردانده است؛ هیچ زبان یا ترجمهٔ
 *   جعلی ساخته نمی‌شود.
 * - زبان/نام نامعتبر پیش از ارسال رد می‌شود تا هیچ درخواست ناقصی به سرور نرود؛ با این حال سرور
 *   همچنان مرجع نهایی است.
 */

/** سقف طول کد روش؛ هم‌اندازهٔ محدودیت canonical سرور. */
export const MAX_METHOD_CODE_LENGTH = 32;

/** سقف طول نام نمایشی؛ هم‌اندازهٔ ستون پایدار ترجمه. */
export const MAX_METHOD_NAME_LENGTH = 128;

/** شکل کد مجاز: یک یا دو بخش با حروف/رقم/زیرخط و حداکثر یک جداکنندهٔ «:». */
const METHOD_CODE_PATTERN = /^[A-Za-z0-9_]+(:[A-Za-z0-9_]+)?$/;

/** شکل metadata ارائه‌دهنده؛ فقط حروف کوچک/رقم/زیرخط. */
const PROVIDER_KIND_PATTERN = /^[a-z0-9_]{1,64}$/;

export type CreateMethodLanguageName = { languageId: string; name: string };

export type CreateMethodInput = {
  methodCode: string;
  providerKind: string;
  names: readonly CreateMethodLanguageName[];
};

export type CreateMethodRequest = {
  methodCode: string;
  providerKind?: string;
  translations: { languageId: string; name: string }[];
};

export type CreateMethodValidation =
  | { ok: true; request: CreateMethodRequest }
  | { ok: false; field: "code" | "provider" | "name"; message: string };

/**
 * ورودی فرم را به بدنهٔ درخواست تبدیل می‌کند یا خطای قابل‌نمایش می‌دهد.
 *
 * نام‌های خالی حذف می‌شوند (فقط زبان‌هایی که واقعاً نام دارند فرستاده می‌شوند) و حداقل یک نام
 * غیرخالی الزامی است؛ کد با فاصله‌های اطراف پاک می‌شود اما هرگز تغییر شکل نمی‌دهد.
 */
export function validateCreateMethodInput(input: CreateMethodInput): CreateMethodValidation {
  const methodCode = input.methodCode.trim();
  if (methodCode.length === 0) {
    return { ok: false, field: "code", message: "کد روش را وارد کنید." };
  }
  if (methodCode.length > MAX_METHOD_CODE_LENGTH) {
    return {
      ok: false,
      field: "code",
      message: `کد روش حداکثر ${MAX_METHOD_CODE_LENGTH} نویسه است.`,
    };
  }
  if (!METHOD_CODE_PATTERN.test(methodCode)) {
    return {
      ok: false,
      field: "code",
      message: "کد روش فقط می‌تواند حروف/رقم/زیرخط و حداکثر یک «:» داشته باشد (مثل tipax:sameday).",
    };
  }

  const providerKind = input.providerKind.trim();
  if (providerKind.length > 0 && !PROVIDER_KIND_PATTERN.test(providerKind)) {
    return {
      ok: false,
      field: "provider",
      message: "شناسهٔ ارائه‌دهنده فقط حروف کوچک انگلیسی/رقم/زیرخط می‌پذیرد.",
    };
  }

  const translations: { languageId: string; name: string }[] = [];
  const seen = new Set<string>();
  for (const entry of input.names) {
    const languageId = entry.languageId.trim();
    const name = entry.name.trim();
    if (!languageId || seen.has(languageId)) continue;
    if (name.length === 0) continue;
    if (name.length > MAX_METHOD_NAME_LENGTH) {
      return {
        ok: false,
        field: "name",
        message: `نام روش حداکثر ${MAX_METHOD_NAME_LENGTH} نویسه است.`,
      };
    }
    seen.add(languageId);
    translations.push({ languageId, name });
  }

  if (translations.length === 0) {
    return { ok: false, field: "name", message: "نام روش را حداقل برای یک زبان ثبت کنید." };
  }

  return {
    ok: true,
    request: {
      methodCode,
      ...(providerKind.length > 0 ? { providerKind } : {}),
      translations,
    },
  };
}

/** روش ساخته‌شده بر اساس پاسخ تأییدشدهٔ سرور؛ نبود هویت پایدار ⇒ `null` (پاسخ نامعتبر). */
export type CreatedMethod = {
  methodCode: string;
  providerKind: string;
  legacyServiceCode: string;
  legacyOptionCode: string | null;
  isActive: boolean;
  sortOrder: number;
  translations: { languageId: string; name: string }[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function pick(row: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (row[key] !== undefined) return row[key];
  }
  return undefined;
}

function text(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

/** پاسخ سرور را محافظه‌کارانه نگاشت می‌کند؛ هر نبود هویت/ناسازگاری ⇒ `null`. */
export function mapCreatedMethod(payload: unknown): CreatedMethod | null {
  const row = asRecord(payload);
  if (!row) return null;
  const methodCode = text(pick(row, "methodCode", "MethodCode"));
  if (!methodCode) return null;
  const rawTranslations = pick(row, "translations", "Translations");
  const translations = Array.isArray(rawTranslations)
    ? rawTranslations.flatMap((item) => {
      const entry = asRecord(item);
      if (!entry) return [];
      const languageId = text(pick(entry, "languageId", "LanguageId"));
      const name = text(pick(entry, "name", "Name"));
      if (!languageId || !name) return [];
      return [{ languageId, name }];
    })
    : [];
  const optionCode = text(pick(row, "legacyOptionCode", "LegacyOptionCode"));
  return {
    methodCode,
    providerKind: text(pick(row, "providerKind", "ProviderKind")),
    legacyServiceCode: text(pick(row, "legacyServiceCode", "LegacyServiceCode")),
    legacyOptionCode: optionCode.length > 0 ? optionCode : null,
    isActive: pick(row, "isActive", "IsActive") === true,
    sortOrder: Number(pick(row, "sortOrder", "SortOrder") ?? 0) || 0,
    translations,
  };
}

/** پیام فارسی پایدار برای کد خطای سرور؛ کد ناشناخته با پیام عمومی نمایش داده می‌شود. */
export function createMethodErrorMessage(errorCode: string, status: number): string {
  switch (errorCode) {
    case "shipping_method.code.duplicate":
      return "روشی با این کد از قبل وجود دارد. کد دیگری انتخاب کنید.";
    case "shipping_method.binding.duplicate":
      return "این پیوند قدیمی در اختیار روش دیگری است؛ کد روش دیگری انتخاب کنید.";
    case "shipping_method.code.invalid":
      return "کد روش canonical نیست یا بیش از حد بلند است.";
    case "shipping_method.name.required":
      return "نام روش برای حداقل یک زبان الزامی است.";
    case "shipping_service.language_invalid":
      return "زبان انتخاب‌شده در رجیستری زبان‌ها وجود ندارد.";
    case "admin.authorization.denied":
      return "برای ساخت روش ارسال مجوز ندارید.";
    default:
      return status >= 500
        ? "سرور در ساخت روش خطا داد؛ هیچ روشی ساخته نشد."
        : "ساخت روش ناموفق بود؛ هیچ روشی ساخته نشد.";
  }
}
