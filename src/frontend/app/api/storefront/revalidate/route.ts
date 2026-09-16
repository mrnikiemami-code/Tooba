import { revalidateTag } from "next/cache";
import { NextResponse } from "next/server";
import {
  storePageRevalidateTags,
  type StorePageRevalidatePayload,
} from "../../../storefront/storefront-store-page-cache.ts";

/**
 * Invalidates Store+locale+page scoped public caches after Admin publish / Home changes.
 * Not a public CDN purge of private Admin preview (preview uses no-store).
 */
export async function POST(request: Request) {
  let body: StorePageRevalidatePayload;
  try {
    body = (await request.json()) as StorePageRevalidatePayload;
  } catch {
    return NextResponse.json({ ok: false, error: "invalid.json" }, { status: 400 });
  }
  if (!body?.storeScope || typeof body.storeScope !== "string") {
    return NextResponse.json({ ok: false, error: "storeScope.required" }, { status: 400 });
  }
  const tags = storePageRevalidateTags(body);
  for (const tag of tags) {
    revalidateTag(tag);
  }
  return NextResponse.json({ ok: true, tags });
}
