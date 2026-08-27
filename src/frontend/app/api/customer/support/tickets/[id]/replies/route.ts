import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../../../../lib/auth/csrf.ts";
import { forwardToHost } from "../../../../../../../lib/server/host-client.ts";

/** پاسخ به تیکت پشتیبانی مشتری → Host. */
export async function POST(
  request: Request,
  context: { params: Promise<{ id: string }> },
): Promise<Response> {
  const jar = await cookies();
  if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
    return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
  }

  const { id } = await context.params;
  const body = await request.text();
  const headers: Record<string, string> = {};
  const idem = request.headers.get("Idempotency-Key");
  if (idem) headers["Idempotency-Key"] = idem;

  const upstream = await forwardToHost(`/v1/customer/support/tickets/${encodeURIComponent(id)}/replies`, {
    method: "POST",
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
