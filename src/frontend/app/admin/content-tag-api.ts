/**
 * کلاینت Admin برای برچسب‌های محتوا و انتساب به مقاله.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { parseAdminProblemErrorCode } from "./admin-error-map.ts";

export type ContentTagDto = {
  tagId: string;
  languageCode: string;
  name: string;
  normalizedName: string;
  slug: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

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

function mapTag(raw: unknown): ContentTagDto | null {
  const row = recordOf(raw);
  if (!row) return null;
  const tagId = text(prop(row, "tagId", "TagId"));
  const languageCode = text(prop(row, "languageCode", "LanguageCode"));
  const name = text(prop(row, "name", "Name"));
  if (!tagId || !languageCode || !name) return null;
  return {
    tagId,
    languageCode,
    name,
    normalizedName: text(prop(row, "normalizedName", "NormalizedName"), name),
    slug: text(prop(row, "slug", "Slug")) || null,
    isActive: Boolean(prop(row, "isActive", "IsActive") ?? true),
    createdAt: text(prop(row, "createdAt", "CreatedAt")),
    updatedAt: text(prop(row, "updatedAt", "UpdatedAt")),
  };
}

function mapTags(raw: unknown): ContentTagDto[] {
  if (!Array.isArray(raw)) return [];
  return raw.map(mapTag).filter((row): row is ContentTagDto => row != null);
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

async function adminWrite(path: string, init?: RequestInit): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      ...init,
      headers: {
        ...adminHeaders(),
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...(init?.headers ?? {}),
      },
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

export async function searchContentTags(
  languageCode: string,
  search?: string,
  activeOnly = true,
  limit = 30,
): Promise<AdminResult<ContentTagDto[]>> {
  const params = new URLSearchParams({
    languageCode,
    activeOnly: String(activeOnly),
    limit: String(limit),
  });
  if (search?.trim()) params.set("search", search.trim());
  const result = await adminRead(`/v1/admin/content/tags?${params.toString()}`);
  if (result.state !== "ok") return { ...result, data: null };
  return { ...result, data: mapTags(result.data) };
}

export async function createContentTag(input: {
  languageCode: string;
  name: string;
  slug?: string | null;
}): Promise<AdminResult<ContentTagDto>> {
  const result = await adminWrite("/v1/admin/content/tags", {
    method: "POST",
    body: JSON.stringify({
      languageCode: input.languageCode,
      name: input.name,
      slug: input.slug ?? null,
    }),
  });
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapTag(result.data);
  return mapped
    ? { ...result, data: mapped }
    : { state: "error", data: null, status: result.status, message: "content.tag.invalid_name" };
}

export async function listContentArticleTags(articleId: string): Promise<AdminResult<ContentTagDto[]>> {
  const result = await adminRead(`/v1/admin/content/articles/${articleId}/tags`);
  if (result.state !== "ok") return { ...result, data: null };
  return { ...result, data: mapTags(result.data) };
}

export async function assignContentArticleTag(
  articleId: string,
  tagId: string,
): Promise<AdminResult<ContentTagDto[]>> {
  const result = await adminWrite(`/v1/admin/content/articles/${articleId}/tags/${tagId}`, {
    method: "POST",
  });
  if (result.state !== "ok") return { ...result, data: null };
  return { ...result, data: mapTags(result.data) };
}

export async function removeContentArticleTag(
  articleId: string,
  tagId: string,
): Promise<AdminResult<ContentTagDto[]>> {
  const result = await adminWrite(`/v1/admin/content/articles/${articleId}/tags/${tagId}`, {
    method: "DELETE",
  });
  if (result.state !== "ok") return { ...result, data: null };
  return { ...result, data: mapTags(result.data) };
}
