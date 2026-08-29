import type { ProductWorkspaceView } from "./workspace-model.ts";
import { adminHeaders } from "./admin-api.ts";
import type { GridServerQuery, GridServerPage } from "../../design-system/data-grid/types";
import { fromHostGridPage, toHostGridQuery } from "../../design-system/app-data-grid/grid-query-mapper.ts";
import type { ProductSeoDetail, ProductSeoReadiness } from "./product-seo-panel-model.ts";
import { mapSeoDetail, mapSeoReadiness } from "./product-seo-panel-model.ts";
import type { ProductPublishReadiness } from "./product-publishing-panel-model.ts";
import { mapPublishReadiness } from "./product-publishing-panel-model.ts";
import type { ProductHistoryPage } from "./product-history-panel-model.ts";
import { mapProductHistoryPage } from "./product-history-panel-model.ts";

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
  categorySummary: string;
  offerAmountRange: string;
  sellableUnits: number;
  locationCount: number;
  updatedAt: string;
  primaryMediaAssetId: string | null;
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
        categorySummary: asString(readProp(item, "categorySummary", "CategorySummary"), "بدون دسته"),
        offerAmountRange: asString(readProp(item, "offerAmountRange", "OfferAmountRange"), "بدون مبلغ"),
        sellableUnits: asNumber(readProp(item, "sellableUnits", "SellableUnits")),
        locationCount: asNumber(readProp(item, "locationCount", "LocationCount")),
        updatedAt: asString(readProp(item, "updatedAt", "UpdatedAt")),
        primaryMediaAssetId: (() => {
          const raw = readProp(item, "primaryMediaAssetId", "PrimaryMediaAssetId");
          const text = asString(raw);
          return text.length > 0 ? text : null;
        })(),
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
  const primaryCategoryRaw = readProp(item, "primaryCategoryId", "PrimaryCategoryId");
  const primaryCategoryText = asString(primaryCategoryRaw);
  const categoryPathRaw = readProp(item, "categoryPath", "CategoryPath");
  const isAssignableRaw = readProp(item, "isPrimaryCategoryAssignable", "IsPrimaryCategoryAssignable");
  const slugRaw = readProp(item, "slug", "Slug");
  const shortDescriptionRaw = readProp(item, "shortDescription", "ShortDescription");
  return {
    productId,
    title: asString(readProp(item, "title", "Title"), "untitled"),
    status: asString(readProp(item, "status", "Status")),
    kind: asString(readProp(item, "kind", "Kind")),
    brandName: readProp(item, "brandName", "BrandName") == null ? null : asString(readProp(item, "brandName", "BrandName")),
    brandId: (() => {
      const raw = readProp(item, "brandId", "BrandId");
      const text = asString(raw);
      return text.length > 0 ? text : null;
    })(),
    categoryNames: Array.isArray(readProp(item, "categoryNames", "CategoryNames"))
      ? (readProp(item, "categoryNames", "CategoryNames") as unknown[]).map((name) => asString(name))
      : [],
    primaryCategoryId: primaryCategoryText.length > 0 ? primaryCategoryText : null,
    categoryPath: categoryPathRaw == null || asString(categoryPathRaw).length === 0 ? null : asString(categoryPathRaw),
    isPrimaryCategoryAssignable: Boolean(isAssignableRaw),
    slug: slugRaw == null || asString(slugRaw).length === 0 ? null : asString(slugRaw),
    shortDescription:
      shortDescriptionRaw == null || asString(shortDescriptionRaw).length === 0
        ? null
        : asString(shortDescriptionRaw),
    translations: asRecordArray(readProp(item, "translations", "Translations")).map((row) => ({
      locale: asString(readProp(row, "locale", "Locale")),
      name: asString(readProp(row, "name", "Name")),
      slug: (() => {
        const raw = readProp(row, "slug", "Slug");
        const text = asString(raw);
        return text.length > 0 ? text : null;
      })(),
      shortDescription: (() => {
        const raw = readProp(row, "shortDescription", "ShortDescription");
        return raw == null ? null : asString(raw);
      })(),
      description: (() => {
        const raw = readProp(row, "description", "Description");
        return raw == null ? null : asString(raw);
      })(),
      seoTitle: (() => {
        const raw = readProp(row, "seoTitle", "SeoTitle");
        return raw == null ? null : asString(raw);
      })(),
      seoDescription: (() => {
        const raw = readProp(row, "seoDescription", "SeoDescription");
        return raw == null ? null : asString(raw);
      })(),
    })),
    variants: asRecordArray(readProp(item, "variants", "Variants")).map((variant) => ({
      variantId: asString(readProp(variant, "variantId", "VariantId")),
      fingerprint: asString(readProp(variant, "fingerprint", "Fingerprint")),
      status: asString(readProp(variant, "status", "Status")),
      catalogCodeSeam: (() => {
        const raw = readProp(variant, "catalogCodeSeam", "CatalogCodeSeam");
        const text = asString(raw);
        return text.length > 0 ? text : null;
      })(),
      offerCount: asNumber(readProp(variant, "offerCount", "OfferCount")),
    })),
    media: asRecordArray(readProp(item, "media", "Media")).map((media) => ({
      mediaAssetId: asString(readProp(media, "mediaAssetId", "MediaAssetId")),
      primary: asBoolean(readProp(media, "primary", "Primary")),
      displayOrder: asNumber(readProp(media, "displayOrder", "DisplayOrder")),
      altText: (() => {
        const raw = readProp(media, "altText", "AltText");
        return raw == null ? null : asString(raw);
      })(),
    })),
    offers: asRecordArray(readProp(item, "offers", "Offers")).map((offer) => ({
      offerId: asString(readProp(offer, "offerId", "OfferId")),
      catalogVariantId: asString(readProp(offer, "catalogVariantId", "CatalogVariantId")),
      sellerPartyId: asString(readProp(offer, "sellerPartyId", "SellerPartyId")),
      sellerDisplayName: asString(readProp(offer, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
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
      locationName: asString(readProp(row, "locationName", "LocationName"), asString(readProp(row, "locationCode", "LocationCode"))),
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
      statusUpdatedAt: readProp(publicationRaw, "statusUpdatedAt", "StatusUpdatedAt") == null
        ? null
        : asString(readProp(publicationRaw, "statusUpdatedAt", "StatusUpdatedAt")),
      aggregateReadiness: mapPublishReadiness(readProp(publicationRaw, "aggregateReadiness", "AggregateReadiness")),
    },
    activity: asRecordArray(readProp(item, "activity", "Activity")).map((row) => ({
      kind: asString(readProp(row, "kind", "Kind")),
      summary: asString(readProp(row, "summary", "Summary")),
      at: asString(readProp(row, "at", "At")),
      actor: readProp(row, "actor", "Actor") == null ? undefined : asString(readProp(row, "actor", "Actor")),
      section: readProp(row, "section", "Section") == null ? undefined : asString(readProp(row, "section", "Section")),
      beforeSummary:
        readProp(row, "beforeSummary", "BeforeSummary") == null
          ? undefined
          : asString(readProp(row, "beforeSummary", "BeforeSummary")),
      afterSummary:
        readProp(row, "afterSummary", "AfterSummary") == null
          ? undefined
          : asString(readProp(row, "afterSummary", "AfterSummary")),
    })),
    audit: asRecordArray(readProp(item, "audit", "Audit")).map((row) => ({
      kind: asString(readProp(row, "kind", "Kind")),
      summary: asString(readProp(row, "summary", "Summary")),
      at: asString(readProp(row, "at", "At")),
      actor: readProp(row, "actor", "Actor") == null ? undefined : asString(readProp(row, "actor", "Actor")),
      section: readProp(row, "section", "Section") == null ? undefined : asString(readProp(row, "section", "Section")),
      beforeSummary:
        readProp(row, "beforeSummary", "BeforeSummary") == null
          ? undefined
          : asString(readProp(row, "beforeSummary", "BeforeSummary")),
      afterSummary:
        readProp(row, "afterSummary", "AfterSummary") == null
          ? undefined
          : asString(readProp(row, "afterSummary", "AfterSummary")),
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
export async function loadAdminProductList(): Promise<{ source: HostReadSource; rows: AdminProductListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/admin/products", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", rows: [], message: "admin.authorization.denied", denied: true };
    }
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
 * فهرست محصولات Admin با قرارداد GridQuery/GridPage — server-side paging/filter/sort.
 */
export async function queryAdminProductGrid(
  query: GridServerQuery,
): Promise<{ source: HostReadSource; page: GridServerPage<AdminProductListRow>; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/admin/products/query", {
      method: "POST",
      headers: { ...adminHeaders(), "Content-Type": "application/json" },
      body: JSON.stringify(toHostGridQuery(query)),
    });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", page: { rows: [], total: 0 }, message: "admin.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { source: "error", page: { rows: [], total: 0 }, message: "host-grid-http-" + String(response.status) };
    }
    const payload = (await response.json()) as {
      items?: unknown[];
      totalCount?: number;
      page?: number;
      pageSize?: number;
    };
    const page = fromHostGridPage(
      {
        items: payload.items ?? [],
        page: payload.page ?? query.page,
        pageSize: payload.pageSize ?? query.pageSize,
        totalCount: payload.totalCount ?? 0,
      },
      (item) => mapAdminProductList([item])[0],
    );
    return { source: "host", page };
  } catch {
    return { source: "error", page: { rows: [], total: 0 }, message: "host-unreachable" };
  }
}

/**
 * Workspace را از Host می‌خواند. هدر scope به Host می‌رود؛ کامپوننت عمومی SpiceDB را صدا نمی‌زند.
 */
export async function loadProductWorkspace(
  productId: string,
  viewScope: boolean,
): Promise<{ source: HostReadSource; view: ProductWorkspaceView | null; message?: string; denied?: boolean }> {
  try {
    const headers = adminHeaders();
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}`, { headers });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", view: null, message: "admin.authorization.denied", denied: true };
    }
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
    const headers = adminHeaders({ "Content-Type": "application/json" });
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

/** ایجاد سادهٔ محصول Catalog به‌صورت پیش‌نویس؛ قیمت/موجودی اینجا نیست. */
export async function createAdminProduct(input: {
  title: string;
  slug?: string | null;
  categoryId?: string | null;
  locale?: string | null;
}): Promise<{ ok: true; productId: string } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/admin/products", {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        title: input.title,
        slug: input.slug ?? undefined,
        categoryId: input.categoryId ?? undefined,
        locale: input.locale ?? "fa-IR",
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, errorCode: "admin.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "workspace.product.create-failed" };
    }
    const payload = (await response.json()) as Record<string, unknown>;
    const productId = asString(readProp(payload, "productId", "ProductId"));
    return productId
      ? { ok: true, productId }
      : { ok: false, errorCode: "workspace.product.create-failed" };
  } catch {
    return { ok: false, errorCode: "workspace.host.unreachable" };
  }
}

export interface AdminProductCoreUpdateInput {
  locale: string;
  title: string;
  slug?: string | null;
  shortDescription?: string | null;
  description?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  expectedUpdatedAt: string;
}

/** به‌روزرسانی هستهٔ محصول (عنوان، slug انسانی، شرح، SEO) برای یک locale. */
export async function updateAdminProductCore(
  productId: string,
  input: AdminProductCoreUpdateInput,
  viewScope = false,
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; errorCode: string }> {
  try {
    const headers = adminHeaders({ "Content-Type": "application/json" });
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}/core`, {
      method: "PATCH",
      headers,
      body: JSON.stringify({
        locale: input.locale,
        title: input.title,
        slug: input.slug ?? null,
        shortDescription: input.shortDescription ?? null,
        description: input.description ?? null,
        seoTitle: input.seoTitle ?? null,
        seoDescription: input.seoDescription ?? null,
        expectedUpdatedAt: input.expectedUpdatedAt,
      }),
    });
    if (response.status === 409) {
      return { ok: false, errorCode: "workspace.catalog.stale" };
    }
    if (response.status === 403) {
      return { ok: false, errorCode: "workspace.permission.denied" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      return { ok: false, errorCode: body?.errorCode ?? "workspace.product.core-failed" };
    }
    const view = mapProductWorkspaceView(await response.json());
    if (!view) {
      return { ok: false, errorCode: "workspace.product.core-failed" };
    }
    return { ok: true, view };
  } catch {
    return { ok: false, errorCode: "workspace.host.unreachable" };
  }
}

/** انتساب / تغییر دستهٔ محصول با تأیید صریح اثر schema. */
export async function assignAdminProductCategory(
  productId: string,
  input: { categoryId: string; confirmSchemaImpact: boolean; expectedUpdatedAt: string },
  viewScope = false,
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; errorCode: string }> {
  try {
    const headers = adminHeaders({ "Content-Type": "application/json" });
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}/category`, {
      method: "PUT",
      headers,
      body: JSON.stringify({
        categoryId: input.categoryId,
        confirmSchemaImpact: input.confirmSchemaImpact,
        expectedUpdatedAt: input.expectedUpdatedAt,
      }),
    });
    if (response.status === 403) {
      return { ok: false, errorCode: "workspace.permission.denied" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      const code = body?.errorCode?.trim();
      if (code) return { ok: false, errorCode: code };
      if (response.status === 409) return { ok: false, errorCode: "workspace.catalog.stale" };
      return { ok: false, errorCode: "workspace.product.category-failed" };
    }
    const view = mapProductWorkspaceView(await response.json());
    if (!view) {
      return { ok: false, errorCode: "workspace.product.category-failed" };
    }
    return { ok: true, view };
  } catch {
    return { ok: false, errorCode: "workspace.host.unreachable" };
  }
}

/** انتساب یا حذف برند Catalog. */
export async function assignAdminProductBrand(
  productId: string,
  input: { brandId: string | null; expectedUpdatedAt: string },
  viewScope = false,
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; errorCode: string }> {
  try {
    const headers = adminHeaders({ "Content-Type": "application/json" });
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}/brand`, {
      method: "PUT",
      headers,
      body: JSON.stringify({
        brandId: input.brandId,
        expectedUpdatedAt: input.expectedUpdatedAt,
      }),
    });
    if (response.status === 403) {
      return { ok: false, errorCode: "workspace.permission.denied" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string } | null;
      const code = body?.errorCode?.trim();
      if (code) return { ok: false, errorCode: code };
      if (response.status === 409) return { ok: false, errorCode: "workspace.catalog.stale" };
      return { ok: false, errorCode: "workspace.product.brand-failed" };
    }
    const view = mapProductWorkspaceView(await response.json());
    if (!view) {
      return { ok: false, errorCode: "workspace.product.brand-failed" };
    }
    return { ok: true, view };
  } catch {
    return { ok: false, errorCode: "workspace.host.unreachable" };
  }
}

export type AdminBrandOption = { brandId: string; name: string; status: string };

/** فهرست برندها برای انتخابگر محصول. */
export async function listAdminBrandOptions(
  search?: string,
): Promise<{ ok: true; items: AdminBrandOption[] } | { ok: false; message: string }> {
  try {
    const q = search?.trim() ? `?q=${encodeURIComponent(search.trim())}` : "";
    const response = await fetch(`/v1/admin/products/brand-options${q}`, { headers: adminHeaders() });
    if (!response.ok) {
      return { ok: false, message: "فهرست برند خوانده نشد" };
    }
    const payload = await response.json();
    const rows = Array.isArray(payload) ? payload : [];
    return {
      ok: true,
      items: rows.map((row) => {
        const item = row as Record<string, unknown>;
        return {
          brandId: asString(readProp(item, "brandId", "BrandId")),
          name: asString(readProp(item, "name", "Name"), "برند"),
          status: asString(readProp(item, "status", "Status")),
        };
      }),
    };
  } catch {
    return { ok: false, message: "اتصال برقرار نشد" };
  }
}

export type AdminProductLifecycleAction = "publish" | "unpublish" | "archive" | "restore" | "delete";

/**
 * انتشار / لغو انتشار / بایگانی / بازگردانی / حذف امن محصول. حذف از DELETE؛ بقیه POST.
 */
export async function mutateAdminProductLifecycle(
  productId: string,
  action: AdminProductLifecycleAction,
): Promise<{ ok: true; view?: ProductWorkspaceView } | { ok: false; message: string }> {
  try {
    const method = action === "delete" ? "DELETE" : "POST";
    const path =
      action === "delete"
        ? `/v1/admin/products/${productId}`
        : `/v1/admin/products/${productId}/${action}`;
    const response = await fetch(path, {
      method,
      headers: adminHeaders({ "Content-Type": "application/json" }),
    });
    if (response.status === 404) {
      return { ok: false, message: "این عملیات هنوز روی Host فعال نیست" };
    }
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (response.status === 409) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? "حذف به دلیل ارجاع ممکن نیست؛ محصول آرشیو شد" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    if (action === "delete" || response.status === 204) {
      return { ok: true };
    }
    const view = mapProductWorkspaceView(await response.json().catch(() => null));
    return view ? { ok: true, view } : { ok: true };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

function mapMediaList(payload: unknown): ProductWorkspaceView["media"] {
  return asRecordArray(payload).map((media) => ({
    mediaAssetId: asString(readProp(media, "mediaAssetId", "MediaAssetId")),
    primary: asBoolean(readProp(media, "primary", "Primary")),
    displayOrder: asNumber(readProp(media, "displayOrder", "DisplayOrder")),
    altText: (() => {
      const raw = readProp(media, "altText", "AltText");
      return raw == null ? null : asString(raw);
    })(),
  }));
}

async function readMediaMutation(
  response: Response,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  if (response.status === 401 || response.status === 403) {
    return { ok: false, message: "دسترسی مجاز نیست" };
  }
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
    return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
  }
  return { ok: true, media: mapMediaList(await response.json()) };
}

/** فهرست گالری رسانهٔ محصول. */
export async function listAdminProductMedia(
  productId: string,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media`, {
      headers: adminHeaders(),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** آمادگی گالری رسانه. */
export async function getAdminProductMediaReadiness(
  productId: string,
): Promise<
  | {
      ok: true;
      readiness: {
        hasPrimaryImage: boolean;
        mediaCount: number;
        isReady: boolean;
        messageFa: string | null;
      };
    }
  | { ok: false; message: string }
> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/readiness`, {
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const raw = (await response.json()) as Record<string, unknown>;
    const messageRaw = readProp(raw, "messageFa", "MessageFa");
    return {
      ok: true,
      readiness: {
        hasPrimaryImage: asBoolean(readProp(raw, "hasPrimaryImage", "HasPrimaryImage")),
        mediaCount: asNumber(readProp(raw, "mediaCount", "MediaCount")),
        isReady: asBoolean(readProp(raw, "isReady", "IsReady")),
        messageFa: messageRaw == null ? null : asString(messageRaw),
      },
    };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

export type AdminProductSeoUpdateInput = {
  locale: string;
  slug?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  expectedUpdatedAt: string;
};

/** جزئیات SEO محصول برای یک locale. */
export async function getAdminProductSeo(
  productId: string,
  locale: string,
  viewScope = false,
): Promise<{ ok: true; detail: ProductSeoDetail } | { ok: false; message: string }> {
  try {
    const headers = adminHeaders();
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const q = new URLSearchParams({ locale });
    const response = await fetch(`/v1/admin/products/${productId}/seo?${q}`, { headers });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const detail = mapSeoDetail((await response.json()) as Record<string, unknown>);
    if (!detail) {
      return { ok: false, message: "پاسخ SEO نامعتبر است" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** آمادگی SEO محصول. */
export async function getAdminProductSeoReadiness(
  productId: string,
  locale: string,
  viewScope = false,
): Promise<{ ok: true; readiness: ProductSeoReadiness } | { ok: false; message: string }> {
  try {
    const headers = adminHeaders();
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const q = new URLSearchParams({ locale });
    const response = await fetch(`/v1/admin/products/${productId}/seo/readiness?${q}`, { headers });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    return { ok: true, readiness: mapSeoReadiness((await response.json()) as Record<string, unknown>) };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** آمادگی تجمیعی انتشار محصول (Catalog-only). */
export async function getAdminProductPublishReadiness(
  productId: string,
  locale = "fa-IR",
  viewScope = false,
): Promise<{ ok: true; readiness: ProductPublishReadiness } | { ok: false; message: string }> {
  try {
    const headers = adminHeaders();
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const q = new URLSearchParams({ locale });
    const response = await fetch(`/v1/admin/products/${productId}/publish/readiness?${q}`, { headers });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const readiness = mapPublishReadiness(await response.json());
    if (!readiness) {
      return { ok: false, message: "پاسخ آمادگی انتشار نامعتبر است" };
    }
    return { ok: true, readiness };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** صفحهٔ تاریخچهٔ محصول برای تب تاریخچه Workspace. */
export async function getAdminProductHistory(
  productId: string,
  opts?: { skip?: number; take?: number; section?: string; viewScope?: boolean },
): Promise<{ ok: true; page: ProductHistoryPage } | { ok: false; message: string }> {
  try {
    const headers = adminHeaders();
    if (opts?.viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const q = new URLSearchParams({
      skip: String(opts?.skip ?? 0),
      take: String(opts?.take ?? 50),
    });
    const section = opts?.section?.trim();
    if (section) {
      q.set("section", section);
    }
    const response = await fetch(`/v1/admin/products/${productId}/history?${q}`, { headers });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const page = mapProductHistoryPage(await response.json());
    if (!page) {
      return { ok: false, message: "پاسخ تاریخچه نامعتبر است" };
    }
    return { ok: true, page };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** به‌روزرسانی SEO محصول. */
export async function updateAdminProductSeo(
  productId: string,
  input: AdminProductSeoUpdateInput,
  viewScope = false,
): Promise<{ ok: true; detail: ProductSeoDetail } | { ok: false; message: string }> {
  try {
    const headers = adminHeaders({ "Content-Type": "application/json" });
    if (viewScope) {
      headers["X-Tooba-Workspace-Scope"] = "view";
    }
    const response = await fetch(`/v1/admin/products/${productId}/seo`, {
      method: "PUT",
      headers,
      body: JSON.stringify({
        locale: input.locale,
        slug: input.slug ?? null,
        seoTitle: input.seoTitle ?? null,
        seoDescription: input.seoDescription ?? null,
        expectedUpdatedAt: input.expectedUpdatedAt,
      }),
    });
    if (response.status === 409) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      if (body?.errorCode === "workspace.product.slug.duplicate") {
        return { ok: false, message: body.title ?? "این نشانی صفحه قبلاً استفاده شده است." };
      }
      return { ok: false, message: body?.title ?? "تداخل ذخیره — صفحه را تازه کنید" };
    }
    if (response.status === 403) {
      return { ok: false, message: "دسترسی ویرایش ندارید" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const detail = mapSeoDetail((await response.json()) as Record<string, unknown>);
    if (!detail) {
      return { ok: false, message: "پاسخ SEO نامعتبر است" };
    }
    return { ok: true, detail };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** پیوست مرجع رسانه با Guid دارایی (مسیر پیشرفته؛ بارگذاری باینری DEFERRED). */
export async function attachAdminProductMedia(
  productId: string,
  mediaAssetId: string,
  altText?: string | null,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media`, {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ mediaAssetId, altText: altText ?? null }),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** افزودن تصویر نمایشی بدون Guid سمت کلاینت. */
export async function attachAdminProductPlaceholderMedia(
  productId: string,
  altText?: string | null,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/placeholder`, {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ altText: altText ?? null }),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** بازنویسی ترتیب گالری؛ فهرست باید همهٔ شناسه‌های فعلی را پوشش دهد. */
export async function reorderAdminProductMedia(
  productId: string,
  orderedMediaAssetIds: string[],
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/order`, {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ orderedMediaAssetIds }),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** تنظیم تصویر اصلی. */
export async function setAdminProductMediaPrimary(
  productId: string,
  mediaAssetId: string,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/${mediaAssetId}/primary`, {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** ویرایش متن جایگزین رسانه. */
export async function patchAdminProductMediaAlt(
  productId: string,
  mediaAssetId: string,
  altText: string | null,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/${mediaAssetId}`, {
      method: "PATCH",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ altText }),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** جدا کردن مرجع رسانه از محصول. */
export async function removeAdminProductMedia(
  productId: string,
  mediaAssetId: string,
): Promise<{ ok: true; media: ProductWorkspaceView["media"] } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/media/${mediaAssetId}`, {
      method: "DELETE",
      headers: adminHeaders(),
    });
    return await readMediaMutation(response);
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

export interface AdminProductVariantAxisInput {
  definitionId: string;
  rawValue?: string | null;
  enumOptionId?: string | null;
}

/** ایجاد تنوع با محورها؛ بدون قیمت/موجودی. */
export async function createAdminProductVariant(
  productId: string,
  input: { catalogCodeSeam?: string | null; axes: AdminProductVariantAxisInput[] },
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/variants`, {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        catalogCodeSeam: input.catalogCodeSeam ?? null,
        axes: input.axes.map((axis) => ({
          definitionId: axis.definitionId,
          rawValue: axis.rawValue ?? null,
          enumOptionId: axis.enumOptionId ?? null,
        })),
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const view = mapProductWorkspaceView(await response.json());
    return view ? { ok: true, view } : { ok: false, message: "پاسخ تنوع نامعتبر است" };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}

/** ویرایش وضعیت یا کد Catalog تنوع بدون تغییر اثرانگشت. */
export async function patchAdminProductVariant(
  productId: string,
  variantId: string,
  input: { status?: string | null; catalogCodeSeam?: string | null },
): Promise<{ ok: true; view: ProductWorkspaceView } | { ok: false; message: string }> {
  try {
    const response = await fetch(`/v1/admin/products/${productId}/variants/${variantId}`, {
      method: "PATCH",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        status: input.status ?? null,
        catalogCodeSeam: input.catalogCodeSeam ?? null,
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, message: "دسترسی مجاز نیست" };
    }
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { errorCode?: string; title?: string } | null;
      return { ok: false, message: body?.title ?? body?.errorCode ?? `خطای Host (${response.status})` };
    }
    const view = mapProductWorkspaceView(await response.json());
    return view ? { ok: true, view } : { ok: false, message: "پاسخ تنوع نامعتبر است" };
  } catch {
    return { ok: false, message: "اتصال به Host برقرار نیست" };
  }
}
