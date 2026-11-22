import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { ADMIN_ACTOR_STORAGE_KEY, ADMIN_DEV_ACTOR_HEADER, adminRequestHeaders } from "../../lib/admin/admin-result.ts";
import { prepareAdminDevActor } from "./admin-api";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? "";
}

function headers(extra?: Record<string, string>): Record<string, string> {
  return { ...adminRequestHeaders(), ...(extra ?? {}), [ADMIN_DEV_ACTOR_HEADER]: actorId() };
}

export interface AdminOfferRow {
  id: string;
  offerId: string;
  productTitle: string;
  sellerSku: string | null;
  status: string;
  amount: number | null;
  currency: string;
  availableUnits: number;
  lastUpdatedAt: string | null;
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? value as Record<string, unknown> : null;
}
function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown { return item[camel] ?? item[pascal]; }
function text(value: unknown, fallback = ""): string { return value == null ? fallback : String(value); }
function number(value: unknown): number { const n = Number(value); return Number.isFinite(n) ? n : 0; }
function nullableNumber(value: unknown): number | null { if (value == null || value === "") return null; const n = Number(value); return Number.isFinite(n) ? n : null; }

export function mapAdminOfferRow(value: unknown): AdminOfferRow | null {
  const item = record(value);
  if (!item) return null;
  const offerId = text(prop(item, "offerId", "OfferId"));
  if (!offerId) return null;
  return {
    id: offerId,
    offerId,
    productTitle: text(prop(item, "productTitle", "ProductTitle"), "—"),
    sellerSku: prop(item, "sellerSku", "SellerSku") == null ? null : text(prop(item, "sellerSku", "SellerSku")),
    status: text(prop(item, "status", "Status")),
    amount: nullableNumber(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    availableUnits: number(prop(item, "availableUnits", "AvailableUnits")),
    lastUpdatedAt: prop(item, "lastUpdatedAt", "LastUpdatedAt") == null ? null : text(prop(item, "lastUpdatedAt", "LastUpdatedAt")),
  };
}

export async function queryAdminOffersGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminOfferRow>> {
  await prepareAdminDevActor();
  return postAdminGridQuery("/v1/admin/offers/query", query, headers(), (item) => mapAdminOfferRow(item));
}

export interface AdminCatalogVariant {
  catalogVariantId: string;
  productId: string;
  productTitle: string;
  catalogCode: string | null;
  productStatus: string;
}

export async function loadAdminCatalogVariants(search = "", page = 1, pageSize = 20): Promise<{
  source: "host" | "error";
  rows: AdminCatalogVariant[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
  message?: string;
  denied?: boolean;
}> {
  try {
    await prepareAdminDevActor();
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search.trim()) params.set("search", search.trim());
    const response = await fetch(`/v1/admin/catalog/variants?${params.toString()}`, { headers: headers() });
    if (response.status === 401 || response.status === 403) return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: "admin.authorization.denied", denied: true };
    if (!response.ok) return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: `admin.catalog-variants.http.${response.status}` };
    const payload = await response.json() as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; hasMore?: boolean };
    const rows = (payload.items ?? []).flatMap((raw): AdminCatalogVariant[] => {
      const item = record(raw);
      if (!item) return [];
      const catalogVariantId = text(prop(item, "catalogVariantId", "CatalogVariantId"));
      if (!catalogVariantId) return [];
      return [{
        catalogVariantId,
        productId: text(prop(item, "productId", "ProductId")),
        productTitle: text(prop(item, "productTitle", "ProductTitle"), "—"),
        catalogCode: prop(item, "catalogCode", "CatalogCode") == null ? null : text(prop(item, "catalogCode", "CatalogCode")),
        productStatus: text(prop(item, "productStatus", "ProductStatus")),
      }];
    });
    return { source: "host", rows, totalCount: Number(payload.totalCount ?? 0), page: Number(payload.page ?? page), pageSize: Number(payload.pageSize ?? pageSize), hasMore: Boolean(payload.hasMore) };
  } catch {
    return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: "host-unreachable" };
  }
}

export interface AdminOfferDetail {
  offerId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  productTitle: string;
  sellerSku: string | null;
  status: string;
  amount: number | null;
  currency: string;
  onHand: number;
  availableUnits: number;
}

export async function loadAdminOfferDetail(offerId: string): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}`, { headers: headers() });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) return { ok: false, errorCode: `admin.offer.http.${response.status}` };
    const item = record(await response.json());
    if (!item) return { ok: false, errorCode: "admin.offer.invalid-response" };
    return {
      ok: true,
      detail: {
        offerId: text(prop(item, "offerId", "OfferId")),
        sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
        sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName")),
        productTitle: text(prop(item, "productTitle", "ProductTitle"), "—"),
        sellerSku: prop(item, "sellerSku", "SellerSku") == null ? null : text(prop(item, "sellerSku", "SellerSku")),
        status: text(prop(item, "status", "Status")),
        amount: nullableNumber(prop(item, "amount", "Amount")),
        currency: text(prop(item, "currency", "Currency"), "IRR"),
        onHand: number(prop(item, "onHand", "OnHand")),
        availableUnits: number(prop(item, "availableUnits", "AvailableUnits")),
      },
    };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function createAdminOffer(input: {
  sellerPartyId: string;
  catalogVariantId: string;
  sellerSku?: string | null;
  requestId: string;
}): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch("/v1/admin/offers", {
      method: "POST",
      headers: headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        sellerPartyId: input.sellerPartyId,
        catalogVariantId: input.catalogVariantId,
        sellerSku: input.sellerSku,
        status: "Draft",
        requestId: input.requestId,
      }),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "admin.offer.create-failed" };
    }
    const item = record(await response.json());
    if (!item) return { ok: false, errorCode: "admin.offer.invalid-response" };
    const offerId = text(prop(item, "offerId", "OfferId"));
    const sellerPartyId = text(prop(item, "sellerPartyId", "SellerPartyId"));
    if (!offerId || sellerPartyId !== input.sellerPartyId || text(prop(item, "catalogVariantId", "CatalogVariantId")) !== input.catalogVariantId) {
      return { ok: false, errorCode: "admin.offer.response_mismatch" };
    }
    return { ok: true, detail: {
      offerId, sellerPartyId,
      sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName")),
      productTitle: text(prop(item, "productTitle", "ProductTitle"), "—"),
      sellerSku: prop(item, "sellerSku", "SellerSku") == null ? null : text(prop(item, "sellerSku", "SellerSku")),
      status: text(prop(item, "status", "Status")),
      amount: nullableNumber(prop(item, "amount", "Amount")),
      currency: text(prop(item, "currency", "Currency"), "IRR"),
      onHand: number(prop(item, "onHand", "OnHand")),
      availableUnits: number(prop(item, "availableUnits", "AvailableUnits")),
    } };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

