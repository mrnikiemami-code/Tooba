/**
 * هستهٔ locale ویترین.
 * locale ≠ market ≠ currency — ابعاد جدا؛ هیچ‌کدام دیگری را تعیین نمی‌کند.
 */

export const LOCALES = ["fa", "en"] as const;
export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "fa";

/** نام کوکی پایدار برای انتخاب locale نمایش. */
export const LOCALE_COOKIE_NAME = "tooba_locale";

export type TextDirection = "rtl" | "ltr";

/** جهت نوشتار از locale؛ مستقل از market/currency. */
export function dirForLocale(locale: Locale): TextDirection {
  return locale === "fa" ? "rtl" : "ltr";
}

/** مقدار attribute lang روی html. */
export function langForLocale(locale: Locale): string {
  return locale === "fa" ? "fa" : "en";
}

/** locale برای Open Graph (BCP-47 با underscore به سبک OG). */
export function openGraphLocaleFor(locale: Locale): string {
  return locale === "fa" ? "fa_IR" : "en_US";
}

export function isLocale(value: unknown): value is Locale {
  return value === "fa" || value === "en";
}

/** پارس امن؛ مقدار نامعتبر → پیش‌فرض فارسی. */
export function parseLocale(value: string | null | undefined): Locale {
  if (value == null) return DEFAULT_LOCALE;
  const normalized = value.trim().toLowerCase();
  if (normalized === "fa" || normalized.startsWith("fa-") || normalized.startsWith("fa_")) return "fa";
  if (normalized === "en" || normalized.startsWith("en-") || normalized.startsWith("en_")) return "en";
  return DEFAULT_LOCALE;
}

/**
 * اثبات جداسازی ابعاد تجاری: locale جهت/زبان است، نه ارز و نه بازار.
 * تست واحد این قرارداد را قفل می‌کند؛ runtime ویترین نباید currency را از locale استنتاج کند.
 */
export function assertLocaleMarketSeparation(input: {
  locale: Locale;
  currency?: string;
  market?: string;
}): { locale: Locale; currency: string | undefined; market: string | undefined; independent: true } {
  const { locale, currency, market } = input;
  // Locale never implies a currency code or market id.
  if (currency !== undefined && (currency === locale || currency.toLowerCase() === locale)) {
    throw new Error("locale must not equal currency; dimensions are independent");
  }
  if (market !== undefined && (market === locale || market.toLowerCase() === locale)) {
    throw new Error("locale must not equal market; dimensions are independent");
  }
  return { locale, currency, market, independent: true };
}
