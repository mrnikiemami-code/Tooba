import { DEFAULT_LOCALE, LOCALE_COOKIE_NAME, parseLocale, type Locale } from "./locale.ts";

const MAX_AGE_SECONDS = 60 * 60 * 24 * 365;

/** خواندن locale از رشتهٔ document.cookie در مرورگر. */
export function readLocaleFromCookieString(cookieHeader: string | null | undefined): Locale {
  if (!cookieHeader) return DEFAULT_LOCALE;
  const parts = cookieHeader.split(";").map((part) => part.trim());
  for (const part of parts) {
    const eq = part.indexOf("=");
    if (eq <= 0) continue;
    const name = part.slice(0, eq).trim();
    if (name !== LOCALE_COOKIE_NAME) continue;
    return parseLocale(decodeURIComponent(part.slice(eq + 1).trim()));
  }
  return DEFAULT_LOCALE;
}

/** خواندن locale در کلاینت. */
export function readBrowserLocaleCookie(): Locale {
  if (typeof document === "undefined") return DEFAULT_LOCALE;
  return readLocaleFromCookieString(document.cookie);
}

/** نوشتن کوکی locale در مرورگر و بازگرداندن مقدار نرمال‌شده. */
export function writeBrowserLocaleCookie(locale: Locale): Locale {
  if (typeof document === "undefined") return locale;
  const secure = typeof window !== "undefined" && window.location.protocol === "https:" ? "; Secure" : "";
  document.cookie = `${LOCALE_COOKIE_NAME}=${encodeURIComponent(locale)}; Path=/; Max-Age=${MAX_AGE_SECONDS}; SameSite=Lax${secure}`;
  return locale;
}

/** پارس از query (?locale=en)؛ نامعتبر → null تا کوکی/پیش‌فرض استفاده شود. */
export function parseLocaleQueryParam(value: string | null | undefined): Locale | null {
  if (value == null || value.trim() === "") return null;
  const normalized = value.trim().toLowerCase();
  if (normalized === "fa" || normalized.startsWith("fa-") || normalized.startsWith("fa_")) return "fa";
  if (normalized === "en" || normalized.startsWith("en-") || normalized.startsWith("en_")) return "en";
  return null;
}
