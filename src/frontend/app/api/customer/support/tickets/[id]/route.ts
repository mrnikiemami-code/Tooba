import { NextResponse } from "next/server";
import { forwardToHost } from "../../../../../../lib/server/host-client.ts";

/** جزئیات تیکت پشتیبانی مشتری → Host. */
export async function GET(
  _request: Request,
  context: { params: Promise<{ id: string }> },
): Promise<Response> {
  const { id } = await context.params;
  const upstream = await forwardToHost(`/v1/customer/support/tickets/${encodeURIComponent(id)}`, {
    method: "GET",
  });
  const payload = await upstream.text();
  return new NextResponse(payload, {
    status: upstream.status,
    headers: { "Content-Type": upstream.headers.get("Content-Type") ?? "application/json" },
  });
}
