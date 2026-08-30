/**
 * کلاینت Admin برای برچسب تاکسونومی Catalog.
 * Tags ≠ meta keywords؛ بدون comma-separated storage.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";

export interface CatalogTag {
  tagId: string;
  code: string;
  slugSeam: string | null;
  status: string;
  name: string;
  createdAt: string;
  updatedAt: string;
}

/** @deprecated استفاده از CatalogTag؛ نام سازگار با کارت قبلی. */
export type CatalogTagDto = CatalogTag;

export interface CreateCatalogTagInput {
  nameFa: string;
  nameEn?: string | null;
  code?: string | null;
  slug?: string | null;
  locale?: string;
}

export const TAG_HELPER_FA =
  "برچسب‌ها برای گروه‌بندی، جستجو و نمایش هدفمند استفاده می‌شوند. برچسب زیاد و تکراری ایجاد نکنید.";

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

function errorMessage(payload: unknown, status: number): string {
  const item = recordOf(payload);
  if (item) {
    const title = text(prop(item, "title", "Title"));
    if (title) return title;
    const message = text(prop(item, "message", "Message"));
    if (message) return message;
  }
  return `http-${status}`;
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
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorMessage(payload, response.status),
      };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** نگاشت یک برچسب از Host (camel/Pascal). */
export function mapCatalogTag(payload: unknown): CatalogTag | null {
  const item = recordOf(payload);
  if (!item) return null;
  const tagId = text(prop(item, "tagId", "TagId")).trim();
  const name = text(prop(item, "name", "Name")).trim();
  const code = text(prop(item, "code", "Code")).trim();
  if (!tagId || (!name && !code)) return null;
  const slugRaw = prop(item, "slugSeam", "SlugSeam");
  return {
    tagId,
    code: code || tagId,
    slugSeam: slugRaw == null || slugRaw === "" ? null : text(slugRaw),
    status: text(prop(item, "status", "Status"), "Draft"),
    name: name || code,
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

function mapTagList(payload: unknown): CatalogTag[] {
  const rows = Array.isArray(payload) ? payload : [];
  return rows.map(mapCatalogTag).filter((row): row is CatalogTag => row != null);
}

/** فهرست/جستجوی برچسب‌ها بر اساس نام محلی. */
export async function listCatalogTags(
  locale = "fa-IR",
  search?: string | null,
): Promise<AdminResult<CatalogTag[]>> {
  const q = new URLSearchParams({ locale });
  if (search?.trim()) q.set("search", search.trim());
  const response = await adminRead(`/v1/admin/catalog/tags?${q}`);
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

/** ایجاد برچسب با نام فارسی الزامی. */
export async function createCatalogTag(
  input: CreateCatalogTagInput,
): Promise<AdminResult<CatalogTag>> {
  const nameFa = input.nameFa.trim();
  if (!nameFa) {
    return { state: "error", data: null, status: 400, message: "نام فارسی برچسب الزامی است." };
  }
  const body: Record<string, unknown> = {
    nameFa,
    locale: input.locale ?? "fa-IR",
    localizedNames: {
      "fa-IR": nameFa,
      ...(input.nameEn?.trim() ? { en: input.nameEn.trim() } : {}),
    },
  };
  if (input.nameEn?.trim()) body.nameEn = input.nameEn.trim();
  if (input.code?.trim()) body.code = input.code.trim();
  if (input.slug?.trim()) body.slug = input.slug.trim();

  const response = await adminWrite("/v1/admin/catalog/tags", "POST", body);
  if (response.state !== "ok") {
    return {
      state: response.state,
      data: null,
      status: response.status,
      message: response.message,
    };
  }
  const mapped = mapCatalogTag(response.data);
  if (!mapped) {
    return { state: "error", data: null, status: response.status, message: "catalog.tag.map.failed" };
  }
  return { state: "ok", data: mapped, status: response.status };
}

export async function listProductTags(
  productId: string,
  locale = "fa-IR",
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminRead(
    `/v1/admin/catalog/products/${productId}/tags?locale=${encodeURIComponent(locale)}`,
  );
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

export async function assignProductTag(
  productId: string,
  tagId: string,
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminWrite(
    `/v1/admin/catalog/products/${productId}/tags/${tagId}`,
    "POST",
  );
  if (response.state !== "ok") {
    return {
      state: response.state,
      data: null,
      status: response.status,
      message: response.message,
    };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

export async function removeProductTag(
  productId: string,
  tagId: string,
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminWrite(
    `/v1/admin/catalog/products/${productId}/tags/${tagId}`,
    "DELETE",
  );
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

export async function listCategoryTags(
  categoryId: string,
  locale = "fa-IR",
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminRead(
    `/v1/admin/catalog/categories/${categoryId}/tags?locale=${encodeURIComponent(locale)}`,
  );
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

export async function assignCategoryTag(
  categoryId: string,
  tagId: string,
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/tags/${tagId}`,
    "POST",
  );
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

export async function removeCategoryTag(
  categoryId: string,
  tagId: string,
): Promise<AdminResult<CatalogTag[]>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/tags/${tagId}`,
    "DELETE",
  );
  if (response.state !== "ok") {
    return { state: response.state, data: null, status: response.status, message: response.message };
  }
  return { state: "ok", data: mapTagList(response.data), status: response.status };
}

/** برچسب‌های موجود که هنوز اختصاص نیافته‌اند (برای multi-select). */
export function filterUnassignedTags(
  all: CatalogTag[],
  assigned: CatalogTag[],
): CatalogTag[] {
  const taken = new Set(assigned.map((t) => t.tagId));
  return all.filter((t) => !taken.has(t.tagId));
}
