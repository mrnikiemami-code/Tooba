import { LOCALES, type Locale } from "../i18n/locale.ts";

const SECRET_QUERY = /(guestSecret|refreshToken|accessToken|tooba_session|password|otp|secret)/i;

/** مسیر بازگشت داخلی امن؛ locale حفظ می‌شود و redirect باز رد می‌شود. */
export function sanitizeReturnTo(raw: string | null | undefined, locale: Locale): string {
  const fallback = `/${locale}/shipping`;
  if (!raw) {
    return fallback;
  }

  let value = raw.trim();
  try {
    if (/^[a-zA-Z][a-zA-Z+\-.]*:/.test(value) || value.startsWith("//") || value.includes("\\")) {
      return fallback;
    }
  } catch {
    return fallback;
  }

  if (!value.startsWith("/")) {
    return fallback;
  }

  const [pathPart, query = ""] = value.split("?", 2);
  if (!LOCALES.some((item) => pathPart === `/${item}` || pathPart.startsWith(`/${item}/`))) {
    return fallback;
  }

  if (SECRET_QUERY.test(pathPart) || SECRET_QUERY.test(query)) {
    return fallback;
  }

  if (pathPart.includes("/login")) {
    return fallback;
  }

  return query ? `${pathPart}?${query}` : pathPart;
}

/** نشانی ورود locale-aware. */
export function loginPath(locale: Locale, returnTo?: string | null): string {
  const safe = sanitizeReturnTo(returnTo, locale);
  return `/${locale}/login?returnTo=${encodeURIComponent(safe)}`;
}
