/**
 * Admin client for known-buyer (customer) directory list.
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

/** ردیف خریدار شناخته‌شدهٔ Admin؛ CRM نیست. */
export interface AdminCustomerRow {
  id: string;
  actorUserId: string;
  displayName: string;
  contact: string;
  orderCount: number;
  lastActivityAt: string | null;
  status: string;
}

/** فهرست خریداران شناخته‌شده از سفارش‌ها را نگاشت می‌کند؛ این مدل CRM نیست. */
export function mapAdminCustomers(value: unknown): AdminCustomerRow[] {
  return array(value).flatMap((raw): AdminCustomerRow[] => {
    const item = record(raw);
    if (!item) return [];
    const actorUserId = text(prop(item, "actorUserId", "ActorUserId"), text(prop(item, "customerUserId", "CustomerUserId"), text(prop(item, "customerId", "CustomerId"))));
    if (!actorUserId) return [];
    return [{
      id: actorUserId,
      actorUserId,
      displayName: text(prop(item, "displayName", "DisplayName"), text(prop(item, "customerDisplayName", "CustomerDisplayName"), "مشتری")),
      contact: text(prop(item, "contact", "Contact"), text(prop(item, "contactMobile", "ContactMobile"), "—")),
      orderCount: number(prop(item, "orderCount", "OrderCount")),
      lastActivityAt: text(prop(item, "lastActivityAt", "LastActivityAt"), text(prop(item, "lastOrderAt", "LastOrderAt"))) || null,
      status: text(prop(item, "status", "Status"), "Active"),
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

/** خریداران شناخته‌شده را از read composition زنده می‌خواند. */
export function loadAdminCustomers(): Promise<AdminResult<AdminCustomerRow[]>> {
  return mapped("/v1/admin/customers", (value) => Array.isArray(value) ? mapAdminCustomers(value) : null);
}

/** Server GridQuery — مشتریان Admin. */
export function queryAdminCustomersGrid(query: GridServerQuery): Promise<AdminGridQueryResult<AdminCustomerRow>> {
  return postAdminGridQuery("/v1/admin/customers/query", query, adminHeaders(), (item) => {
    const rows = mapAdminCustomers([item]);
    return rows[0] ?? null;
  });
}
