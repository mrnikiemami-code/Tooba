/**
 * Admin client for seller directory list.
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

function array(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

/** ردیف فهرست فروشندگان Admin. */
export interface AdminSellerRow {
  id: string;
  sellerPartyId: string;
  displayName: string;
  status: string;
  relationship: string;
  activeOfferCount: number;
  orderCount: number;
}

/** فهرست فروشندگان را بدون ایجاد دادهٔ CRM نگاشت می‌کند. */
export function mapAdminSellers(value: unknown): AdminSellerRow[] {
  return array(value).flatMap((raw): AdminSellerRow[] => {
    const item = record(raw);
    if (!item) return [];
    const sellerPartyId = text(prop(item, "sellerPartyId", "SellerPartyId"));
    if (!sellerPartyId) return [];
    return [{
      id: sellerPartyId,
      sellerPartyId,
      displayName: text(prop(item, "displayName", "DisplayName"), text(prop(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده")),
      status: text(prop(item, "status", "Status"), "Active"),
      relationship: text(prop(item, "relationship", "Relationship"), "فروشنده"),
      activeOfferCount: number(prop(item, "activeOfferCount", "ActiveOfferCount") ?? prop(item, "activeOffers", "ActiveOffers")),
      orderCount: number(prop(item, "orderCount", "OrderCount")),
    }];
  });
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

/** فروشندگان را از read composition زنده می‌خواند. */
export function loadAdminSellers(): Promise<AdminResult<AdminSellerRow[]>> {
  return mapped("/v1/admin/sellers", (value) => Array.isArray(value) ? mapAdminSellers(value) : null);
}

/** Server GridQuery — فروشندگان Admin. */
export function queryAdminSellersGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminSellerRow>> {
  return postAdminGridQuery("/v1/admin/sellers/query", query, adminHeaders(), (item) => {
    const rows = mapAdminSellers([item]);
    return rows[0] ?? null;
  });
}
