import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { REFRESH_COOKIE_NAME } from "../../../../lib/auth/constants.ts";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../lib/auth/csrf.ts";
import { buildAuthCookies, clearAuthCookieOptions, SESSION_COOKIE_NAME } from "../../../../lib/server/session-cookies.ts";
import { hostBaseUrl } from "../../../../lib/server/host-client.ts";

export async function POST(request: Request): Promise<Response> {
  const jar = await cookies();
  if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
    return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
  }

  const sessionId = jar.get(SESSION_COOKIE_NAME)?.value;
  const refreshToken = jar.get(REFRESH_COOKIE_NAME)?.value;
  if (!sessionId || !refreshToken) {
    return NextResponse.json({ title: "Unauthorized", errorCode: "identity.session.invalid" }, { status: 401 });
  }

  const upstream = await fetch(`${hostBaseUrl()}/v1/auth/refresh`, {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify({ sessionId, refreshToken }),
    cache: "no-store",
  });
  const payload = await upstream.json().catch(() => null) as Record<string, unknown> | null;
  if (!upstream.ok) {
    const secure = process.env.NODE_ENV === "production";
    const response = NextResponse.json(payload ?? { title: "Unauthorized" }, { status: upstream.status });
    for (const cookie of clearAuthCookieOptions(secure)) {
      response.cookies.set(cookie.name, "", cookie.options);
    }
    return response;
  }

  const nextSession = String(payload?.accessToken ?? payload?.sessionId ?? "");
  const nextRefresh = String(payload?.refreshToken ?? "");
  if (!nextSession || !nextRefresh) {
    return NextResponse.json({ title: "Bad Gateway", errorCode: "auth.session.invalid" }, { status: 502 });
  }

  const response = NextResponse.json({ authenticated: true, userId: payload?.userId ?? null });
  for (const cookie of buildAuthCookies(nextSession, nextRefresh)) {
    response.cookies.set(cookie.name, cookie.value, cookie.options);
  }
  return response;
}
