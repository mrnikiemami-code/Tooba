import { NextResponse } from "next/server";
import { CSRF_COOKIE_NAME, createCsrfToken, csrfCookieOptions } from "../../../../lib/auth/csrf.ts";
import { authCookieSecure } from "../../../../lib/server/session-cookies.ts";

/** Issues readable CSRF cookie for browser mutating requests. */
export async function GET(request: Request): Promise<Response> {
  const token = createCsrfToken();
  const secure = authCookieSecure(request);
  const response = NextResponse.json({ ok: true });
  response.cookies.set(CSRF_COOKIE_NAME, token, { ...csrfCookieOptions(secure), name: CSRF_COOKIE_NAME, value: token });
  return response;
}
