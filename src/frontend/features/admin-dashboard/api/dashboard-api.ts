/**
 * Admin client for operational dashboard metrics.
 */
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

function adminHeaders(): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId() };
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

/** متریک‌های زندهٔ داشبورد Admin. */
export interface AdminDashboard {
  activeProducts: number;
  activeOffers: number;
  openOrders: number;
  paidOrders: number;
  pendingOrders: number;
  sellersCount: number;
  customersCount: number;
}

/** خلاصهٔ زندهٔ داشبورد را از DTO با casing رایج Host نگاشت می‌کند. */
export function mapAdminDashboard(value: unknown): AdminDashboard | null {
  const item = record(value);
  if (!item) return null;
  return {
    activeProducts: number(prop(item, "activeProducts", "ActiveProducts") ?? prop(item, "publishedProducts", "PublishedProducts")),
    activeOffers: number(prop(item, "activeOffers", "ActiveOffers")),
    openOrders: number(prop(item, "openOrders", "OpenOrders")),
    paidOrders: number(prop(item, "paidOrders", "PaidOrders")),
    pendingOrders: number(prop(item, "pendingOrders", "PendingOrders")),
    sellersCount: number(prop(item, "sellersCount", "SellersCount") ?? prop(item, "sellers", "Sellers")),
    customersCount: number(prop(item, "customersCount", "CustomersCount") ?? prop(item, "customers", "Customers")),
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

/** داشبورد عملیاتی را از Host می‌خواند. */
export function loadAdminDashboard(): Promise<AdminResult<AdminDashboard>> {
  return mapped("/v1/admin/dashboard", mapAdminDashboard);
}
