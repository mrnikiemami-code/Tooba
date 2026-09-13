import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../lib/auth/csrf.ts";
import { buildAuthCookies } from "../../../../lib/server/session-cookies.ts";
import { hostBaseUrl } from "../../../../lib/server/host-client.ts";

export async function POST(request: Request): Promise<Response> {
  const jar = await cookies();
  if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
    return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
  }

  const body = (await request.json().catch(() => null)) as {
    identifier?: string;
    challengeId?: string;
    secret?: string;
  } | null;
  if (!body?.identifier || !body.challengeId || !body.secret) {
    return NextResponse.json({ title: "Bad Request", errorCode: "identity.validation.failed" }, { status: 400 });
  }

  const upstream = await fetch(`${hostBaseUrl()}/v1/auth/otp-login/complete`, {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify({
      identifier: body.identifier,
      challengeId: body.challengeId,
      secret: body.secret,
    }),
    cache: "no-store",
  });
  const payload = await upstream.json().catch(() => null) as Record<string, unknown> | null;
  if (!upstream.ok) {
    return NextResponse.json(payload ?? { title: "Unauthorized" }, { status: upstream.status });
  }

  const sessionId = String(payload?.accessToken ?? payload?.sessionId ?? "");
  const refreshToken = String(payload?.refreshToken ?? "");
  if (!sessionId || !refreshToken) {
    return NextResponse.json({ title: "Bad Gateway", errorCode: "auth.session.invalid" }, { status: 502 });
  }

  const response = NextResponse.json({ userId: payload?.userId, authenticated: true });
  for (const cookie of buildAuthCookies(sessionId, refreshToken)) {
    response.cookies.set(cookie.name, cookie.value, cookie.options);
  }
  return response;
}
