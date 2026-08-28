/**
 * کلاینت Admin برای درخت و workspace رده Catalog (قرارداد T004).
 * الگو: adminHeaders / AdminResult / نگاشت camel+Pascal مانند catalog-attribute-api.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";

export type CategoryPublicationStatus = "Draft" | "Published" | "Archived";

export interface CategoryTreeNodeDto {
  id: string;
  parentId: string | null;
  name: string;
  slug: string;
  status: CategoryPublicationStatus;
  sortOrder: number;
  isVisible: boolean;
  hasChildren: boolean;
  productCount: number | null;
}

export interface CategoryTranslationDto {
  categoryId: string;
  locale: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  description: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  metaKeywords: string | null;
  updatedAt: string;
}

export interface CategoryWorkspaceSummary {
  categoryId: string;
  parentCategoryId: string | null;
  status: CategoryPublicationStatus;
  sortOrder: number;
  isVisible: boolean;
  imageMediaAssetId: string | null;
  iconMediaAssetId: string | null;
  createdAt: string;
  updatedAt: string;
  translations: CategoryTranslationDto[];
}

export interface CreateCategoryInput {
  parentCategoryId?: string | null;
  sortOrder?: number;
  isVisible?: boolean;
  name: string;
  slug: string;
  locale: string;
}

export interface MoveCategoryInput {
  newParentId: string | null;
  expectedUpdatedAt?: string | null;
}

export interface ReorderCategoriesInput {
  parentId: string | null;
  orderedCategoryIds: string[];
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function bool(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function num(value: unknown): number | null {
  if (value == null || value === "") return null;
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : null;
}

function intOr(value: unknown, fallback: number): number {
  const n = num(value);
  return n == null ? fallback : Math.trunc(n);
}

function guidOrNull(value: unknown): string | null {
  if (value == null || value === "") return null;
  const s = text(value).trim();
  return s || null;
}

const STATUS_BY_NUMBER: Record<number, CategoryPublicationStatus> = {
  0: "Draft",
  1: "Published",
  2: "Archived",
};

export function parseCategoryStatus(raw: unknown): CategoryPublicationStatus {
  if (typeof raw === "number" && STATUS_BY_NUMBER[raw]) return STATUS_BY_NUMBER[raw];
  const s = text(raw);
  if (s === "Draft" || s === "Published" || s === "Archived") return s;
  const asNum = Number(s);
  if (Number.isFinite(asNum) && STATUS_BY_NUMBER[asNum]) return STATUS_BY_NUMBER[asNum];
  return "Draft";
}

function errorMessage(payload: unknown, status: number): string {
  const item = recordOf(payload);
  if (item) {
    const title = text(prop(item, "title", "Title"));
    const code = text(prop(item, "errorCode", "ErrorCode"));
    if (title) return code ? `${title} (${code})` : title;
    if (code) return code;
  }
  return `admin.http.${status}`;
}

async function adminRead(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function adminWrite(
  path: string,
  method: string,
  body?: unknown,
): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      method,
      headers: adminHeaders(body === undefined ? undefined : { "Content-Type": "application/json" }),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** نگاشت گره درخت از Host. */
export function mapCategoryTreeNode(payload: unknown): CategoryTreeNodeDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const id = text(prop(item, "id", "Id"));
  if (!id) return null;
  const productRaw = prop(item, "productCount", "ProductCount");
  return {
    id,
    parentId: guidOrNull(prop(item, "parentId", "ParentId")),
    name: text(prop(item, "name", "Name")),
    slug: text(prop(item, "slug", "Slug")),
    status: parseCategoryStatus(prop(item, "status", "Status")),
    sortOrder: intOr(prop(item, "sortOrder", "SortOrder"), 0),
    isVisible: bool(prop(item, "isVisible", "IsVisible"), true),
    hasChildren: bool(prop(item, "hasChildren", "HasChildren")),
    productCount: productRaw == null ? null : intOr(productRaw, 0),
  };
}

function mapTreeList(payload: unknown): CategoryTreeNodeDto[] {
  const rows = Array.isArray(payload) ? payload : [];
  return rows.map(mapCategoryTreeNode).filter((row): row is CategoryTreeNodeDto => row != null);
}

function mapTranslation(payload: unknown): CategoryTranslationDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const categoryId = text(prop(item, "categoryId", "CategoryId"));
  const locale = text(prop(item, "locale", "Locale"));
  if (!categoryId || !locale) return null;
  return {
    categoryId,
    locale,
    name: text(prop(item, "name", "Name")),
    slug: text(prop(item, "slug", "Slug")),
    shortDescription: (() => {
      const v = prop(item, "shortDescription", "ShortDescription");
      return v == null || v === "" ? null : text(v);
    })(),
    description: (() => {
      const v = prop(item, "description", "Description");
      return v == null || v === "" ? null : text(v);
    })(),
    seoTitle: (() => {
      const v = prop(item, "seoTitle", "SeoTitle");
      return v == null || v === "" ? null : text(v);
    })(),
    seoDescription: (() => {
      const v = prop(item, "seoDescription", "SeoDescription");
      return v == null || v === "" ? null : text(v);
    })(),
    metaKeywords: (() => {
      const v = prop(item, "metaKeywords", "MetaKeywords");
      return v == null || v === "" ? null : text(v);
    })(),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

/** نگاشت خلاصه workspace. */
export function mapCategoryWorkspace(payload: unknown): CategoryWorkspaceSummary | null {
  const item = recordOf(payload);
  if (!item) return null;
  const categoryId = text(prop(item, "categoryId", "CategoryId"));
  if (!categoryId) return null;
  const translationsRaw = prop(item, "translations", "Translations");
  const translations = Array.isArray(translationsRaw)
    ? translationsRaw.map(mapTranslation).filter((t): t is CategoryTranslationDto => t != null)
    : [];
  return {
    categoryId,
    parentCategoryId: guidOrNull(prop(item, "parentCategoryId", "ParentCategoryId")),
    status: parseCategoryStatus(prop(item, "status", "Status")),
    sortOrder: intOr(prop(item, "sortOrder", "SortOrder"), 0),
    isVisible: bool(prop(item, "isVisible", "IsVisible"), true),
    imageMediaAssetId: guidOrNull(prop(item, "imageMediaAssetId", "ImageMediaAssetId")),
    iconMediaAssetId: guidOrNull(prop(item, "iconMediaAssetId", "IconMediaAssetId")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    translations,
  };
}

/** درخت کامل رده‌ها برای یک locale. */
export async function fetchCategoryTree(
  locale: string,
  search?: string | null,
): Promise<AdminResult<CategoryTreeNodeDto[]>> {
  const params = new URLSearchParams({ locale });
  if (search && search.trim()) params.set("search", search.trim());
  const response = await adminRead(`/v1/admin/catalog/categories/tree?${params.toString()}`);
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: mapTreeList(response.data) };
}

/** خلاصه workspace یک رده. */
export async function fetchCategoryWorkspace(
  categoryId: string,
  locale?: string | null,
): Promise<AdminResult<CategoryWorkspaceSummary>> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  const response = await adminRead(`/v1/admin/catalog/categories/${categoryId}${params}`);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapCategoryWorkspace(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** ایجاد رده با ترجمهٔ فعال. */
export async function createCategory(
  input: CreateCategoryInput,
): Promise<AdminResult<{ categoryId: string; parentCategoryId: string | null; status: CategoryPublicationStatus }>> {
  const body = {
    parentCategoryId: input.parentCategoryId ?? null,
    sortOrder: input.sortOrder ?? 0,
    isVisible: input.isVisible ?? true,
    imageMediaAssetId: null,
    iconMediaAssetId: null,
    translations: [
      {
        locale: input.locale,
        name: input.name,
        slug: input.slug,
      },
    ],
    localizedNames: null,
  };
  const response = await adminWrite("/v1/admin/catalog/categories", "POST", body);
  if (response.state !== "ok") return { ...response, data: null };
  const item = recordOf(response.data);
  const categoryId = item ? text(prop(item, "categoryId", "CategoryId")) : "";
  if (!categoryId) {
    return { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  }
  return {
    ...response,
    data: {
      categoryId,
      parentCategoryId: item ? guidOrNull(prop(item, "parentCategoryId", "ParentCategoryId")) : null,
      status: item ? parseCategoryStatus(prop(item, "status", "Status")) : "Draft",
    },
  };
}

/** جابه‌جایی والد رده. */
export async function moveCategory(
  categoryId: string,
  input: MoveCategoryInput,
): Promise<AdminResult<CategoryWorkspaceSummary>> {
  const response = await adminWrite(`/v1/admin/catalog/categories/${categoryId}/move`, "POST", {
    newParentId: input.newParentId,
    expectedUpdatedAt: input.expectedUpdatedAt ?? null,
  });
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapCategoryWorkspace(response.data);
  return data
    ? { ...response, data }
    : { ...response, data: null };
}

/** ترتیب خواهر/برادرها. */
export async function reorderCategories(
  input: ReorderCategoriesInput,
): Promise<AdminResult<{ ok: true }>> {
  const response = await adminWrite("/v1/admin/catalog/categories/reorder", "POST", {
    parentId: input.parentId,
    orderedCategoryIds: input.orderedCategoryIds,
  });
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: { ok: true } };
}

/** انتشار رده (اختیاری برای نمایش وضعیت). */
export async function publishCategory(
  categoryId: string,
): Promise<AdminResult<CategoryWorkspaceSummary>> {
  const response = await adminWrite(`/v1/admin/catalog/categories/${categoryId}/publish`, "POST");
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapCategoryWorkspace(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** ساخت slug اولیه از نام (هم‌راستا با نرمال‌ساز دامنه). */
export function slugifyCategoryName(name: string): string {
  const trimmed = name.trim().toLowerCase();
  let pendingHyphen = false;
  let result = "";
  for (const ch of trimmed) {
    if (/\s|_|\//.test(ch) || ch === "\\") {
      pendingHyphen = result.length > 0;
      continue;
    }
    if (ch === "-") {
      pendingHyphen = result.length > 0;
      continue;
    }
    if (/[a-z0-9]/i.test(ch) || ch.charCodeAt(0) > 127) {
      if (pendingHyphen) {
        result += "-";
        pendingHyphen = false;
      }
      result += ch;
    }
  }
  return result.replace(/^-+|-+$/g, "");
}
