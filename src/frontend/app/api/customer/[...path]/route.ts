import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { CSRF_COOKIE_NAME, validateCsrf } from "../../../../lib/auth/csrf.ts";
import { forwardToHost } from "../../../../lib/server/host-client.ts";

const MUTATING = new Set(["POST", "PUT", "PATCH", "DELETE"]);

export async function GET(request: Request, context: { params: Promise<{ path: string[] }> }): Promise<Response> {
  return proxy(request, context, "GET");
}

export async function POST(request: Request, context: { params: Promise<{ path: string[] }> }): Promise<Response> {
  return proxy(request, context, "POST");
}

export async function PUT(request: Request, context: { params: Promise<{ path: string[] }> }): Promise<Response> {
  return proxy(request, context, "PUT");
}

export async function PATCH(request: Request, context: { params: Promise<{ path: string[] }> }): Promise<Response> {
  return proxy(request, context, "PATCH");
}

export async function DELETE(request: Request, context: { params: Promise<{ path: string[] }> }): Promise<Response> {
  return proxy(request, context, "DELETE");
}

async function proxy(request: Request, context: { params: Promise<{ path: string[] }> }, method: string): Promise<Response> {
  if (MUTATING.has(method)) {
    const jar = await cookies();
    if (!validateCsrf(request, jar.get(CSRF_COOKIE_NAME)?.value)) {
      return NextResponse.json({ title: "Forbidden", errorCode: "auth.csrf.invalid" }, { status: 403 });
    }
    const origin = request.headers.get("origin");
    const host = request.headers.get("host");
    if (origin && host && !origin.includes(host)) {
      return NextResponse.json({ title: "Forbidden", errorCode: "auth.origin.invalid" }, { status: 403 });
    }
  }

  const { path } = await context.params;
  const suffix = path.join("/");
  const url = new URL(request.url);
  const upstreamPath = `/v1/customer/${suffix}${url.search}`;
  const body = MUTATING.has(method) ? await request.text() : undefined;
  const upstream = await forwardToHost(upstreamPath, {
    method,
    body: body && body.length > 0 ? body : undefined,
    json: Boolean(body),
  });
  const payload = await upstream.text();
  return new NextResponse(payload, {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
  });
}
