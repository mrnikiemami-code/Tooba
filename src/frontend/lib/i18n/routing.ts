/**
 * مسیرهای عمومی locale-prefixed — منبع واحد حقیقت برای prefix و SEO.
 */
import {
  DEFAULT_LOCALE,
  LOCALES,
  isLocale,
  parseLocale,
  type Locale,
} from "./locale.ts";

export const LOCALE_HEADER_NAME = "x-tooba-locale";

/** مسیرهای SEO-عمومی که باید prefix locale داشته باشند. */
export const PUBLIC_STOREFRONT_PREFIXES = [
  "/products",
  "/blogs",
  "/cart",
  "/checkout",
  "/offers",
  "/sale",
  "/new-products",
  "/most-viewed",
  "/best-seller",
  "/brands",
  "/brand",
  "/sellers",
  "/seller-profile",
  "/trending",
  "/order",
  "/payment/result",
] as const;

/** مسیرهای داخلی بدون prefix locale. */
export const LOCALE_EXCLUDED_PREFIXES = [
  "/admin",
  "/customer-panel",
  "/vendor-panel",
  "/api",
  "/design-system",
  "/payment/sandbox",
  "/_next",
  "/icon.svg",
] as const;

export function isExcludedFromLocalePrefix(pathname: string): boolean {
  if (pathname.includes(".")) return true;
  return LOCALE_EXCLUDED_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

export function isPublicStorefrontPath(pathname: string): boolean {
  if (pathname === "/") return true;
  return PUBLIC_STOREFRONT_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`),
  );
}

/** استخراج locale از prefix URL؛ null اگر prefix نداشته باشد. */
export function parseLocalePrefix(pathname: string): { locale: Locale; pathname: string } | null {
  const match = pathname.match(/^\/(fa|en)(\/.*)?$/);
  if (!match) return null;
  const locale = match[1] as Locale;
  const rest = match[2] ?? "/";
  return { locale, pathname: rest === "" ? "/" : rest };
}

/** prefix نامعتبر دوحرفی (مثل /fr) — برای 404. */
export function parseInvalidLocalePrefix(pathname: string): string | null {
  const match = pathname.match(/^\/([a-zA-Z]{2})(\/|$)/);
  if (!match) return null;
  const code = match[1]!.toLowerCase();
  if (isLocale(code)) return null;
  return code;
}

/** مسیر داخلی + locale → URL عمومی canonical. */
export function localePath(locale: Locale, internalPath: string): string {
  const normalized = internalPath.startsWith("/") ? internalPath : `/${internalPath}`;
  if (normalized === "/") return `/${locale}`;
  return `/${locale}${normalized}`;
}

/** حذف prefix locale از URL عمومی. */
export function stripLocalePrefix(pathname: string): string {
  const parsed = parseLocalePrefix(pathname);
  return parsed?.pathname ?? pathname;
}

/** locale ترجیحی از کوکی یا پیش‌فرض. */
export function resolvePreferredLocale(cookieValue: string | null | undefined): Locale {
  return parseLocale(cookieValue ?? null);
}

/** BCP-47 برای API Content/PageComposition. */
export function localeToContentApi(locale: Locale): string {
  return locale === "fa" ? "fa-IR" : "en";
}

/** hreflang tag value. */
export function hreflangForLocale(locale: Locale): string {
  return locale === "fa" ? "fa-IR" : "en";
}

export function allPublicLocales(): readonly Locale[] {
  return LOCALES;
}

export interface LocalizedAlternate {
  locale: Locale;
  hreflang: string;
  path: string;
}

/** alternate واقعی برای صفحات معادل fa/en — بدون hreflang ساختگی. */
export function buildLocaleAlternates(
  internalPath: string,
  options?: { includeXDefault?: boolean },
): { canonical: string; languages: Record<string, string> } {
  const languages: Record<string, string> = {};
  for (const locale of LOCALES) {
    languages[hreflangForLocale(locale)] = localePath(locale, internalPath);
  }
  if (options?.includeXDefault) {
    languages["x-default"] = localePath(DEFAULT_LOCALE, internalPath);
  }
  return {
    canonical: localePath(DEFAULT_LOCALE, internalPath),
    languages,
  };
}

/** canonical برای locale فعال (self-referencing). */
export function canonicalForLocale(locale: Locale, internalPath: string): string {
  return localePath(locale, internalPath);
}
