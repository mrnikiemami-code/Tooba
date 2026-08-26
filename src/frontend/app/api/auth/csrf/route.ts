import { NextResponse } from "next/server";
import { CSRF_COOKIE_NAME, createCsrfToken, csrfCookieOptions } from "../../../../lib/auth/csrf.ts";

/** Issues readable CSRF cookie for browser mutating requests. */
export async function GET(): Promise<Response> {
  const token = createCsrfToken();
  const secure = process.env.NODE_ENV === "production";
  const response = NextResponse.json({ ok: true });
  response.cookies.set(CSRF_COOKIE_NAME, token, { ...csrfCookieOptions(secure), name: CSRF_COOKIE_NAME, value: token });
  return response;
}
