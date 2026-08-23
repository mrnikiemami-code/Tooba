import type { ProductWorkspaceView } from "./workspace-model.ts";

/**
 * منبع خواندن UI. `error` یعنی Host در دسترس نبود یا پاسخ نامعتبر بود؛ مسیر Admin فیکسچر را جایگزین persistence نمی‌کند.
 */
export type HostReadSource = "host" | "error";

/**
 * ردیف فهرست Admin. قیمت و موجودی روی هویت Product نیستند.
 */
export interface AdminProductListRow {
  id: string;
  title: string;
  status: string;
  variantCount: number;
  offerCount: number;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  if (value == null) {
    return fallback;
  }
  return String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function asRecordArray(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value) ? value.filter((item): item is Record<string, unknown> => !!item && typeof item === "object") : [];
}


/**
 * ردیف فهرست را از JSON ترکیب Host می‌خواند. فیلد Price روی Product وجود ندارد.
 */
export function mapAdminProductList(payload: unknown): AdminProductListRow[] {
  const items = Array.isArray(payload) ? payload : [];
  return items
    .filter((item): item is Record<string, unknown> => !!item && typeof item === "object")
    .map((item) => {
      const productId = asString(readProp(item, "productId", "ProductId"));
      return {
        id: productId,
        title: asString(readProp(item, "title", "Title"), productId),
        status: asString(readProp(item, "status", "Status")),
        variantCount: asNumber(readProp(item, "variantCount", "VariantCount")),
        offerCount: asNumber(readProp(item, "offerCount", "OfferCount")),
      };
    })
    .filter((row) => row.id.length > 0);
}

/**
 * مدل نمایش Workspace را از DTO ترکیب Host می‌سازد. مبلغ متعلق به Price است نه Product.
 */
export function mapProductWorkspaceView(payload: unknown): ProductWorkspaceView | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const productId = asString(readProp(item, "productId", "ProductId"));
  if (!productId) {
    return null;
  }
  const permissionsRaw = (readProp(item, "permissions", "Permissions") ?? {}) as Record<string, unknown>;
  const seoRaw = (readProp(item, "seo", "Seo") ?? {}) as Record<string, unknown>;
  const publicationRaw = (readProp(item, "publication", "Publication") ?? {}) as Record<string, unknown>;
  return {
    productId,
    title: asString(readProp(item, "title", "Title"), "untitled"),
    status: asString(readProp(item, "status", "Status")),
    kind: asString(readProp(item, "kind", "Kind")),
    brandName: readProp(item, "brandName", "BrandName") == null ? null : asString(readProp(item, "brandName", "BrandName")),
    categoryNames: Array.isArray(readProp(item, "categoryNames", "CategoryNames"))
      ? (readProp(item, "categoryNames", "CategoryNames") as unknown[]).map((name) => asString(name))
      : [],
    variants: asRecordArray(readProp(item, "variants", "Variants")).map((variant) => ({
      variantId: asString(readProp(variant, "variantId", "VariantId")),
      fingerprint: asString(readProp(variant, "fingerprint", "Fingerprint")),
      status: asString(readProp(variant, "status", "Status")),
      offerCount: asNumber(readProp(variant, "offerCount", "OfferCount")),
    })),
    media: asRecordArray(readProp(item, "media", "Media")).map((media) => ({
      mediaAssetId: asString(readProp(media, "mediaAssetId", "MediaAssetId")),
      primary: asBoolean(readProp(media, "primary", "Primary")),
    })),
    offers: asRecordArray(readProp(item, "offers", "Offers")).map((offer) => ({
      offerId: asString(readProp(offer, "offerId", "OfferId")),
      catalogVariantId: asString(readProp(offer, "catalogVariantId", "CatalogVariantId")),
      sellerPartyId: asString(readProp(offer, "sellerPartyId", "SellerPartyId")),
      status: asString(readProp(offer, "status", "Status")),
      channel: asString(readProp(offer, "channel", "Channel")),
      sellerSku: readProp(offer, "sellerSku", "SellerSku") == null ? null : asString(readProp(offer, "sellerSku", "SellerSku")),
    })),
    prices: asRecordArray(readProp(item, "prices", "Prices")).map((price) => ({
      priceId: asString(readProp(price, "priceId", "PriceId")),
      offerId: asString(readProp(price, "offerId", "OfferId")),
      market: asString(readProp(price, "market", "Market")),
      currency: asString(readProp(price, "currency", "Currency")),
      amountExclusiveOfTax: asNumber(readProp(price, "amountExclusiveOfTax", "AmountExclusiveOfTax")),
      status: asString(readProp(price, "status", "Status")),
    })),
    taxClassifications: asRecordArray(readProp(item, "taxClassifications", "TaxClassifications")).map((row) => ({
      offerId: asString(readProp(row, "offerId", "OfferId")),
      categoryCode: asString(readProp(row, "categoryCode", "CategoryCode")),
      displayName: asString(readProp(row, "displayName", "DisplayName")),
    })),
    stock: asRecordArray(readProp(item, "stock", "Stock")).map((row) => ({
      offerId: asString(readProp(row, "offerId", "OfferId")),
      locationId: asString(readProp(row, "locationId", "LocationId")),
      locationCode: asString(readProp(row, "locationCode", "LocationCode")),
      onHand: asNumber(readProp(row, "onHand", "OnHand")),
      reserved: asNumber(readProp(row, "reserved", "Reserved")),
      available: asNumber(readProp(row, "available", "Available")),
    })),
    seo: {
      slugSeam: readProp(seoRaw, "slugSeam", "SlugSeam") == null ? null : asString(readProp(seoRaw, "slugSeam", "SlugSeam")),
      seoTitleSeam:
        readProp(seoRaw, "seoTitleSeam", "SeoTitleSeam") == null ? null : asString(readProp(seoRaw, "seoTitleSeam", "SeoTitleSeam")),
      semanticNote: asString(readProp(seoRaw, "semanticNote", "SemanticNote"), "Semantic Content != Page Composition"),
    },
    publication: {
      catalogStatus: asString(readProp(publicationRaw, "catalogStatus", "CatalogStatus")),
      purchasableHint: asBoolean(readProp(publicationRaw, "purchasableHint", "PurchasableHint")),
      checks: Array.isArray(readProp(publicationRaw, "checks", "Checks"))
        ? (readProp(publicationRaw, "checks", "Checks") as unknown[]).map((check) => asString(check))
        : [],
    },
    activity: asRecordArray(readProp(item, "activity", "Activity")).map((row) => ({
      kind: asString(readProp(row, "kind", "Kind")),
      summary: asString(readProp(row, "summary", "Summary")),
      at: asString(readProp(row, "at", "At")),
    })),
    audit: asRecordArray(readProp(item, "audit", "Audit")).map((row) => ({
      kind: asString(readProp(row, "kind", "Kind")),
      summary: asString(readProp(row, "summary", "Summary")),
      at: asString(readProp(row, "at", "At")),
    })),
    permissions: {
      canView: asBoolean(readProp(permissionsRaw, "canView", "CanView"), true),
      canEditCatalog: asBoolean(readProp(permissionsRaw, "canEditCatalog", "CanEditCatalog")),
      canEditCommercial: asBoolean(readProp(permissionsRaw, "canEditCommercial", "CanEditCommercial")),
      canEditInventory: asBoolean(readProp(permissionsRaw, "canEditInventory", "CanEditInventory")),
      canPublish: asBoolean(readProp(permissionsRaw, "canPublish", "CanPublish")),
    },
    catalogUpdatedAt: asString(readProp(item, "catalogUpdatedAt", "CatalogUpdatedAt")),
    readinessWarnings: Array.isArray(readProp(item, "readinessWarnings", "ReadinessWarnings"))
      ? (readProp(item, "readinessWarnings", "ReadinessWarnings") as unknown[]).map((warning) => asString(warning))
      : [],
    unsupportedMutations: Array.isArray(readProp(item, "unsupportedMutations", "UnsupportedMutations"))
      ? (readProp(item, "unsupportedMutations", "UnsupportedMutations") as unknown[]).map((name) => asString(name))
      : [],
  };
}

/**
 * فهرست Host را می‌خواند. قطع ارتباط یا فهرست خالی با بنر fixture اعلام می‌شود.
 */
export async function loadAdminProductList(): Promise<{ source: HostReadSource; rows: AdminProductListRow[]; message?: string }> {
  try {
    const response = await fetch("/v1/admin/products", { headers: { Accept: "application/json" } });
    if (!response.ok) {
      return { source: "error", rows: [], message: "host-list-http-" + String(response.status) };
    }
    const rows = mapAdminProductList(await response.json());
    return { source: "host", rows };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/**
 * Workspace را از Host می‌خواند. هدر scope به Host می‌رود؛ کامپوننت عمومی SpiceDB را صدا نمی‌زند.
 */
export async function loadProductWorkspace(
  productId: string,
  viewScope: boolean,
): Promise<{ source: HostReadSource; view: ProductWorkspaceView | null; message?: string }> {
  try {
    const headers: Record<string, string> = { Accept: "application/json" };
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}`, { headers });
    if (response.ok) {
      const view = mapProductWorkspaceView(await response.json());
      if (view) {
        return { source: "host", view };
      }
      return { source: "error", view: null, message: "host-workspace-invalid" };
    }
    return { source: "error", view: null, message: "host-workspace-http-" + String(response.status) };
  } catch {
    return { source: "error", view: null, message: "host-unreachable" };
  }
}

/**
 * عنوان Catalog را با قفل خوش‌بینانه به‌روز می‌کند. ۴۰۹ یعنی ردیف Catalog کهنه است.
 */
export async function patchCatalogTitle(
  productId: string,
  locale: string,
  title: string,
  expectedUpdatedAt: string,
  viewScope: boolean,
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; errorCode: string }> {
  try {
    const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "application/json" };
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}/catalog-title`, {
      method: "PATCH",
      headers,
      body: JSON.stringify({ locale, title, expectedUpdatedAt }),
    });
    if (response.status === 409) {
      return { ok: false, errorCode: "workspace.catalog.stale" };
    }
    if (response.status === 403) {
      return { ok: false, errorCode: "workspace.permission.denied" };
    }
    if (!response.ok) {
      return { ok: false, errorCode: "workspace.catalog.patch-failed" };
    }
    const view = mapProductWorkspaceView(await response.json());
    if (!view) {
      return { ok: false, errorCode: "workspace.catalog.patch-failed" };
    }
    return { ok: true, view };
  } catch {
    return { ok: false, errorCode: "workspace.host.unreachable" };
  }
}
