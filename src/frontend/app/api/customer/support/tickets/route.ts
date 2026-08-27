import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../../lib/auth/csrf.ts";
import { forwardToHost } from "../../../../../lib/server/host-client.ts";

const MUTATING = new Set(["POST", "PUT", "PATCH", "DELETE"]);

/** فهرست / ایجاد تیکت پشتیبانی مشتری → Host. */
export async function GET(request: Request): Promise<Response> {
  return proxy(request, "GET");
}

export async function POST(request: Request): Promise<Response> {
  return proxy(request, "POST");
}

async function proxy(request: Request, method: string): Promise<Response> {
  if (MUTATING.has(method)) {
    const jar = await cookies();
    if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
      return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
    }
  }

  const url = new URL(request.url);
  const upstreamPath = `/v1/customer/support/tickets${url.search}`;
  const body = MUTATING.has(method) ? await request.text() : undefined;
  const headers: HeadersInit = {};
  const idem = request.headers.get("Idempotency-Key");
  if (idem) (headers as Record<string, string>)["Idempotency-Key"] = idem;

  const upstream = await forwardToHost(upstreamPath, {
    method,
    body: body && body.length > 0 ? body : undefined,
    json: Boolean(body),
    headers,
  });
  const payload = await upstream.text();
  return new NextResponse(payload, {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
  });
}
