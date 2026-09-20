/**
 * Admin client for review moderation queue.
 */
import type { GridServerQuery } from "../../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../../design-system/app-data-grid/admin-grid-query-client.ts";
import {
  ADMIN_ACTOR_STORAGE_KEY,
  ADMIN_DEV_ACTOR_HEADER,
  DEFAULT_ADMIN_ACTOR_ID,
  type AdminResult,
} from "../../../lib/admin/admin-result.ts";

function actorId(): string {
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

/** ردیف تعدیل نظر Admin؛ شناسه فقط برای فرمان publish/reject استفاده می‌شود. */
export interface AdminReviewRow {
  id: string;
  reviewerDisplayName: string;
  productTitle: string;
  rating: number;
  excerpt: string;
  verifiedPurchase: boolean;
  status: string;
  createdAt: string;
}

/** صفحهٔ نظرهای Admin با شمارش مقتدر Host. */
export interface AdminReviewsPage {
  rows: AdminReviewRow[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** پاسخ صفحه‌بندی‌شدهٔ تعدیل نظر را با casingهای Host نگاشت می‌کند. */
export function mapAdminReviews(value: unknown): AdminReviewsPage | null {
  const root = record(value);
  if (!root) return null;
  const rawRows = prop(root, "reviews", "Reviews") ?? prop(root, "items", "Items");
  if (!Array.isArray(rawRows)) return null;
  const rows = rawRows.flatMap((raw): AdminReviewRow[] => {
    const item = record(raw);
    if (!item) return [];
    const id = text(prop(item, "reviewId", "ReviewId") ?? prop(item, "publicId", "PublicId"));
    if (!id) return [];
    const body = text(prop(item, "body", "Body") ?? prop(item, "excerpt", "Excerpt"));
    return [{
      id,
      reviewerDisplayName: text(prop(item, "authorDisplayName", "AuthorDisplayName") ?? prop(item, "reviewerDisplayName", "ReviewerDisplayName"), "مشتری"),
      productTitle: text(prop(item, "productTitle", "ProductTitle"), "کالا"),
      rating: number(prop(item, "rating", "Rating")),
      excerpt: body.length > 120 ? `${body.slice(0, 120)}…` : body,
      verifiedPurchase: prop(item, "verifiedPurchase", "VerifiedPurchase") === true,
      status: text(prop(item, "status", "Status"), "Pending"),
      createdAt: text(prop(item, "createdAt", "CreatedAt")),
    }];
  });
  return {
    rows,
    page: Math.max(1, number(prop(root, "page", "Page")) || 1),
    pageSize: Math.max(1, number(prop(root, "pageSize", "PageSize")) || 20),
    totalCount: Math.max(0, number(prop(root, "totalCount", "TotalCount"))),
  };
}

function mapAdminReviewGridItem(value: unknown): AdminReviewRow | null {
  const item = record(value);
  if (!item) return null;
  const id = text(prop(item, "reviewId", "ReviewId"));
  if (!id) return null;
  const body = text(prop(item, "body", "Body"));
  return {
    id,
    reviewerDisplayName: text(prop(item, "authorDisplayName", "AuthorDisplayName"), "مشتری"),
    productTitle: text(prop(item, "productTitle", "ProductTitle"), "کالا"),
    rating: number(prop(item, "rating", "Rating")),
    excerpt: body.length > 120 ? `${body.slice(0, 120)}…` : body,
    verifiedPurchase: prop(item, "verifiedPurchase", "VerifiedPurchase") === true,
    status: text(prop(item, "status", "Status"), "Pending"),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
  };
}

async function read(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function mapped<T>(path: string, mapper: (value: unknown) => T | null): Promise<AdminResult<T>> {
  const response = await read(path);
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapper(response.data);
  return data == null
    ? { state: "error", data: null, status: response.status, message: "admin.invalid-response" }
    : { ...response, data };
}

/** نظرهای در انتظار را از مرز مقتدر Admin می‌خواند. */
export function loadAdminReviews(page = 1, pageSize = 20): Promise<AdminResult<AdminReviewsPage>> {
  return mapped(`/v1/admin/reviews?status=Pending&page=${page}&pageSize=${pageSize}`, mapAdminReviews);
}

/** فرمان تعدیل را به routeهای متمرکز Host می‌فرستد؛ UI هیچ مجوزی صادر نمی‌کند. */
export async function moderateAdminReview(reviewId: string, action: "publish" | "reject"): Promise<AdminResult<null>> {
  try {
    const response = await fetch(`/v1/admin/reviews/${encodeURIComponent(reviewId)}/${action}`, {
      method: "POST",
      headers: adminHeaders({ "content-type": "application/json" }),
    });
    if (response.status === 401 || response.status === 403) return { state: "denied", data: null, status: response.status };
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: null, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — نظرات Admin. */
export function queryAdminReviewsGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminReviewRow>> {
  return postAdminGridQuery("/v1/admin/reviews/query", query, adminHeaders(), (item) => mapAdminReviewGridItem(item));
}
