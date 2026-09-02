import {
  isExcludedFromLocalePrefix,
  isPublicStorefrontPath,
  parseInvalidLocalePrefix,
  parseLocalePrefix,
  resolvePreferredLocale,
  type Locale,
} from "./routing.ts";

export type LocaleMiddlewarePlan =
  | { type: "pass" }
  | { type: "not-found" }
  | { type: "rewrite"; locale: Locale; internalPath: string }
  | { type: "redirect"; location: string };

/**
 * تصمیم middleware locale.
 * بازنویسی /fa|/en نباید دوباره به مسیر عمومی بدون prefix بخورد و 308 به خودش بزند.
 */
export function planLocaleMiddleware(
  pathname: string,
  cookieValue: string | undefined,
  rewrittenLocaleHeader: string | null,
): LocaleMiddlewarePlan {
  if (isExcludedFromLocalePrefix(pathname)) {
    return { type: "pass" };
  }

  if (parseInvalidLocalePrefix(pathname)) {
    return { type: "not-found" };
  }

  const prefixed = parseLocalePrefix(pathname);
  if (prefixed) {
    return { type: "rewrite", locale: prefixed.locale, internalPath: prefixed.pathname };
  }

  if (isPublicStorefrontPath(pathname)) {
    if (rewrittenLocaleHeader === "fa" || rewrittenLocaleHeader === "en") {
      return { type: "pass" };
    }

    const preferred = resolvePreferredLocale(cookieValue);
    return {
      type: "redirect",
      location: pathname === "/" ? `/${preferred}` : `/${preferred}${pathname}`,
    };
  }

  return { type: "pass" };
}
