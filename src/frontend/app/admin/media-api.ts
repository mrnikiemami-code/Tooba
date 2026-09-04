/**
 * کلاینت Admin برای Media DAM — آپلود multipart، فهرست صفحه‌بندی‌شده، فراداده.
 * Catalog فقط MediaAssetId را ارجاع می‌دهد؛ باینری اینجا مالکیت دارد.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "./admin-error-map.ts";

export interface MediaAssetDto {
  mediaAssetId: string;
  originalFileName: string;
  contentType: string;
  byteSize: number;
  width: number | null;
  height: number | null;
  createdAt: string;
  displayUrl: string | null;
}

export interface MediaLibraryPage {
  items: MediaAssetDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MediaUploadItemOk {
  ok: true;
  asset: MediaAssetDto;
}

export interface MediaUploadItemFail {
  ok: false;
  fileName: string;
  title: string;
  errorCode: string;
}

export type MediaUploadItem = MediaUploadItemOk | MediaUploadItemFail;

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

function num(value: unknown): number | null {
  if (value == null || value === "") return null;
  const n = typeof value === "number" ? value : Number(value);
  return Number.isFinite(n) ? n : null;
}

function guidOrNull(value: unknown): string | null {
  if (value == null || value === "") return null;
  const s = String(value).trim();
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(s) ? s : null;
}

function errorMessage(payload: unknown, status: number): string {
  const code = parseAdminProblemErrorCode(payload, status);
  return mapAdminErrorMessage(code, "fa");
}

/** نگاشت فرادادهٔ دارایی از Host (camel یا Pascal). */
export function mapMediaAsset(payload: unknown): MediaAssetDto | null {
  const item = recordOf(payload);
  if (!item) return null;
  const mediaAssetId = guidOrNull(prop(item, "mediaAssetId", "MediaAssetId"));
  if (!mediaAssetId) return null;
  return {
    mediaAssetId,
    originalFileName: text(prop(item, "originalFileName", "OriginalFileName")),
    contentType: text(prop(item, "contentType", "ContentType")),
    byteSize: num(prop(item, "byteSize", "ByteSize")) ?? 0,
    width: num(prop(item, "width", "Width")),
    height: num(prop(item, "height", "Height")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    displayUrl: text(prop(item, "displayUrl", "DisplayUrl")) || null,
  };
}

/** URL پایدار پیش‌نمایش — از طریق پروکسی Admin به Host. */
export function mediaPreviewUrl(mediaAssetId: string | null | undefined): string | null {
  const id = mediaAssetId?.trim();
  if (!id) return null;
  return `/v1/storefront/media/${id}`;
}

function mapPage(payload: unknown): MediaLibraryPage | null {
  const root = recordOf(payload);
  if (!root) return null;
  const rawItems = prop(root, "items", "Items");
  const items = Array.isArray(rawItems)
    ? rawItems.map(mapMediaAsset).filter((x): x is MediaAssetDto => x != null)
    : [];
  return {
    items,
    page: num(prop(root, "page", "Page")) ?? 1,
    pageSize: num(prop(root, "pageSize", "PageSize")) ?? 24,
    totalCount: num(prop(root, "totalCount", "TotalCount")) ?? items.length,
  };
}

/** فهرست صفحه‌بندی‌شدهٔ کتابخانه (جستجو و فیلتر ContentType اختیاری). */
export async function queryAdminMediaLibrary(input?: {
  search?: string | null;
  page?: number;
  pageSize?: number;
  /** e.g. `image/`, `video/`, `application/pdf` — server-filtered totalCount. */
  contentTypePrefix?: string | null;
}): Promise<AdminResult<MediaLibraryPage>> {
  const params = new URLSearchParams();
  const search = input?.search?.trim();
  if (search) params.set("search", search);
  const prefix = input?.contentTypePrefix?.trim();
  if (prefix) params.set("contentTypePrefix", prefix);
  params.set("page", String(input?.page && input.page > 0 ? input.page : 1));
  params.set("pageSize", String(input?.pageSize && input.pageSize > 0 ? input.pageSize : 24));
  try {
    const response = await fetch(`/v1/admin/media?${params.toString()}`, {
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    const data = mapPage(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** فرادادهٔ یک دارایی. */
export async function getAdminMediaAsset(mediaAssetId: string): Promise<AdminResult<MediaAssetDto>> {
  try {
    const response = await fetch(`/v1/admin/media/${encodeURIComponent(mediaAssetId)}`, {
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    const data = mapMediaAsset(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export type MediaUploadRowState = "queued" | "uploading" | "succeeded" | "failed";

export interface MediaUploadRow {
  id: string;
  fileName: string;
  state: MediaUploadRowState;
  /** 0–100 وقتی XHR progress در دسترس است؛ در غیر این صورت null (نامعین). */
  progressPercent: number | null;
  errorCode?: string;
  messageFa?: string;
  messageEn?: string;
  asset?: MediaAssetDto;
  file: File;
}

/** برچسب وضعیت آپلود fa/en برای UX سطح اپلیکیشن. */
export function mediaUploadStateLabel(
  state: MediaUploadRowState,
  locale: "fa" | "en" = "fa",
): string {
  const map: Record<MediaUploadRowState, { fa: string; en: string }> = {
    queued: { fa: "در صف", en: "Queued" },
    uploading: { fa: "در حال بارگذاری", en: "Uploading" },
    succeeded: { fa: "موفق", en: "Succeeded" },
    failed: { fa: "ناموفق", en: "Failed" },
  };
  return map[state][locale];
}

function parseUploadPayload(payload: unknown): MediaUploadItem[] {
  const root = recordOf(payload);
  const rawItems = root ? prop(root, "items", "Items") : null;
  const items: MediaUploadItem[] = [];
  if (!Array.isArray(rawItems)) return items;
  for (const row of rawItems) {
    const rec = recordOf(row);
    if (!rec) continue;
    const ok = Boolean(prop(rec, "ok", "Ok"));
    if (ok) {
      const asset = mapMediaAsset(prop(rec, "asset", "Asset"));
      if (asset) items.push({ ok: true, asset });
    } else {
      const errorCode = text(prop(rec, "errorCode", "ErrorCode")) || "media.upload.failed";
      items.push({
        ok: false,
        fileName: text(prop(rec, "fileName", "FileName")),
        title: text(prop(rec, "title", "Title")),
        errorCode,
      });
    }
  }
  return items;
}

/**
 * آپلود یک فایل با XHR تا progress بایت‌محور در دسترس باشد.
 * اگر lengthComputable نباشد progressPercent=null (نامعین) می‌ماند.
 */
export function uploadAdminMediaFileWithProgress(
  file: File,
  onProgress?: (progressPercent: number | null) => void,
): Promise<AdminResult<{ items: MediaUploadItem[] }>> {
  return new Promise((resolve) => {
    const form = new FormData();
    form.append("files", file, file.name);
    const xhr = new XMLHttpRequest();
    xhr.open("POST", "/v1/admin/media/upload");
    const headers = adminHeaders();
    for (const [key, value] of Object.entries(headers)) {
      if (key.toLowerCase() === "content-type") continue;
      xhr.setRequestHeader(key, value);
    }
    xhr.upload.onprogress = (event) => {
      if (!onProgress) return;
      if (event.lengthComputable && event.total > 0) {
        onProgress(Math.min(100, Math.round((event.loaded / event.total) * 100)));
      } else {
        onProgress(null);
      }
    };
    xhr.onload = () => {
      let payload: unknown = null;
      try {
        payload = xhr.responseText ? JSON.parse(xhr.responseText) : null;
      } catch {
        payload = null;
      }
      if (xhr.status === 401 || xhr.status === 403) {
        resolve({ state: "denied", data: null, status: xhr.status, message: "admin.authorization.denied" });
        return;
      }
      if (xhr.status < 200 || xhr.status >= 300) {
        resolve({
          state: "error",
          data: null,
          status: xhr.status,
          message: errorMessage(payload, xhr.status),
        });
        return;
      }
      resolve({ state: "ok", data: { items: parseUploadPayload(payload) }, status: xhr.status });
    };
    xhr.onerror = () => {
      resolve({ state: "error", data: null, status: 0, message: "host-unreachable" });
    };
    xhr.onabort = () => {
      resolve({ state: "error", data: null, status: 0, message: "media.upload.failed" });
    };
    onProgress?.(null);
    xhr.send(form);
  });
}

/**
 * آپلود multipart واقعی — فیلد فرم `files`.
 * Content-Type را عمداً ست نمی‌کنیم تا boundary مرورگر حفظ شود.
 */
export async function uploadAdminMediaFiles(
  files: File[],
): Promise<AdminResult<{ items: MediaUploadItem[] }>> {
  if (!files.length) {
    return {
      state: "error",
      data: null,
      status: 400,
      message: mapAdminErrorMessage("media.upload.failed", "fa"),
    };
  }
  const form = new FormData();
  for (const file of files) {
    form.append("files", file, file.name);
  }
  try {
    const response = await fetch("/v1/admin/media/upload", {
      method: "POST",
      headers: adminHeaders(),
      body: form,
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: { items: parseUploadPayload(payload) }, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** پیام فارسی برای یک کد خطای آپلود تکی. */
export function mediaUploadItemMessage(item: MediaUploadItemFail, locale: "fa" | "en" = "fa"): string {
  return mapAdminErrorMessage(item.errorCode, locale);
}
