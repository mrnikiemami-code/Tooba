import { NextResponse } from "next/server";
import { forwardToHost } from "../../../../lib/server/host-client.ts";

export async function GET(): Promise<Response> {
  const upstream = await forwardToHost("/v1/auth/me");
  const payload = await upstream.json().catch(() => null);
  return NextResponse.json(payload ?? {}, { status: upstream.status });
}
