/**
 * کلاینت Admin برای درخت و workspace دسته‌بندی مقاله (Content-owned).
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "./admin-error-map.ts";

export type ContentCategoryStatus = "Active" | "Archived";

export interface ContentCategoryTreeNodeDto {
  id: string;
  languageCode: string;
  parentId: string | null;
  name: string;
  slug: string;
  status: ContentCategoryStatus;
  sortOrder: number;
  hasChildren: boolean;
  articleCount: number;
}

export interface ContentCategoryWorkspaceDto {
  id: string;
  languageCode: string;
  parentId: string | null;
  name: string;
  slug: string;
  shortDescription: string | null;
  description: string | null;
  status: ContentCategoryStatus;
  sortOrder: number;
  seoTitle: string | null;
  seoDescription: string | null;
  imageMediaAssetId: string | null;
  articleCount: number;
  createdAt: string;
  updatedAt: string;
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

function intOr(value: unknown, fallback: number): number {
  if (value == null || value === "") return fallback;
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function guidOrNull(value: unknown): string | null {
  if (value == null || value === "") return null;
  const s = text(value).trim();
  return s || null;
}

function parseStatus(raw: unknown): ContentCategoryStatus {
  const s = text(raw);
  return s === "Archived" ? "Archived" : "Active";
}

function errorMessage(payload: unknown, status: number): string {
  return parseAdminProblemErrorCode(payload, status);
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

async function adminWrite(path: string, method: string, body?: unknown): Promise<AdminResult<unknown>> {
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

export function mapContentCategoryTreeNode(payload: unknown): ContentCategoryTreeNodeDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const id = text(prop(item, "id", "Id"));
  if (!id) return null;
  return {
    id,
    languageCode: text(prop(item, "languageCode", "LanguageCode")),
    parentId: guidOrNull(prop(item, "parentId", "ParentId")),
    name: text(prop(item, "name", "Name")),
    slug: text(prop(item, "slug", "Slug")),
    status: parseStatus(prop(item, "status", "Status")),
    sortOrder: intOr(prop(item, "sortOrder", "SortOrder"), 0),
    hasChildren: Boolean(prop(item, "hasChildren", "HasChildren")),
    articleCount: intOr(prop(item, "articleCount", "ArticleCount"), 0),
  };
}

export function mapContentCategoryWorkspace(payload: unknown): ContentCategoryWorkspaceDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const id = text(prop(item, "id", "Id"));
  if (!id) return null;
  return {
    id,
    languageCode: text(prop(item, "languageCode", "LanguageCode")),
    parentId: guidOrNull(prop(item, "parentId", "ParentId")),
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
    status: parseStatus(prop(item, "status", "Status")),
    sortOrder: intOr(prop(item, "sortOrder", "SortOrder"), 0),
    seoTitle: (() => {
      const v = prop(item, "seoTitle", "SeoTitle");
      return v == null || v === "" ? null : text(v);
    })(),
    seoDescription: (() => {
      const v = prop(item, "seoDescription", "SeoDescription");
      return v == null || v === "" ? null : text(v);
    })(),
    imageMediaAssetId: guidOrNull(prop(item, "imageMediaAssetId", "ImageMediaAssetId")),
    articleCount: intOr(prop(item, "articleCount", "ArticleCount"), 0),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

export function mapContentCategoryMutationError(result: { message?: string; status?: number; data?: unknown }): string {
  const code = typeof result.message === "string" ? result.message : "";
  if (code === "content.category.slug_duplicate") return "این نامک برای یک دستهٔ دیگر در همین زبان استفاده شده است.";
  if (code === "content.category.cross_language_parent") return "والد باید همان زبان دسته باشد.";
  if (code === "content.category.language_mismatch") return "زبان مقاله با زبان دسته هم‌خوان نیست.";
  return mapAdminErrorMessage(code || "admin.error.generic");
}

export async function fetchContentCategoryTree(
  languageCode: string,
  search?: string | null,
): Promise<AdminResult<ContentCategoryTreeNodeDto[]>> {
  const params = new URLSearchParams({ languageCode });
  if (search?.trim()) params.set("search", search.trim());
  const response = await adminRead(`/v1/admin/content/categories/tree?${params.toString()}`);
  if (response.state !== "ok") return { ...response, data: null };
  const rows = Array.isArray(response.data) ? response.data : [];
  return {
    ...response,
    data: rows.map(mapContentCategoryTreeNode).filter((row): row is ContentCategoryTreeNodeDto => row != null),
  };
}

export async function fetchContentCategoryWorkspace(
  categoryId: string,
): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminRead(`/v1/admin/content/categories/${categoryId}`);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function createContentCategory(input: {
  languageCode: string;
  parentCategoryId?: string | null;
  name: string;
  slug: string;
  shortDescription?: string | null;
  description?: string | null;
  sortOrder?: number;
}): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminWrite("/v1/admin/content/categories", "POST", {
    languageCode: input.languageCode,
    parentCategoryId: input.parentCategoryId ?? null,
    name: input.name,
    slug: input.slug,
    shortDescription: input.shortDescription ?? null,
    description: input.description ?? null,
    sortOrder: input.sortOrder ?? 0,
  });
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentCategoryCore(
  categoryId: string,
  input: {
    name: string;
    slug: string;
    shortDescription?: string | null;
    description?: string | null;
    sortOrder: number;
    status: ContentCategoryStatus;
  },
): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/categories/${categoryId}`, "PATCH", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentCategorySeo(
  categoryId: string,
  input: { seoTitle?: string | null; seoDescription?: string | null },
): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/categories/${categoryId}/seo`, "PUT", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentCategoryMedia(
  categoryId: string,
  imageMediaAssetId: string | null,
): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/categories/${categoryId}/media`, "PUT", { imageMediaAssetId });
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function moveContentCategory(
  categoryId: string,
  newParentId: string | null,
): Promise<AdminResult<ContentCategoryWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/categories/${categoryId}/move`, "POST", { newParentId });
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentCategoryWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function archiveContentCategory(categoryId: string): Promise<AdminResult<null>> {
  const response = await adminWrite(`/v1/admin/content/categories/${categoryId}/archive`, "POST");
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: null };
}

export function slugifyContentCategoryName(name: string): string {
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
  return result;
}
