/**
 * کلاینت Admin برای فهرست و workspace نویسندهٔ مقاله (Content-owned).
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "./admin-error-map.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";

export interface ContentAuthorGridRow {
  authorId: string;
  displayName: string;
  slug: string;
  isActive: boolean;
  profileImageMediaAssetId: string | null;
  articleCount: number;
  updatedAt: string;
  id: string;
}

export interface ContentAuthorWorkspaceDto {
  authorId: string;
  displayName: string;
  slug: string;
  isActive: boolean;
  profileImageMediaAssetId: string | null;
  coverImageMediaAssetId: string | null;
  shortBio: string | null;
  fullBio: string | null;
  websiteUrl: string | null;
  instagramUrl: string | null;
  twitterUrl: string | null;
  linkedInUrl: string | null;
  articleCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ContentAuthorPickerItem {
  authorId: string;
  displayName: string;
  slug: string;
  profileImageMediaAssetId: string | null;
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

function boolOr(value: unknown, fallback = false): boolean {
  if (value == null) return fallback;
  if (typeof value === "boolean") return value;
  if (value === "true" || value === "1") return true;
  if (value === "false" || value === "0") return false;
  return Boolean(value);
}

function nullableText(value: unknown): string | null {
  if (value == null || value === "") return null;
  return text(value);
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

export function mapContentAuthorGridRow(payload: unknown): ContentAuthorGridRow | null {
  const item = recordOf(payload);
  if (!item) return null;
  const authorId = text(prop(item, "authorId", "AuthorId") ?? prop(item, "id", "Id"));
  if (!authorId) return null;
  return {
    authorId,
    displayName: text(prop(item, "displayName", "DisplayName")),
    slug: text(prop(item, "slug", "Slug")),
    isActive: boolOr(prop(item, "isActive", "IsActive"), true),
    profileImageMediaAssetId: guidOrNull(prop(item, "profileImageMediaAssetId", "ProfileImageMediaAssetId")),
    articleCount: intOr(prop(item, "articleCount", "ArticleCount"), 0),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    id: authorId,
  };
}

export function mapContentAuthorWorkspace(payload: unknown): ContentAuthorWorkspaceDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const authorId = text(prop(item, "authorId", "AuthorId") ?? prop(item, "id", "Id"));
  if (!authorId) return null;
  return {
    authorId,
    displayName: text(prop(item, "displayName", "DisplayName")),
    slug: text(prop(item, "slug", "Slug")),
    isActive: boolOr(prop(item, "isActive", "IsActive"), true),
    profileImageMediaAssetId: guidOrNull(prop(item, "profileImageMediaAssetId", "ProfileImageMediaAssetId")),
    coverImageMediaAssetId: guidOrNull(prop(item, "coverImageMediaAssetId", "CoverImageMediaAssetId")),
    shortBio: nullableText(prop(item, "shortBio", "ShortBio")),
    fullBio: nullableText(prop(item, "fullBio", "FullBio")),
    websiteUrl: nullableText(prop(item, "websiteUrl", "WebsiteUrl")),
    instagramUrl: nullableText(prop(item, "instagramUrl", "InstagramUrl")),
    twitterUrl: nullableText(prop(item, "twitterUrl", "TwitterUrl")),
    linkedInUrl: nullableText(prop(item, "linkedInUrl", "LinkedInUrl")),
    articleCount: intOr(prop(item, "articleCount", "ArticleCount"), 0),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

export function mapContentAuthorPickerItem(payload: unknown): ContentAuthorPickerItem | null {
  const item = recordOf(payload);
  if (!item) return null;
  const authorId = text(prop(item, "authorId", "AuthorId") ?? prop(item, "id", "Id"));
  if (!authorId) return null;
  return {
    authorId,
    displayName: text(prop(item, "displayName", "DisplayName")),
    slug: text(prop(item, "slug", "Slug")),
    profileImageMediaAssetId: guidOrNull(prop(item, "profileImageMediaAssetId", "ProfileImageMediaAssetId")),
  };
}

export function mapContentAuthorMutationError(result: { message?: string; status?: number; data?: unknown }): string {
  const code = typeof result.message === "string" ? result.message : "";
  if (code === "content.author.slug_duplicate") return "این نامک برای یک نویسندهٔ دیگر استفاده شده است.";
  if (code === "content.author.inactive") return "نویسندهٔ غیرفعال برای انتساب جدید مجاز نیست.";
  if (code === "content.author.not_found") return "نویسنده یافت نشد.";
  return mapAdminErrorMessage(code || "admin.error.generic");
}

/** Server GridQuery — نویسندگان Admin. */
export function queryAdminContentAuthorsGrid(
  query: GridServerQuery,
): Promise<AdminGridQueryResult<ContentAuthorGridRow>> {
  return postAdminGridQuery("/v1/admin/content/authors/query", query, adminHeaders(), (item) =>
    mapContentAuthorGridRow(item),
  );
}

export async function fetchActiveContentAuthors(
  search?: string,
): Promise<AdminResult<ContentAuthorPickerItem[]>> {
  // picker?activeOnly=true — canonical Host endpoint (not /authors/active)
  const response = await adminRead(`/v1/admin/content/authors/picker?activeOnly=true${
    search?.trim() ? `&search=${encodeURIComponent(search.trim())}` : ""
  }`);
  if (response.state !== "ok") return { ...response, data: null };
  const rows = Array.isArray(response.data) ? response.data : [];
  return {
    ...response,
    data: rows.map(mapContentAuthorPickerItem).filter((row): row is ContentAuthorPickerItem => row != null),
  };
}

export async function fetchContentAuthorWorkspace(
  authorId: string,
): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminRead(`/v1/admin/content/authors/${authorId}`);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function createContentAuthor(input: {
  displayName: string;
  slug: string;
}): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminWrite("/v1/admin/content/authors", "POST", {
    displayName: input.displayName,
    slug: input.slug,
  });
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentAuthorCore(
  authorId: string,
  input: { displayName: string; slug: string; isActive: boolean },
): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/authors/${authorId}`, "PATCH", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentAuthorAbout(
  authorId: string,
  input: { shortBio?: string | null; fullBio?: string | null },
): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/authors/${authorId}/about`, "PUT", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentAuthorMedia(
  authorId: string,
  input: { profileImageMediaAssetId?: string | null; coverImageMediaAssetId?: string | null },
): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/authors/${authorId}/media`, "PUT", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function updateContentAuthorSocial(
  authorId: string,
  input: {
    websiteUrl?: string | null;
    instagramUrl?: string | null;
    twitterUrl?: string | null;
    linkedInUrl?: string | null;
  },
): Promise<AdminResult<ContentAuthorWorkspaceDto>> {
  const response = await adminWrite(`/v1/admin/content/authors/${authorId}/social`, "PUT", input);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapContentAuthorWorkspace(response.data);
  return data ? { ...response, data } : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

export async function deactivateContentAuthor(authorId: string): Promise<AdminResult<null>> {
  const response = await adminWrite(`/v1/admin/content/authors/${authorId}/deactivate`, "POST");
  if (response.state !== "ok") return { ...response, data: null };
  return { ...response, data: null };
}

export function slugifyContentAuthorName(name: string): string {
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
