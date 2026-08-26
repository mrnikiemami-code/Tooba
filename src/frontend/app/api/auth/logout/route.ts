import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../lib/auth/csrf.ts";
import { clearAuthCookieOptions } from "../../../../lib/server/session-cookies.ts";
import { forwardToHost, readSessionId } from "../../../../lib/server/host-client.ts";

export async function POST(request: Request): Promise<Response> {
  const jar = await cookies();
  if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
    return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
  }

  const sessionId = await readSessionId();
  if (sessionId) {
    await forwardToHost("/v1/auth/logout", {
      method: "POST",
      headers: { Authorization: `Bearer ${sessionId}` },
    });
  }

  const secure = process.env.NODE_ENV === "production";
  const response = new NextResponse(null, { status: 204 });
  for (const cookie of clearAuthCookieOptions(secure)) {
    response.cookies.set(cookie.name, "", cookie.options);
  }
  return response;
}
