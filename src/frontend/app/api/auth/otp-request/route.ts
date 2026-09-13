import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../lib/auth/csrf.ts";
import { hostBaseUrl } from "../../../../lib/server/host-client.ts";

export async function POST(request: Request): Promise<Response> {
  const jar = await cookies();
  if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
    return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
  }

  const body = (await request.json().catch(() => null)) as { identifier?: string } | null;
  if (!body?.identifier) {
    return NextResponse.json({ title: "Bad Request", errorCode: "identity.validation.failed" }, { status: 400 });
  }

  const upstream = await fetch(`${hostBaseUrl()}/v1/auth/otp-login/request`, {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify({ identifier: body.identifier }),
    cache: "no-store",
  });
  const payload = await upstream.json().catch(() => null);
  return NextResponse.json(payload ?? {}, { status: upstream.status });
}
