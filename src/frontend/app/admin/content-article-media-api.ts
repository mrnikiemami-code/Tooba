/**
 * کلاینت Admin برای workspace رسانهٔ مقاله (DAM-only).
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "./admin-error-map.ts";

export interface ArticleGalleryItemDto {
  mediaAssetId: string;
  displayOrder: number;
  altText: string | null;
  caption: string | null;
}

export interface ArticleMediaWorkspaceDto {
  articleId: string;
  featuredMediaAssetId: string | null;
  seoImageMediaAssetId: string | null;
  effectiveSeoImageMediaAssetId: string | null;
  gallery: ArticleGalleryItemDto[];
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function guidOrNull(value: unknown): string | null {
  if (value == null || value === "") return null;
  const s = text(value).trim();
  return s || null;
}

function intOr(value: unknown, fallback = 0): number {
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

function errorMessage(payload: unknown, status: number): string {
  return parseAdminProblemErrorCode(payload, status);
}

function mapGalleryItem(payload: unknown): ArticleGalleryItemDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const mediaAssetId = guidOrNull(prop(item, "mediaAssetId", "MediaAssetId"));
  if (!mediaAssetId) return null;
  const alt = prop(item, "altText", "AltText");
  const cap = prop(item, "caption", "Caption");
  return {
    mediaAssetId,
    displayOrder: intOr(prop(item, "displayOrder", "DisplayOrder")),
    altText: alt == null || alt === "" ? null : text(alt),
    caption: cap == null || cap === "" ? null : text(cap),
  };
}

export function mapArticleMediaWorkspace(payload: unknown): ArticleMediaWorkspaceDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const articleId = guidOrNull(prop(item, "articleId", "ArticleId"));
  if (!articleId) return null;
  const galleryRaw = prop(item, "gallery", "Gallery");
  const gallery = Array.isArray(galleryRaw)
    ? galleryRaw.map(mapGalleryItem).filter((row): row is ArticleGalleryItemDto => row !== null)
    : [];
  return {
    articleId,
    featuredMediaAssetId: guidOrNull(prop(item, "featuredMediaAssetId", "FeaturedMediaAssetId")),
    seoImageMediaAssetId: guidOrNull(prop(item, "seoImageMediaAssetId", "SeoImageMediaAssetId")),
    effectiveSeoImageMediaAssetId: guidOrNull(prop(item, "effectiveSeoImageMediaAssetId", "EffectiveSeoImageMediaAssetId")),
    gallery,
  };
}

async function mediaWrite<T>(
  path: string,
  method: string,
  body?: unknown,
): Promise<AdminResult<T>> {
  try {
    const response = await fetch(path, {
      method,
      headers: adminHeaders(body !== undefined),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload as T, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function fetchArticleMediaWorkspace(articleId: string): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(`/v1/admin/content/articles/${encodeURIComponent(articleId)}/media`, "GET");
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function assignArticleFeaturedImage(
  articleId: string,
  mediaAssetId: string | null,
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/featured`,
    "PUT",
    { mediaAssetId },
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function assignArticleSeoImage(
  articleId: string,
  mediaAssetId: string | null,
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/seo-image`,
    "PUT",
    { mediaAssetId },
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function addArticleGalleryItems(
  articleId: string,
  mediaAssetIds: string[],
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/gallery`,
    "POST",
    { mediaAssetIds },
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function removeArticleGalleryItem(
  articleId: string,
  mediaAssetId: string,
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/gallery/${encodeURIComponent(mediaAssetId)}`,
    "DELETE",
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function reorderArticleGallery(
  articleId: string,
  orderedMediaAssetIds: string[],
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/gallery/reorder`,
    "PUT",
    { orderedMediaAssetIds },
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export async function patchArticleGalleryItem(
  articleId: string,
  mediaAssetId: string,
  input: { altText?: string | null; caption?: string | null },
): Promise<AdminResult<ArticleMediaWorkspaceDto>> {
  const result = await mediaWrite<unknown>(
    `/v1/admin/content/articles/${encodeURIComponent(articleId)}/media/gallery/${encodeURIComponent(mediaAssetId)}`,
    "PATCH",
    input,
  );
  if (result.state !== "ok") return result as AdminResult<ArticleMediaWorkspaceDto>;
  const mapped = mapArticleMediaWorkspace(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

export function mapArticleMediaMutationError(result: { message?: string; status?: number }): string {
  return mapAdminErrorMessage(result.message ?? `admin.http.${result.status ?? 0}`, "fa");
}
