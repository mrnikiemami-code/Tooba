/**
 * حل locale برای metadata/API سرور — اول URL prefix، سپس query، سپس کوکی.
 */
import { cookies, headers } from "next/headers";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  openGraphLocaleFor,
  parseLocale,
  type Locale,
} from "./locale.ts";
import { LOCALE_HEADER_NAME } from "./routing.ts";
import { parseLocaleQueryParam } from "./locale-cookie.ts";

export async function resolveRequestLocale(searchParams?: {
  locale?: string | string[] | undefined;
}): Promise<Locale> {
  try {
    const headerStore = await headers();
    const fromHeader = headerStore.get(LOCALE_HEADER_NAME);
    if (fromHeader === "fa" || fromHeader === "en") return fromHeader;
  } catch {
    // outside request scope in tests
  }

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
