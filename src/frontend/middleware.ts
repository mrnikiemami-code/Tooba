import { NextResponse, type NextRequest } from "next/server";
import { LOCALE_HEADER_NAME } from "./lib/i18n/routing.ts";
import { LOCALE_COOKIE_NAME } from "./lib/i18n/locale.ts";
import { planLocaleMiddleware } from "./lib/i18n/middleware-locale.ts";

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const plan = planLocaleMiddleware(
    pathname,
    request.cookies.get(LOCALE_COOKIE_NAME)?.value,
    request.headers.get(LOCALE_HEADER_NAME),
  );

  if (plan.type === "not-found") {
    return NextResponse.rewrite(new URL("/not-found", request.url));
  }

  if (plan.type === "rewrite") {
    const rewriteTarget = new URL(
      `${plan.internalPath}${request.nextUrl.search}${request.nextUrl.hash}`,
      request.url,
    );
    const requestHeaders = new Headers(request.headers);
    requestHeaders.set(LOCALE_HEADER_NAME, plan.locale);
    const response = NextResponse.rewrite(rewriteTarget, { request: { headers: requestHeaders } });
    response.cookies.set(LOCALE_COOKIE_NAME, plan.locale, {
      path: "/",
      maxAge: 60 * 60 * 24 * 365,
      sameSite: "lax",
    });
    return response;
  }

  if (plan.type === "redirect") {
    const redirectUrl = new URL(request.url);
    redirectUrl.pathname = plan.location;
    return NextResponse.redirect(redirectUrl, 308);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
