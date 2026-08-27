/**
 * حل locale برای metadata سرور (blog SEO).
 * canonical همیشه مسیر fa است؛ hreflang منتظر locale منتشرشدهٔ دوم می‌ماند.
 */
import { cookies } from "next/headers";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  openGraphLocaleFor,
  parseLocale,
  type Locale,
} from "./locale.ts";
import { parseLocaleQueryParam } from "./locale-cookie.ts";

export async function resolveRequestLocale(searchParams?: {
  locale?: string | string[] | undefined;
}): Promise<Locale> {
  const fromQuery = parseLocaleQueryParam(
    Array.isArray(searchParams?.locale) ? searchParams?.locale[0] : searchParams?.locale,
  );
  if (fromQuery) return fromQuery;
  try {
    const jar = await cookies();
    return parseLocale(jar.get(LOCALE_COOKIE_NAME)?.value);
  } catch {
    return DEFAULT_LOCALE;
  }
}

export function blogOpenGraphLocale(locale: Locale): string {
  return openGraphLocaleFor(locale);
}
