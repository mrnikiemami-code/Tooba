import { NextResponse, type NextRequest } from "next/server";
import {
  LOCALE_HEADER_NAME,
  isExcludedFromLocalePrefix,
  isPublicStorefrontPath,
  parseInvalidLocalePrefix,
  parseLocalePrefix,
  resolvePreferredLocale,
} from "./lib/i18n/routing.ts";
import { LOCALE_COOKIE_NAME } from "./lib/i18n/locale.ts";

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (isExcludedFromLocalePrefix(pathname)) {
    return NextResponse.next();
  }

  const invalid = parseInvalidLocalePrefix(pathname);
  if (invalid) {
    return NextResponse.rewrite(new URL("/not-found", request.url));
  }

  const prefixed = parseLocalePrefix(pathname);
  if (prefixed) {
    const url = request.nextUrl.clone();
    url.pathname = prefixed.pathname;
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set(LOCALE_HEADER_NAME, prefixed.locale);
    const response = NextResponse.rewrite(url, { request: { headers: requestHeaders } });
    response.cookies.set(LOCALE_COOKIE_NAME, prefixed.locale, {
      path: "/",
      maxAge: 60 * 60 * 24 * 365,
      sameSite: "lax",
    });
    return response;
  }

  if (isPublicStorefrontPath(pathname)) {
    const preferred = resolvePreferredLocale(request.cookies.get(LOCALE_COOKIE_NAME)?.value);
    const redirectUrl = request.nextUrl.clone();
    redirectUrl.pathname = pathname === "/" ? `/${preferred}` : `/${preferred}${pathname}`;
    return NextResponse.redirect(redirectUrl, 308);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
