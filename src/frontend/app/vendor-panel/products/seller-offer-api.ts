import type { HostReadSource, SellerOfferDetail } from "../seller-api.ts";
import { sellerHeaders, readJson, currentActor, isDeniedStatus, readProp, asString, asNullableString, asNumber, asNullableNumber, mapSellerOfferDetail } from "../seller-api.ts";

export async function loadSellerOfferDetail(
  sellerPartyId: string,
  offerId: string,
): Promise<{ source: HostReadSource; detail: SellerOfferDetail | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
      return { source: "error", detail: null, message: "seller.offer.missing", denied: true };
    }
    if (!response.ok) {
      return { source: "error", detail: null, message: "seller-offer-http-" + String(response.status) };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    return detail && detail.offerId === offerId && detail.sellerPartyId === sellerPartyId ? { source: "host", detail } : { source: "error", detail: null, message: "seller-offer-invalid" };
  } catch {
    return { source: "error", detail: null, message: "host-unreachable" };
  }
}

/**
 * سیاست رزرو مؤثر Offer را فقط‌خواندنی برمی‌گرداند؛ فروشنده مجوز mutation ندارد.
 */
export async function patchSellerOffer(
  sellerPartyId: string,
  offerId: string,
  patch: {
    sellerSku?: string | null;
    status?: string | null;
    returnPolicyChoice?: string | null;
    customReturnWindowDays?: number | null;
    minimumOrderQuantity?: number | null;
    maximumOrderQuantity?: number | null;
    clearOrderQuantityLimits?: boolean;
  },
  expectedVersion?: string,
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}`, {
      method: "PATCH",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({
        sellerSku: patch.sellerSku,
        status: patch.status,
        returnPolicyChoice: patch.returnPolicyChoice,
        customReturnWindowDays: patch.customReturnWindowDays,
        minimumOrderQuantity: patch.minimumOrderQuantity,
        maximumOrderQuantity: patch.maximumOrderQuantity,
        clearOrderQuantityLimits: patch.clearOrderQuantityLimits,
        expectedVersion,
      }),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.offer.patch-failed" };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    if (!detail || detail.offerId !== offerId || detail.sellerPartyId !== sellerPartyId) {
      return { ok: false, errorCode: "seller.offer.response_mismatch" };
    }
    if (patch.sellerSku !== undefined && (detail.sellerSku ?? null) !== patch.sellerSku) {
      return { ok: false, errorCode: "seller.offer.response_mismatch" };
    }
    if (patch.status !== undefined && detail.status !== patch.status) {
      return { ok: false, errorCode: "seller.offer.response_mismatch" };
    }
    if (patch.clearOrderQuantityLimits && (detail.minimumOrderQuantity != null || detail.maximumOrderQuantity != null)) {
      return { ok: false, errorCode: "seller.offer.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** گزینهٔ گونهٔ Catalog برای ایجاد Offer؛ Catalog فقط‌خواندنی است. */
export interface SellerCatalogVariantOption {
  catalogVariantId: string;
  productId: string;
  productTitle: string;
  catalogCode: string | null;
  productStatus: string;
}

export function mapSellerCatalogVariants(payload: unknown): SellerCatalogVariantOption[] {
  const items = Array.isArray(payload) ? payload : [];
  return items
    .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    .map((item) => ({
      catalogVariantId: asString(readProp(item, "catalogVariantId", "CatalogVariantId")),
      productId: asString(readProp(item, "productId", "ProductId")),
      productTitle: asString(readProp(item, "productTitle", "ProductTitle"), "بدون عنوان"),
      catalogCode: asNullableString(readProp(item, "catalogCode", "CatalogCode")),
      productStatus: asString(readProp(item, "productStatus", "ProductStatus")),
    }))
    .filter((row) => row.catalogVariantId.length > 0);
}

/** فهرست گونه‌های Catalog منتشرشده برای انتخاب Offer. */
export async function loadSellerCatalogVariants(
  sellerPartyId: string,
  search = "",
  page = 1,
  pageSize = 20,
): Promise<{ source: HostReadSource; rows: SellerCatalogVariantOption[]; totalCount: number; page: number; pageSize: number; hasMore: boolean; message?: string; denied?: boolean }> {
  try {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search.trim()) params.set("search", search.trim());
    const response = await fetch(`/v1/seller/catalog-variants?${params.toString()}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (isDeniedStatus(response.status)) {
      return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: "seller-catalog-variants-http-" + String(response.status) };
    }
    const payload = (await readJson(response)) as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; hasMore?: boolean };
    return {
      source: "host",
      rows: mapSellerCatalogVariants(payload.items ?? []),
      totalCount: Number(payload.totalCount ?? 0),
      page: Number(payload.page ?? page),
      pageSize: Number(payload.pageSize ?? pageSize),
      hasMore: Boolean(payload.hasMore),
    };
  } catch {
    return { source: "error", rows: [], totalCount: 0, page, pageSize, hasMore: false, message: "host-unreachable" };
  }
}

/** ایجاد Offer فقط برای Party فروشندهٔ احرازشده. */
export async function createSellerOffer(
  sellerPartyId: string,
  input: { catalogVariantId: string; sellerSku?: string | null; status?: string | null; channel?: string | null; returnPolicyChoice?: string | null; customReturnWindowDays?: number | null; minimumOrderQuantity?: number | null; maximumOrderQuantity?: number | null; requestId?: string | null },
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/offers", {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({
        catalogVariantId: input.catalogVariantId,
        sellerSku: input.sellerSku,
        channel: input.channel ?? undefined,
        returnPolicyChoice: input.returnPolicyChoice ?? undefined,
        customReturnWindowDays: input.customReturnWindowDays ?? undefined,
        minimumOrderQuantity: input.minimumOrderQuantity ?? undefined,
        maximumOrderQuantity: input.maximumOrderQuantity ?? undefined,
        status: input.status,
        requestId: input.requestId,
      }),
    });
    if (isDeniedStatus(response.status)) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.offer.create-failed" };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    if (!detail || detail.sellerPartyId !== sellerPartyId || detail.catalogVariantId !== input.catalogVariantId) {
      return { ok: false, errorCode: "seller.offer.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** نوشتن مبلغ بدون مالیات روی Offer خود فروشنده از طریق Pricing. */
export async function writeSellerOfferPrice(
  sellerPartyId: string,
  offerId: string,
  input: { amount: number; currency?: string | null; market?: string | null; operationId?: string },
  expectedVersion?: string,
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  if (!expectedVersion?.trim() || !input.operationId) return { ok: false, errorCode: "concurrency.conflict" };
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}/price`, {
      method: "PUT",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({
        amount: input.amount,
        currency: input.currency ?? undefined,
        market: input.market ?? undefined,
        expectedVersion,
        operationId: input.operationId,
      }),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
      return { ok: false, errorCode: "seller.offer.missing", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.price.write-failed" };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    if (!detail || detail.offerId !== offerId || detail.sellerPartyId !== sellerPartyId || detail.amount !== input.amount || (input.currency && detail.currency !== input.currency)) {
      return { ok: false, errorCode: "seller.price.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** تنظیم موجودی روی‌دست Offer خود فروشنده از طریق Inventory. */
export async function writeSellerOfferedQuantity(
  sellerPartyId: string,
  offerId: string,
  quantity: number,
  expectedVersion: string,
  operationId: string,
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}/offered-quantity`, {
      method: "PUT",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({ quantity, expectedVersion, operationId }),
    });
    if (isDeniedStatus(response.status) || response.status === 404) return { ok: false, errorCode: "seller.offer.missing", denied: true };
    if (!response.ok) { const body = await response.json().catch(() => null) as { errorCode?: string } | null; return { ok: false, errorCode: body?.errorCode ?? "seller.offered-quantity.write-failed" }; }
    const detail = mapSellerOfferDetail(await readJson(response));
    return detail && detail.offerId === offerId && detail.sellerPartyId === sellerPartyId && detail.offeredQuantity === quantity ? { ok: true, detail } : { ok: false, errorCode: "seller.offered-quantity.response_mismatch" };
  } catch { return { ok: false, errorCode: "host-unreachable" }; }
}
export async function writeSellerOfferInventory(
  sellerPartyId: string,
  offerId: string,
  input: { onHand: number; reason?: string | null; stockItemId?: string | null; operationId?: string },
  expectedVersion?: string,
): Promise<{ ok: true; detail: SellerOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  if (!expectedVersion?.trim() || !input.operationId) return { ok: false, errorCode: "concurrency.conflict" };
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}/inventory`, {
      method: "PUT",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({
        onHand: input.onHand,
        reason: input.reason ?? undefined,
        stockItemId: input.stockItemId ?? undefined,
        expectedVersion,
        operationId: input.operationId,
      }),
    });
    if (isDeniedStatus(response.status) || response.status === 404) {
      return { ok: false, errorCode: "seller.offer.missing", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "seller.inventory.write-failed" };
    }
    const detail = mapSellerOfferDetail(await readJson(response));
    const target = input.stockItemId
      ? detail?.inventoryLocations.find((row) => row.stockItemId === input.stockItemId)
      : undefined;
    if (!detail || detail.offerId !== offerId || detail.sellerPartyId !== sellerPartyId
        || (input.stockItemId ? !target || target.onHand !== input.onHand : detail.onHand !== input.onHand)) {
      return { ok: false, errorCode: "seller.inventory.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/**
 * فهرست سفارش‌های فروشنده را می‌خواند.
 */
export interface SellerCatalogProductOption {
  productId: string;
  title: string;
  brandName: string | null;
  primaryMediaAssetId: string | null;
  publishedVariantCount: number;
  unitCode: string | null;
  unitName: string | null;
  quantityDecimalPlaces: number;
  quantityStep: number | null;
  roundingMode: string | null;
}

export interface SellerCatalogProductPage {
  rows: SellerCatalogProductOption[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface SellerCatalogVariantLabelOption {
  catalogVariantId: string;
  productId: string;
  displayLabel: string;
  axisLabels: Array<{ name: string; value: string }>;
  catalogCode: string | null;
  status: string;
}

export async function searchSellerCatalogProducts(
  sellerPartyId: string,
  search = "",
  page = 1,
  pageSize = 20,
  locale = "fa-IR",
): Promise<SellerCatalogProductPage> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize), locale });
  if (search.trim()) params.set("search", search.trim());
  try {
    const response = await fetch(`/v1/seller/catalog-products?${params.toString()}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (!response.ok) return { rows: [], totalCount: 0, page, pageSize, hasMore: false };
    const payload = (await readJson(response)) as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; hasMore?: boolean };
    const rows = (Array.isArray(payload.items) ? payload.items : []).flatMap((raw): SellerCatalogProductOption[] => {
      const item = raw && typeof raw === "object" ? (raw as Record<string, unknown>) : null;
      if (!item) return [];
      const productId = asString(readProp(item, "productId", "ProductId"));
      if (!productId) return [];
      return [{
        productId,
        title: asString(readProp(item, "title", "Title"), "بدون عنوان"),
        brandName: asNullableString(readProp(item, "brandName", "BrandName")),
        primaryMediaAssetId: asNullableString(readProp(item, "primaryMediaAssetId", "PrimaryMediaAssetId")),
        publishedVariantCount: asNumber(readProp(item, "publishedVariantCount", "PublishedVariantCount")),
        unitCode: asNullableString(readProp(item, "unitCode", "UnitCode")),
        unitName: asNullableString(readProp(item, "unitName", "UnitName")),
        quantityDecimalPlaces: asNumber(readProp(item, "quantityDecimalPlaces", "QuantityDecimalPlaces")),
        quantityStep: readProp(item, "quantityStep", "QuantityStep") == null ? null : asNumber(readProp(item, "quantityStep", "QuantityStep")),
        roundingMode: asNullableString(readProp(item, "roundingMode", "RoundingMode")),
      }];
    });
    return {
      rows,
      totalCount: Number(payload.totalCount ?? 0),
      page: Number(payload.page ?? page),
      pageSize: Number(payload.pageSize ?? pageSize),
      hasMore: Boolean(payload.hasMore),
    };
  } catch {
    return { rows: [], totalCount: 0, page, pageSize, hasMore: false };
  }
}

export async function loadSellerProductVariants(
  sellerPartyId: string,
  productId: string,
  locale = "fa-IR",
): Promise<SellerCatalogVariantLabelOption[]> {
  try {
    const params = new URLSearchParams({ locale });
    const response = await fetch(`/v1/seller/catalog-products/${productId}/variants?${params.toString()}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (!response.ok) return [];
    const payload = await readJson(response);
    const items = Array.isArray(payload) ? payload : [];
    return items.flatMap((raw): SellerCatalogVariantLabelOption[] => {
      const item = raw && typeof raw === "object" ? (raw as Record<string, unknown>) : null;
      if (!item) return [];
      const catalogVariantId = asString(readProp(item, "catalogVariantId", "CatalogVariantId"));
      if (!catalogVariantId) return [];
      const axisRaw = readProp(item, "axisLabels", "AxisLabels");
      const axisLabels = Array.isArray(axisRaw)
        ? axisRaw.flatMap((axis) => {
            const row = axis && typeof axis === "object" ? (axis as Record<string, unknown>) : null;
            if (!row) return [];
            return [{ name: asString(readProp(row, "name", "Name")), value: asString(readProp(row, "value", "Value")) }];
          })
        : [];
      return [{
        catalogVariantId,
        productId: asString(readProp(item, "productId", "ProductId")),
        displayLabel: asString(readProp(item, "displayLabel", "DisplayLabel")),
        axisLabels,
        catalogCode: asNullableString(readProp(item, "catalogCode", "CatalogCode")),
        status: asString(readProp(item, "status", "Status")),
      }];
    });
  } catch {
    return [];
  }
}

export async function findSellerActiveOffer(
  sellerPartyId: string,
  catalogVariantId: string,
  channel?: string | null,
): Promise<{ offerId: string } | null> {
  const params = new URLSearchParams({ catalogVariantId });
  if (channel) params.set("channel", channel);
  try {
    const response = await fetch(`/v1/seller/offers/active-listing?${params.toString()}`, {
      headers: sellerHeaders(sellerPartyId, currentActor()),
    });
    if (!response.ok) return null;
    const item = await readJson(response);
    const offerId = item && typeof item === "object" ? asString(readProp(item as Record<string, unknown>, "offerId", "OfferId")) : "";
    return offerId ? { offerId } : null;
  } catch {
    return null;
  }
}

export async function loadSellerAllowedOfferChannels(sellerPartyId: string): Promise<string[]> {
  try {
    const response = await fetch("/v1/seller/offers/allowed-channels", { headers: sellerHeaders(sellerPartyId, currentActor()) });
    if (!response.ok) return [];
    const payload = await response.json();
    return Array.isArray(payload) ? payload.filter((x): x is string => typeof x === "string") : [];
  } catch { return []; }
}

export interface SellerInventoryLocationOption { locationId: string; code: string; name: string; }

export async function loadSellerInventoryLocations(sellerPartyId: string): Promise<SellerInventoryLocationOption[]> {
  try {
    const response = await fetch("/v1/seller/offers/inventory-locations", { headers: sellerHeaders(sellerPartyId, currentActor()) });
    if (!response.ok) return [];
    const payload = await response.json();
    return Array.isArray(payload) ? payload.flatMap((raw) => {
      if (!raw || typeof raw !== "object") return [];
      const item = raw as Record<string, unknown>;
      const locationId = asString(readProp(item, "locationId", "LocationId")); if (!locationId) return [];
      return [{ locationId, code: asString(readProp(item, "code", "Code")), name: asString(readProp(item, "name", "Name")) }];
    }) : [];
  } catch { return []; }
}

export async function ensureSellerInventoryPosition(sellerPartyId: string, offerId: string, locationId: string): Promise<{ stockItemId: string } | null> {
  try {
    const response = await fetch(`/v1/seller/offers/${offerId}/inventory/positions`, {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, currentActor(), { "Content-Type": "application/json" }),
      body: JSON.stringify({ locationId }),
    });
    if (!response.ok) return null;
    const item = await readJson(response);
    const stockItemId = item && typeof item === "object" ? asString(readProp(item as Record<string, unknown>, "stockItemId", "StockItemId")) : "";
    return stockItemId ? { stockItemId } : null;
  } catch { return null; }
}

export async function loadSellerOfferHistory(sellerPartyId: string, offerId: string, page = 1, pageSize = 20) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  const response = await fetch(`/v1/seller/offers/${offerId}/history?${params.toString()}`, { headers: sellerHeaders(sellerPartyId, currentActor()) });
  if (!response.ok) throw new Error("offer.history.load_failed");
  const payload = await response.json() as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number };
  const items = (Array.isArray(payload.items) ? payload.items : []).flatMap((raw) => {
    if (!raw || typeof raw !== "object") return [];
    const item = raw as Record<string, unknown>;
    return [{ offerId: asString(readProp(item, "offerId", "OfferId")), occurredAt: asString(readProp(item, "occurredAt", "OccurredAt")), kind: asString(readProp(item, "kind", "Kind")), actorUserId: asNullableString(readProp(item, "actorUserId", "ActorUserId")), summary: asString(readProp(item, "summary", "Summary")), oldValue: asNullableString(readProp(item, "oldValue", "OldValue")), newValue: asNullableString(readProp(item, "newValue", "NewValue")) }];
  });
  return { items, totalCount: Number(payload.totalCount ?? 0), page: Number(payload.page ?? page), pageSize: Number(payload.pageSize ?? pageSize) };
}
