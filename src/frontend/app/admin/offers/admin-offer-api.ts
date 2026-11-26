import type { GridServerQuery } from "../../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../../design-system/app-data-grid/admin-grid-query-client.ts";
import { ADMIN_ACTOR_STORAGE_KEY, ADMIN_DEV_ACTOR_HEADER, adminRequestHeaders } from "../../../lib/admin/admin-result.ts";
import type {
  OfferInventoryLocation,
  OfferShippingTypeOption,
  OfferWorkspaceDetail,
} from "../../../lib/offer-workspace/types";
import { prepareAdminDevActor } from "../admin-api.ts";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? "";
}

function headers(extra?: Record<string, string>): Record<string, string> {
  return {
    ...adminRequestHeaders(),
    ...(extra ?? {}),
    "Accept-Language": typeof document !== "undefined" ? document.documentElement.lang || "fa" : "fa",
    [ADMIN_DEV_ACTOR_HEADER]: actorId(),
  };
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
  sellerDisplayName: string | null;
  variantCode: string | null;
  variantLabel: string | null;
  channel: string;
  brandName: string | null;
  primaryMediaAssetId: string | null;
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
    currency: text(prop(item, "currency", "Currency")),
    availableUnits: number(prop(item, "availableUnits", "AvailableUnits")),
    lastUpdatedAt: prop(item, "lastUpdatedAt", "LastUpdatedAt") == null ? null : text(prop(item, "lastUpdatedAt", "LastUpdatedAt")),
    sellerDisplayName: prop(item, "sellerDisplayName", "SellerDisplayName") == null ? null : text(prop(item, "sellerDisplayName", "SellerDisplayName")),
    variantCode: prop(item, "variantCode", "VariantCode") == null ? null : text(prop(item, "variantCode", "VariantCode")),
    variantLabel: prop(item, "variantLabel", "VariantLabel") == null ? null : text(prop(item, "variantLabel", "VariantLabel")),
    channel: text(prop(item, "channel", "Channel"), "Marketplace"),
    brandName: prop(item, "brandName", "BrandName") == null ? null : text(prop(item, "brandName", "BrandName")),
    primaryMediaAssetId: prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId") == null ? null : text(prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId")),
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

export type AdminOfferDetail = OfferWorkspaceDetail;

function mapInventoryLocations(value: unknown): OfferInventoryLocation[] {
  if (!Array.isArray(value)) return [];
  return value
    .filter((row): row is Record<string, unknown> => Boolean(row) && typeof row === "object")
    .map((row) => ({
      stockItemId: text(prop(row, "stockItemId", "StockItemId")),
      locationId: text(prop(row, "locationId", "LocationId")),
      locationCode: text(prop(row, "locationCode", "LocationCode")),
      locationName: text(prop(row, "locationName", "LocationName"), text(prop(row, "locationCode", "LocationCode"))),
      onHand: number(prop(row, "onHand", "OnHand")),
      reserved: number(prop(row, "reserved", "Reserved")),
      available: number(prop(row, "available", "Available")),
      revision: prop(row, "revision", "Revision") == null ? null : text(prop(row, "revision", "Revision")),
    }))
    .filter((row) => row.stockItemId.length > 0);
}

function mapRevisions(item: Record<string, unknown>) {
  const raw = record(prop(item, "revisions", "Revisions"));
  if (!raw) return null;
  return { offer: text(prop(raw, "offer", "Offer")), price: text(prop(raw, "price", "Price")), offeredQuantity: text(prop(raw, "offeredQuantity", "OfferedQuantity")) };
}
function mapCapabilities(item: Record<string, unknown>) {
  const raw = record(prop(item, "capabilities", "Capabilities"));
  if (!raw) return null;
  return {
    canEditPhysicalInventory: prop(raw, "canEditPhysicalInventory", "CanEditPhysicalInventory") === true,
    canEditOfferedQuantity: prop(raw, "canEditOfferedQuantity", "CanEditOfferedQuantity") === true,
    canAssignFulfillment: prop(raw, "canAssignFulfillment", "CanAssignFulfillment") === true,
    canEditShippingPolicy: prop(raw, "canEditShippingPolicy", "CanEditShippingPolicy") === true,
  };
}
export function mapAdminOfferDetail(value: unknown): AdminOfferDetail | null {
  const item = record(value);
  if (!item) return null;
  const offerId = text(prop(item, "offerId", "OfferId"));
  if (!offerId) return null;
  return {
    offerId,
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName")),
    catalogVariantId: text(prop(item, "catalogVariantId", "CatalogVariantId")),
    productId: prop(item, "productId", "ProductId") == null ? null : text(prop(item, "productId", "ProductId")),
    productTitle: text(prop(item, "productTitle", "ProductTitle"), "—"),
    brandName: prop(item, "brandName", "BrandName") == null ? null : text(prop(item, "brandName", "BrandName")),
    sellerSku: prop(item, "sellerSku", "SellerSku") == null ? null : text(prop(item, "sellerSku", "SellerSku")),
    status: text(prop(item, "status", "Status")),
    channel: text(prop(item, "channel", "Channel"), "Marketplace"),
    amount: nullableNumber(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency")),
    onHand: number(prop(item, "onHand", "OnHand")),
    reserved: number(prop(item, "reserved", "Reserved")),
    availableUnits: number(prop(item, "availableUnits", "AvailableUnits")),
    catalogReadOnly: prop(item, "catalogReadOnly", "CatalogReadOnly") !== false,
    returnPolicyChoice: text(prop(item, "returnPolicyChoice", "ReturnPolicyChoice"), "Default"),
    customReturnWindowDays: nullableNumber(prop(item, "customReturnWindowDays", "CustomReturnWindowDays")),
    defaultReturnWindowDays: number(prop(item, "defaultReturnWindowDays", "DefaultReturnWindowDays")) || 7,
    sellerCanOverrideReturnPolicy: prop(item, "sellerCanOverrideReturnPolicy", "SellerCanOverrideReturnPolicy") !== false,
    minReturnWindowDays: number(prop(item, "minReturnWindowDays", "MinReturnWindowDays")) || 1,
    maxReturnWindowDays: number(prop(item, "maxReturnWindowDays", "MaxReturnWindowDays")) || 30,
    allowNonReturnableOffers: prop(item, "allowNonReturnableOffers", "AllowNonReturnableOffers") !== false,
    minimumOrderQuantity: nullableNumber(prop(item, "minimumOrderQuantity", "MinimumOrderQuantity")),
    maximumOrderQuantity: nullableNumber(prop(item, "maximumOrderQuantity", "MaximumOrderQuantity")),
    productUnitCode: prop(item, "productUnitCode", "ProductUnitCode") == null ? null : text(prop(item, "productUnitCode", "ProductUnitCode")),
    productUnitName: prop(item, "productUnitName", "ProductUnitName") == null ? null : text(prop(item, "productUnitName", "ProductUnitName")),
    productUnitShortName: prop(item, "productUnitShortName", "ProductUnitShortName") == null ? null : text(prop(item, "productUnitShortName", "ProductUnitShortName")),
    unitQuantityDecimalPlaces: number(prop(item, "unitQuantityDecimalPlaces", "UnitQuantityDecimalPlaces")),
    unitQuantityStep: prop(item, "unitQuantityStep", "UnitQuantityStep") == null ? null : Number(prop(item, "unitQuantityStep", "UnitQuantityStep")),
    unitRoundingMode: prop(item, "unitRoundingMode", "UnitRoundingMode") == null ? null : text(prop(item, "unitRoundingMode", "UnitRoundingMode")),
    inventoryLocations: mapInventoryLocations(prop(item, "inventoryLocations", "InventoryLocations")),
    variantCode: prop(item, "variantCode", "VariantCode") == null ? null : text(prop(item, "variantCode", "VariantCode")),
    variantLabel: prop(item, "variantLabel", "VariantLabel") == null ? null : text(prop(item, "variantLabel", "VariantLabel")),
    primaryMediaAssetId: prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId") == null ? null : text(prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId")),
    fulfillmentResponsibility: (() => {
      const raw = text(prop(item, "fulfillmentResponsibility", "FulfillmentResponsibility"));
      return raw === "Seller" || raw === "Platform" ? raw : null;
    })(),
    fulfillmentShippingTypeId: prop(item, "fulfillmentShippingTypeId", "FulfillmentShippingTypeId") == null ? null : text(prop(item, "fulfillmentShippingTypeId", "FulfillmentShippingTypeId")),
    lastUpdatedAt: prop(item, "lastUpdatedAt", "LastUpdatedAt") == null ? null : text(prop(item, "lastUpdatedAt", "LastUpdatedAt")),
    offerRevision: prop(item, "offerRevision", "OfferRevision") == null ? null : text(prop(item, "offerRevision", "OfferRevision")),
    priceRevision: prop(item, "priceRevision", "PriceRevision") == null ? null : text(prop(item, "priceRevision", "PriceRevision")),
    offeredQuantity: nullableNumber(prop(item, "offeredQuantity", "OfferedQuantity")),
    allocationHeld: nullableNumber(prop(item, "allocationHeld", "AllocationHeld")),
    allocationRevision: prop(item, "allocationRevision", "AllocationRevision") == null ? null : text(prop(item, "allocationRevision", "AllocationRevision")),
    revisions: mapRevisions(item),
    capabilities: mapCapabilities(item),

  };
}

export async function loadAdminOfferDetail(offerId: string): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}`, { headers: headers() });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) return { ok: false, errorCode: `admin.offer.http.${response.status}` };
    const detail = mapAdminOfferDetail(await response.json());
    return detail ? { ok: true, detail } : { ok: false, errorCode: "admin.offer.invalid-response" };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function createAdminOffer(input: {
  sellerPartyId: string;
  catalogVariantId: string;
  sellerSku?: string | null;
  channel?: string | null;
  returnPolicyChoice?: string | null;
  customReturnWindowDays?: number | null;
  minimumOrderQuantity?: number | null;
  maximumOrderQuantity?: number | null;
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
        channel: input.channel ?? undefined,
        returnPolicyChoice: input.returnPolicyChoice ?? undefined,
        customReturnWindowDays: input.customReturnWindowDays ?? undefined,
        minimumOrderQuantity: input.minimumOrderQuantity ?? undefined,
        maximumOrderQuantity: input.maximumOrderQuantity ?? undefined,
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
    const detail = mapAdminOfferDetail(item);
    return detail ? { ok: true, detail } : { ok: false, errorCode: "admin.offer.invalid-response" };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}


export async function patchAdminOffer(
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
): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}`, {
      method: "PATCH",
      headers: headers({ "Content-Type": "application/json" }),
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
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "admin.offer.patch-failed" };
    }
    const detail = mapAdminOfferDetail(await response.json());
    if (!detail || detail.offerId !== offerId) return { ok: false, errorCode: "admin.offer.response_mismatch" };
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function writeAdminOfferPrice(
  offerId: string,
  input: { amount: number; currency?: string; market?: string },
  expectedVersion?: string,
): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const currency = input.currency || "IRR";
    const response = await fetch(`/v1/admin/offers/${offerId}/price`, {
      method: "PUT",
      headers: headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({ amount: input.amount, currency, market: input.market ?? "IR", expectedVersion }),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "admin.price.write-failed" };
    }
    const detail = mapAdminOfferDetail(await response.json());
    if (!detail || detail.offerId !== offerId || detail.amount !== input.amount || detail.currency !== currency) {
      return { ok: false, errorCode: "admin.price.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function writeAdminOfferedQuantity(
  offerId: string,
  quantity: number,
  expectedVersion: string,
  operationId: string,
): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}/offered-quantity`, {
      method: "PUT",
      headers: headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({ quantity, expectedVersion, operationId }),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) { const body = await response.json().catch(() => null) as { errorCode?: string } | null; return { ok: false, errorCode: body?.errorCode ?? "admin.offered-quantity.write-failed" }; }
    const detail = mapAdminOfferDetail(await response.json());
    return detail && detail.offerId === offerId && detail.offeredQuantity === quantity ? { ok: true, detail } : { ok: false, errorCode: "admin.offered-quantity.response_mismatch" };
  } catch { return { ok: false, errorCode: "host-unreachable" }; }
}

export async function writeAdminOfferInventory(
  offerId: string,
  input: { onHand: number; reason: string; stockItemId: string },
  expectedVersion?: string,
): Promise<{ ok: true; detail: AdminOfferDetail } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}/inventory`, {
      method: "PUT",
      headers: headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({ onHand: input.onHand, reason: input.reason, stockItemId: input.stockItemId, expectedVersion }),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "admin.inventory.write-failed" };
    }
    const detail = mapAdminOfferDetail(await response.json());
    const target = detail?.inventoryLocations.find((row) => row.stockItemId === input.stockItemId);
    if (!detail || detail.offerId !== offerId || !target || target.onHand !== input.onHand) {
      return { ok: false, errorCode: "admin.inventory.response_mismatch" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function assignAdminOfferFulfillment(
  offerId: string,
  input: { responsibility: "Seller" | "Platform" | null; shippingTypeId: string | null },
  expectedVersion?: string,
): Promise<{ ok: true } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    await prepareAdminDevActor();
    const response = await fetch(`/v1/admin/offers/${offerId}/fulfillment`, {
      method: "PUT",
      headers: headers({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        responsibility: input.responsibility,
        shippingTypeId: input.shippingTypeId,
        expectedVersion,
      }),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "admin.fulfillment.write-failed" };
    }
    const item = record(await response.json().catch(() => null));
    const returnedId = item ? text(prop(item, "offerId", "OfferId")) : "";
    if (returnedId && returnedId !== offerId) return { ok: false, errorCode: "admin.fulfillment.response_mismatch" };
    return { ok: true };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export async function loadAdminShippingTypes(): Promise<OfferShippingTypeOption[]> {
  await prepareAdminDevActor();
  const response = await fetch("/v1/admin/shipping-types", { headers: headers() });
  if (!response.ok) return [];
  const payload = await response.json().catch(() => null);
  if (!Array.isArray(payload)) return [];
  return payload
    .filter((row): row is Record<string, unknown> => Boolean(row) && typeof row === "object")
    .map((row) => {
      const translations = Array.isArray(row.translations) ? row.translations : Array.isArray(row.Translations) ? row.Translations : [];
      const first = translations.find((item): item is Record<string, unknown> => Boolean(item) && typeof item === "object");
      const label = first ? text(prop(first, "name", "Name")) : "";
      return {
        shippingTypeId: text(prop(row, "shippingTypeId", "ShippingTypeId")),
        code: text(prop(row, "code", "Code")),
        label: label || text(prop(row, "code", "Code")),
        isActive: prop(row, "isActive", "IsActive") !== false,
      };
    })
    .filter((row) => row.shippingTypeId.length > 0);
}

export interface AdminCatalogProductOption {
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

export interface AdminCatalogProductPage {
  rows: AdminCatalogProductOption[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface AdminCatalogVariantOption {
  catalogVariantId: string;
  productId: string;
  displayLabel: string;
  axisLabels: Array<{ name: string; value: string }>;
  catalogCode: string | null;
  status: string;
}

export async function searchAdminCatalogProducts(
  search = "",
  page = 1,
  pageSize = 20,
  locale = "fa-IR",
): Promise<AdminCatalogProductPage> {
  await prepareAdminDevActor();
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize), locale });
  if (search.trim()) params.set("search", search.trim());
  const response = await fetch(`/v1/admin/catalog/products?${params.toString()}`, { headers: headers() });
  if (!response.ok) return { rows: [], totalCount: 0, page, pageSize, hasMore: false };
  const payload = await response.json() as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; hasMore?: boolean };
  const rows = (Array.isArray(payload.items) ? payload.items : []).flatMap((raw): AdminCatalogProductOption[] => {
    const item = record(raw);
    if (!item) return [];
    const productId = text(prop(item, "productId", "ProductId"));
    if (!productId) return [];
    return [{
      productId,
      title: text(prop(item, "title", "Title"), "—"),
      brandName: prop(item, "brandName", "BrandName") == null ? null : text(prop(item, "brandName", "BrandName")),
      primaryMediaAssetId: prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId") == null ? null : text(prop(item, "primaryMediaAssetId", "PrimaryMediaAssetId")),
      publishedVariantCount: number(prop(item, "publishedVariantCount", "PublishedVariantCount")),
      unitCode: prop(item, "unitCode", "UnitCode") == null ? null : text(prop(item, "unitCode", "UnitCode")),
      unitName: prop(item, "unitName", "UnitName") == null ? null : text(prop(item, "unitName", "UnitName")),
      quantityDecimalPlaces: number(prop(item, "quantityDecimalPlaces", "QuantityDecimalPlaces")),
      quantityStep: prop(item, "quantityStep", "QuantityStep") == null ? null : Number(prop(item, "quantityStep", "QuantityStep")),
      roundingMode: prop(item, "roundingMode", "RoundingMode") == null ? null : text(prop(item, "roundingMode", "RoundingMode")),
    }];
  });
  return {
    rows,
    totalCount: Number(payload.totalCount ?? 0),
    page: Number(payload.page ?? page),
    pageSize: Number(payload.pageSize ?? pageSize),
    hasMore: Boolean(payload.hasMore),
  };
}

export async function loadAdminProductVariants(productId: string, locale = "fa-IR"): Promise<AdminCatalogVariantOption[]> {
  await prepareAdminDevActor();
  const params = new URLSearchParams({ locale });
  const response = await fetch(`/v1/admin/catalog/products/${productId}/variants?${params.toString()}`, { headers: headers() });
  if (!response.ok) return [];
  const payload = await response.json();
  const items = Array.isArray(payload) ? payload : [];
  return items.flatMap((raw): AdminCatalogVariantOption[] => {
    const item = record(raw);
    if (!item) return [];
    const catalogVariantId = text(prop(item, "catalogVariantId", "CatalogVariantId"));
    if (!catalogVariantId) return [];
    const axisRaw = prop(item, "axisLabels", "AxisLabels");
    const axisLabels = Array.isArray(axisRaw)
      ? axisRaw.flatMap((axis) => {
          const row = record(axis);
          if (!row) return [];
          return [{ name: text(prop(row, "name", "Name")), value: text(prop(row, "value", "Value")) }];
        })
      : [];
    return [{
      catalogVariantId,
      productId: text(prop(item, "productId", "ProductId")),
      displayLabel: text(prop(item, "displayLabel", "DisplayLabel")),
      axisLabels,
      catalogCode: prop(item, "catalogCode", "CatalogCode") == null ? null : text(prop(item, "catalogCode", "CatalogCode")),
      status: text(prop(item, "status", "Status")),
    }];
  });
}

export async function findAdminActiveOffer(
  sellerPartyId: string,
  catalogVariantId: string,
  channel?: string | null,
): Promise<{ offerId: string } | null> {
  await prepareAdminDevActor();
  const params = new URLSearchParams({ sellerPartyId, catalogVariantId });
  if (channel) params.set("channel", channel);
  try {
    const response = await fetch(`/v1/admin/offers/active-listing?${params.toString()}`, { headers: headers() });
    if (!response.ok) return null;
    const item = record(await response.json());
    const offerId = item ? text(prop(item, "offerId", "OfferId")) : "";
    return offerId ? { offerId } : null;
  } catch {
    return null;
  }
}

export async function loadAdminAllowedOfferChannels(): Promise<string[]> {
  await prepareAdminDevActor();
  try {
    const response = await fetch("/v1/admin/offers/allowed-channels", { headers: headers() });
    if (!response.ok) return [];
    const payload = await response.json();
    return Array.isArray(payload) ? payload.filter((x): x is string => typeof x === "string") : [];
  } catch { return []; }
}

export interface AdminInventoryLocationOption { locationId: string; code: string; name: string; }

export async function loadAdminInventoryLocations(): Promise<AdminInventoryLocationOption[]> {
  await prepareAdminDevActor();
  try {
    const response = await fetch("/v1/admin/offers/inventory-locations", { headers: headers() });
    if (!response.ok) return [];
    const payload = await response.json();
    return Array.isArray(payload) ? payload.flatMap((raw) => {
      const item = record(raw); if (!item) return [];
      const locationId = text(prop(item, "locationId", "LocationId")); if (!locationId) return [];
      return [{ locationId, code: text(prop(item, "code", "Code")), name: text(prop(item, "name", "Name")) }];
    }) : [];
  } catch { return []; }
}

export async function ensureAdminInventoryPosition(offerId: string, locationId: string): Promise<{ stockItemId: string } | null> {
  await prepareAdminDevActor();
  try {
    const response = await fetch(`/v1/admin/offers/${offerId}/inventory/positions`, {
      method: "POST", headers: headers({ "Content-Type": "application/json" }), body: JSON.stringify({ locationId }),
    });
    if (!response.ok) return null;
    const item = record(await response.json());
    const stockItemId = item ? text(prop(item, "stockItemId", "StockItemId")) : "";
    return stockItemId ? { stockItemId } : null;
  } catch { return null; }
}

export async function loadAdminOfferHistory(offerId: string, page = 1, pageSize = 20) {
  await prepareAdminDevActor();
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  const response = await fetch(`/v1/admin/offers/${offerId}/history?${params.toString()}`, { headers: headers() });
  if (!response.ok) throw new Error("offer.history.load_failed");
  const payload = await response.json() as { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number };
  const items = (Array.isArray(payload.items) ? payload.items : []).flatMap((raw) => {
    const item = record(raw); if (!item) return [];
    return [{ offerId: text(prop(item, "offerId", "OfferId")), occurredAt: text(prop(item, "occurredAt", "OccurredAt")), kind: text(prop(item, "kind", "Kind")), actorUserId: prop(item, "actorUserId", "ActorUserId") == null ? null : text(prop(item, "actorUserId", "ActorUserId")), summary: text(prop(item, "summary", "Summary")), oldValue: prop(item, "oldValue", "OldValue") == null ? null : text(prop(item, "oldValue", "OldValue")), newValue: prop(item, "newValue", "NewValue") == null ? null : text(prop(item, "newValue", "NewValue")) }];
  });
  return { items, totalCount: Number(payload.totalCount ?? 0), page: Number(payload.page ?? page), pageSize: Number(payload.pageSize ?? pageSize) };
}
