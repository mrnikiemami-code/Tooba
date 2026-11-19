/**
 * Story editor lookups — lawful authority per role (no admin headers inside seller).
 */
import { ADMIN_DEV_ACTOR_HEADER } from "../../admin/admin-api";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  type SellerOfferListRow,
} from "../../vendor-panel/seller-api";

export type StoryLocaleOptionRow = { code: string; name: string; isDefault: boolean };
export type StoryProductOptionRow = {
  offerId: string;
  productId: string;
  productTitle: string;
  status: string;
};

function adminHeaders(): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (typeof window !== "undefined") {
    const actor = window.localStorage.getItem("tooba.adminActorUserId");
    if (actor) headers[ADMIN_DEV_ACTOR_HEADER] = actor;
  }
  return headers;
}

function sellerHeaders(shopId: string, actorUserId: string): Record<string, string> {
  return {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: shopId,
    [DEV_ACTOR_HEADER]: actorUserId,
  };
}

function asArray(payload: unknown): unknown[] {
  if (Array.isArray(payload)) return payload;
  if (payload && typeof payload === "object") {
    const record = payload as Record<string, unknown>;
    if (Array.isArray(record.data)) return record.data;
    if (Array.isArray(record.items)) return record.items;
  }
  return [];
}

export async function loadStoryLocaleOptions(args: {
  mode: "admin" | "seller";
  shopId: string | null;
  actorUserId: string;
}): Promise<StoryLocaleOptionRow[]> {
  const url =
    args.mode === "seller" ? "/v1/seller/stories/locale-options" : "/v1/admin/stories/locale-options";
  const headers =
    args.mode === "seller"
      ? sellerHeaders(args.shopId ?? "", args.actorUserId)
      : adminHeaders();
  const response = await fetch(url, { headers });
  if (!response.ok) return [];
  const payload = await response.json().catch(() => null);
  return asArray(payload)
    .map((row) => {
      if (!row || typeof row !== "object") return null;
      const r = row as Record<string, unknown>;
      const code = String(r.code ?? r.Code ?? "").trim();
      if (!code) return null;
      return {
        code,
        name: String(r.name ?? r.Name ?? r.nativeName ?? r.NativeName ?? code),
        isDefault: Boolean(r.isDefault ?? r.IsDefault ?? r.default),
      } satisfies StoryLocaleOptionRow;
    })
    .filter((row): row is StoryLocaleOptionRow => row != null);
}

export async function loadStoryProductOptions(args: {
  mode: "admin" | "seller";
  shopId: string | null;
  actorUserId: string;
}): Promise<StoryProductOptionRow[]> {
  if (args.mode === "seller") {
    if (!args.shopId) return [];
    const response = await fetch("/v1/seller/stories/product-options", {
      headers: sellerHeaders(args.shopId, args.actorUserId),
    });
    if (!response.ok) return [];
    const payload = await response.json().catch(() => null);
    return mapProductRows(asArray(payload));
  }

  const qs = args.shopId ? `?sellerPartyId=${encodeURIComponent(args.shopId)}` : "";
  const response = await fetch(`/v1/admin/stories/product-options${qs}`, { headers: adminHeaders() });
  if (!response.ok) return [];
  const payload = await response.json().catch(() => null);
  return mapProductRows(asArray(payload));
}

function mapProductRows(rows: unknown[]): StoryProductOptionRow[] {
  return rows
    .map((row) => {
      if (!row || typeof row !== "object") return null;
      const r = row as Record<string, unknown>;
      const productId = String(r.productId ?? r.ProductId ?? "").trim();
      const title = String(r.title ?? r.Title ?? r.productTitle ?? "").trim();
      if (!productId || !title) return null;
      return {
        offerId: productId,
        productId,
        productTitle: title,
        status: "Active",
      } satisfies StoryProductOptionRow;
    })
    .filter((row): row is StoryProductOptionRow => row != null);
}

/** Adapt seller offer list shape when product-options is unavailable (seller-only fallback unused). */
export function offersToProductOptions(rows: SellerOfferListRow[]): StoryProductOptionRow[] {
  return rows
    .filter((row) => row.status === "Active" && row.productId)
    .map((row) => ({
      offerId: row.offerId,
      productId: row.productId!,
      productTitle: row.productTitle,
      status: row.status,
    }));
}
