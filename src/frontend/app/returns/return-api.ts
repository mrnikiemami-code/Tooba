/**
 * کلاینت و نگاشت مرجوعی/بازپرداخت مشترک بین Customer/Seller/Admin.
 * فقط دادهٔ واقعی Host؛ بدون وضعیت ساختگی.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  type HostReadSource,
} from "../vendor-panel/seller-api.ts";

export interface ReturnItem {
  returnItemId: string;
  orderLineId: string;
  quantity: number;
  unitPriceSnapshot: number;
  currency: string;
  reservationId: string | null;
}

export interface RefundAttempt {
  refundAttemptId: string;
  paymentId: string;
  amount: number;
  currency: string;
  status: string;
  idempotencyKey: string;
  providerReference: string | null;
  failureCode: string | null;
  createdAt: string;
  completedAt: string | null;
}

/** مقصد بازپرداخت تایپ‌شده — بدون free-form. */
export type RefundDestination = "OriginalPayment" | "Wallet";

export const DEFAULT_REFUND_DESTINATION: RefundDestination = "OriginalPayment";

export interface ReturnSnapshot {
  returnRequestId: string;
  sellerOrderId: string;
  checkoutId: string;
  sellerPartyId: string;
  requestedByUserId: string;
  status: string;
  reason: string | null;
  currency: string;
  refundAmount: number;
  paymentId: string | null;
  /** مقصد بازپرداخت؛ پیش‌فرض OriginalPayment اگر Host نفرستد. */
  destination: RefundDestination;
  createdAt: string;
  updatedAt: string;
  items: ReturnItem[];
  refundAttempts: RefundAttempt[];
}

export interface ReturnListRow {
  id: string;
  returnRequestId: string;
  sellerOrderId: string;
  checkoutId: string;
  status: string;
  refundAmount: number;
  currency: string;
  itemCount: number;
  createdAt: string;
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

function nullableText(value: unknown): string | null {
  return value == null || String(value).length === 0 ? null : String(value);
}

/** مقصد بازپرداخت را نرمال می‌کند؛ مقدار ناشناخته → OriginalPayment. */
export function normalizeRefundDestination(value: unknown): RefundDestination {
  // Host may serialize enum as number (Wallet=1) or string.
  if (typeof value === "number") {
    return value === 1 ? "Wallet" : "OriginalPayment";
  }
  const raw = text(value).trim();
  if (raw === "1" || raw === "Wallet" || raw === "wallet") return "Wallet";
  return "OriginalPayment";
}

/** برچسب فارسی مقصد بازپرداخت. */
export function formatRefundDestination(destination: RefundDestination | string): string {
  return normalizeRefundDestination(destination) === "Wallet" ? "کیف پول" : "پرداخت اصلی";
}

function mapReturnItem(value: unknown): ReturnItem | null {
  const item = record(value);
  if (!item) return null;
  const returnItemId = text(prop(item, "returnItemId", "ReturnItemId"));
  const orderLineId = text(prop(item, "orderLineId", "OrderLineId"));
  if (!returnItemId || !orderLineId) return null;
  return {
    returnItemId,
    orderLineId,
    quantity: number(prop(item, "quantity", "Quantity")),
    unitPriceSnapshot: number(prop(item, "unitPriceSnapshot", "UnitPriceSnapshot")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    reservationId: nullableText(prop(item, "reservationId", "ReservationId")),
  };
}

function mapRefundAttempt(value: unknown): RefundAttempt | null {
  const item = record(value);
  if (!item) return null;
  const refundAttemptId = text(prop(item, "refundAttemptId", "RefundAttemptId"));
  const paymentId = text(prop(item, "paymentId", "PaymentId"));
  if (!refundAttemptId || !paymentId) return null;
  return {
    refundAttemptId,
    paymentId,
    amount: number(prop(item, "amount", "Amount")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    status: text(prop(item, "status", "Status")),
    idempotencyKey: text(prop(item, "idempotencyKey", "IdempotencyKey")),
    providerReference: nullableText(prop(item, "providerReference", "ProviderReference")),
    failureCode: nullableText(prop(item, "failureCode", "FailureCode")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    completedAt: nullableText(prop(item, "completedAt", "CompletedAt")),
  };
}

/** snapshot مرجوعی را از JSON Host نگاشت می‌کند. */
export function mapReturnSnapshot(value: unknown): ReturnSnapshot | null {
  const item = record(value);
  if (!item) return null;
  const returnRequestId = text(prop(item, "returnRequestId", "ReturnRequestId"));
  if (!returnRequestId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const attemptsRaw = prop(item, "refundAttempts", "RefundAttempts");
  return {
    returnRequestId,
    sellerOrderId: text(prop(item, "sellerOrderId", "SellerOrderId")),
    checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    requestedByUserId: text(prop(item, "requestedByUserId", "RequestedByUserId")),
    status: text(prop(item, "status", "Status")),
    reason: nullableText(prop(item, "reason", "Reason")),
    currency: text(prop(item, "currency", "Currency"), "IRR"),
    refundAmount: number(prop(item, "refundAmount", "RefundAmount")),
    paymentId: nullableText(prop(item, "paymentId", "PaymentId")),
    destination: normalizeRefundDestination(
      prop(item, "refundDestination", "RefundDestination") ?? prop(item, "destination", "Destination"),
    ),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    items: Array.isArray(itemsRaw)
      ? itemsRaw.map(mapReturnItem).filter((row): row is ReturnItem => row !== null)
      : [],
    refundAttempts: Array.isArray(attemptsRaw)
      ? attemptsRaw.map(mapRefundAttempt).filter((row): row is RefundAttempt => row !== null)
      : [],
  };
}

/** فهرست مرجوعی را برای grid نگاشت می‌کند. */
export function mapReturnList(value: unknown): ReturnListRow[] {
  const items = Array.isArray(value) ? value : [];
  return items.flatMap((raw): ReturnListRow[] => {
    const snapshot = mapReturnSnapshot(raw);
    if (!snapshot) return [];
    return [{
      id: snapshot.returnRequestId,
      returnRequestId: snapshot.returnRequestId,
      sellerOrderId: snapshot.sellerOrderId,
      checkoutId: snapshot.checkoutId,
      status: snapshot.status,
      refundAmount: snapshot.refundAmount,
      currency: snapshot.currency,
      itemCount: snapshot.items.length,
      createdAt: snapshot.createdAt,
    }];
  });
}

/** وضعیت مرجوعی را برای UI فارسی می‌کند. */
export function formatReturnStatus(status: string): string {
  const labels: Record<string, string> = {
    Requested: "در انتظار بررسی",
    Approved: "تأیید شده",
    Rejected: "رد شده",
    RefundProcessing: "در حال بازپرداخت",
    Completed: "تکمیل شده",
    RefundFailed: "بازپرداخت ناموفق",
    Cancelled: "لغو شده",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** وضعیت تلاش refund را فارسی می‌کند. */
export function formatRefundAttemptStatus(status: string): string {
  const labels: Record<string, string> = {
    Pending: "در انتظار",
    Succeeded: "موفق",
    Failed: "ناموفق",
  };
  return labels[status] ?? (status || "نامشخص");
}

function returnStatusClasses(status: string): string {
  switch (status) {
    case "Completed":
      return "bg-emerald-50 text-emerald-700";
    case "Rejected":
    case "RefundFailed":
    case "Cancelled":
      return "bg-red-50 text-red-700";
    case "RefundProcessing":
    case "Approved":
      return "bg-blue-50 text-[#2563EB]";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

/** badge وضعیت مرجوعی. */
export function returnStatusBadgeClass(status: string): string {
  return `inline-flex rounded-xl px-3 py-1 text-xs font-bold ${returnStatusClasses(status)}`;
}

/** تاریخ ISO را برای نمایش فارسی برمی‌گرداند. */
export function formatReturnDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date);
}

/** فهرست مرجوعی‌های مشتری. */
export async function loadCustomerReturns(): Promise<ReturnSnapshot[] | null> {
  try {
    const response = await fetch("/api/customer/returns", {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return [];
    if (!response.ok) return null;
    const payload = await response.json();
    if (!Array.isArray(payload)) return null;
    return payload.map(mapReturnSnapshot).filter((row): row is ReturnSnapshot => row !== null);
  } catch {
    return null;
  }
}

/** جزئیات مرجوعی مشتری. */
export async function loadCustomerReturnDetail(returnRequestId: string): Promise<ReturnSnapshot | null> {
  try {
    const response = await fetch(`/api/customer/returns/${encodeURIComponent(returnRequestId)}`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return null;
    if (!response.ok) return null;
    return mapReturnSnapshot(await response.json());
  } catch {
    return null;
  }
}

export interface CreateReturnLineInput {
  orderLineId: string;
  quantity: number;
}

/** درخواست مرجوعی مشتری را ثبت می‌کند. */
export async function createCustomerReturn(input: {
  sellerOrderId: string;
  reason?: string;
  items: CreateReturnLineInput[];
  destination?: RefundDestination;
  idempotencyKey?: string;
}): Promise<{ ok: true; snapshot: ReturnSnapshot } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch("/api/customer/returns", {
      method: "POST",
      credentials: "include",
      headers: { ...customerAuthHeaders(), "Content-Type": "application/json" },
      body: JSON.stringify({
        sellerOrderId: input.sellerOrderId,
        idempotencyKey: input.idempotencyKey ?? crypto.randomUUID(),
        reason: input.reason ?? null,
        destination: input.destination ?? DEFAULT_REFUND_DESTINATION,
        refundDestination: input.destination ?? DEFAULT_REFUND_DESTINATION,
        items: input.items,
      }),
    });
    if (!response.ok) {
      const payload = record(await response.json().catch(() => null));
      return { ok: false, errorCode: text(prop(payload ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    const snapshot = mapReturnSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "return.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

function sellerHeaders(sellerPartyId: string, json = false): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  if (json) headers["Content-Type"] = "application/json";
  const actor = readActorUserId();
  if (actor) headers[DEV_ACTOR_HEADER] = actor;
  return headers;
}

function adminActorHeader(json = false): Record<string, string> {
  const actor = typeof window !== "undefined"
    ? window.localStorage.getItem("tooba.adminActorUserId") ?? ""
    : "";
  const headers: Record<string, string> = { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actor };
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

/** فهرست مرجوعی فروشنده. */
export async function loadSellerReturns(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: ReturnListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/returns", { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) return { source: "error", rows: [], message: `seller-return-http-${response.status}` };
    return { source: "host", rows: mapReturnList(await response.json()) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/** جزئیات مرجوعی فروشنده. */
export async function loadSellerReturnDetail(
  sellerPartyId: string,
  returnRequestId: string,
): Promise<{ source: HostReadSource; snapshot: ReturnSnapshot | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/returns/${encodeURIComponent(returnRequestId)}`, {
      headers: sellerHeaders(sellerPartyId),
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { source: "error", snapshot: null, message: "return.missing", denied: true };
    }
    if (!response.ok) return { source: "error", snapshot: null, message: `seller-return-http-${response.status}` };
    return { source: "host", snapshot: mapReturnSnapshot(await response.json()) };
  } catch {
    return { source: "error", snapshot: null, message: "host-unreachable" };
  }
}

async function sellerMutate(
  sellerPartyId: string,
  path: string,
  body?: unknown,
): Promise<{ ok: true; snapshot: ReturnSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(path, {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, Boolean(body)),
      body: body ? JSON.stringify(body) : undefined,
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      const payload = record(await response.json().catch(() => null));
      return { ok: false, errorCode: text(prop(payload ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    const snapshot = mapReturnSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "return.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export function sellerApproveReturn(
  sellerPartyId: string,
  returnRequestId: string,
  destination?: RefundDestination,
) {
  const dest = destination ?? DEFAULT_REFUND_DESTINATION;
  return sellerMutate(sellerPartyId, `/v1/seller/returns/${returnRequestId}/approve`, {
    destination: dest,
    refundDestination: dest,
  });
}

export function sellerRejectReturn(sellerPartyId: string, returnRequestId: string, reason?: string) {
  return sellerMutate(sellerPartyId, `/v1/seller/returns/${returnRequestId}/reject`, { reason: reason ?? null });
}

/** فهرست مرجوعی Admin. */
export async function loadAdminReturns(): Promise<AdminResult<ReturnListRow[]>> {
  try {
    const response = await fetch("/v1/admin/returns", { headers: adminActorHeader() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: mapReturnList(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — مرجوعی Admin. */
export function queryAdminReturnsGrid(query: GridServerQuery): Promise<AdminGridQueryResult<ReturnListRow>> {
  return postAdminGridQuery("/v1/admin/returns/query", query, adminActorHeader(), (item) => {
    const rows = mapReturnList([item]);
    return rows[0] ?? null;
  });
}

/** جزئیات مرجوعی Admin. */
export async function loadAdminReturnDetail(returnRequestId: string): Promise<AdminResult<ReturnSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/returns/${encodeURIComponent(returnRequestId)}`, {
      headers: adminActorHeader(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) return { state: "ok", data: null, status: 404 };
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: mapReturnSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** retry refund توسط Admin. */
export async function adminRetryReturnRefund(returnRequestId: string): Promise<AdminResult<ReturnSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/returns/${encodeURIComponent(returnRequestId)}/retry-refund`, {
      method: "POST",
      headers: adminActorHeader(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: text(prop(record(payload) ?? {}, "errorCode", "ErrorCode"), "return.rejected") };
    }
    return { state: "ok", data: mapReturnSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
