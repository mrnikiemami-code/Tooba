/**
 * کلاینت و نگاشت fulfillment مشترک بین Customer/Seller/Admin.
 * فقط دادهٔ واقعی Host؛ بدون shipment/tracking ساختگی.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
  type HostReadSource,
} from "../vendor-panel/seller-api.ts";

export interface FulfillmentShipmentLine {
  orderLineId: string;
  quantity: number;
}

export interface FulfillmentShipment {
  shipmentId: string;
  status: string;
  carrierDisplayName: string;
  trackingReference: string | null;
  dispatchedAt: string | null;
  deliveredAt: string | null;
  items: FulfillmentShipmentLine[];
}

export interface FulfillmentItem {
  fulfillmentItemId: string;
  orderLineId: string;
  quantityOrdered: number;
  quantityShipped: number;
  reservationId: string | null;
}

export interface FulfillmentSnapshot {
  fulfillmentId: string;
  sellerOrderId: string;
  checkoutId: string;
  sellerPartyId: string;
  status: string;
  recipientName: string;
  contactMobile: string;
  provinceName: string;
  cityName: string;
  postalAddress: string;
  postalCode: string;
  shippingMethodCode: string;
  shippingMethodLabel: string;
  items: FulfillmentItem[];
  shipments: FulfillmentShipment[];
}

export interface FulfillmentListRow {
  id: string;
  fulfillmentId: string;
  sellerOrderId: string;
  checkoutId: string;
  status: string;
  recipientName: string;
  cityName: string;
  shipmentCount: number;
  trackingReferences: string[];
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

const FULFILLMENT_STATUS_BY_NUMBER: Record<number, string> = {
  0: "ReadyToFulfill",
  1: "Processing",
  2: "Packed",
  3: "Dispatched",
  4: "InTransit",
  5: "Delivered",
  6: "Failed",
  7: "Cancelled",
};

const SHIPMENT_STATUS_BY_NUMBER: Record<number, string> = {
  0: "Created",
  1: "Dispatched",
  2: "InTransit",
  3: "Delivered",
  4: "Failed",
  5: "Cancelled",
};

/** Host may serialize enums as numbers; UI gates compare canonical names. */
export function normalizeFulfillmentStatus(value: unknown): string {
  if (typeof value === "number" && FULFILLMENT_STATUS_BY_NUMBER[value]) {
    return FULFILLMENT_STATUS_BY_NUMBER[value];
  }
  return text(value);
}

export function normalizeShipmentStatus(value: unknown): string {
  if (typeof value === "number" && SHIPMENT_STATUS_BY_NUMBER[value]) {
    return SHIPMENT_STATUS_BY_NUMBER[value];
  }
  return text(value);
}

function mapShipmentLine(value: unknown): FulfillmentShipmentLine | null {
  const item = record(value);
  if (!item) return null;
  const orderLineId = text(prop(item, "orderLineId", "OrderLineId"));
  if (!orderLineId) return null;
  return { orderLineId, quantity: number(prop(item, "quantity", "Quantity")) };
}

function mapShipment(value: unknown): FulfillmentShipment | null {
  const item = record(value);
  if (!item) return null;
  const shipmentId = text(prop(item, "shipmentId", "ShipmentId"));
  if (!shipmentId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const items = Array.isArray(itemsRaw)
    ? itemsRaw.map(mapShipmentLine).filter((row): row is FulfillmentShipmentLine => row !== null)
    : [];
  return {
    shipmentId,
    status: normalizeShipmentStatus(prop(item, "status", "Status")),
    carrierDisplayName: text(prop(item, "carrierDisplayName", "CarrierDisplayName")),
    trackingReference: nullableText(prop(item, "trackingReference", "TrackingReference")),
    dispatchedAt: nullableText(prop(item, "dispatchedAt", "DispatchedAt")),
    deliveredAt: nullableText(prop(item, "deliveredAt", "DeliveredAt")),
    items,
  };
}

function mapItem(value: unknown): FulfillmentItem | null {
  const item = record(value);
  if (!item) return null;
  const fulfillmentItemId = text(prop(item, "fulfillmentItemId", "FulfillmentItemId"));
  const orderLineId = text(prop(item, "orderLineId", "OrderLineId"));
  if (!fulfillmentItemId || !orderLineId) return null;
  return {
    fulfillmentItemId,
    orderLineId,
    quantityOrdered: number(prop(item, "quantityOrdered", "QuantityOrdered")),
    quantityShipped: number(prop(item, "quantityShipped", "QuantityShipped")),
    reservationId: nullableText(prop(item, "reservationId", "ReservationId")),
  };
}

/** snapshot fulfillment را از JSON Host نگاشت می‌کند. */
export function mapFulfillmentSnapshot(value: unknown): FulfillmentSnapshot | null {
  const item = record(value);
  if (!item) return null;
  const fulfillmentId = text(prop(item, "fulfillmentId", "FulfillmentId"));
  if (!fulfillmentId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const shipmentsRaw = prop(item, "shipments", "Shipments");
  return {
    fulfillmentId,
    sellerOrderId: text(prop(item, "sellerOrderId", "SellerOrderId")),
    checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
    sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
    status: normalizeFulfillmentStatus(prop(item, "status", "Status")),
    recipientName: text(prop(item, "recipientName", "RecipientName")),
    contactMobile: text(prop(item, "contactMobile", "ContactMobile")),
    provinceName: text(prop(item, "provinceName", "ProvinceName")),
    cityName: text(prop(item, "cityName", "CityName")),
    postalAddress: text(prop(item, "postalAddress", "PostalAddress")),
    postalCode: text(prop(item, "postalCode", "PostalCode")),
    shippingMethodCode: text(prop(item, "shippingMethodCode", "ShippingMethodCode")),
    shippingMethodLabel: text(prop(item, "shippingMethodLabel", "ShippingMethodLabel")),
    items: Array.isArray(itemsRaw)
      ? itemsRaw.map(mapItem).filter((row): row is FulfillmentItem => row !== null)
      : [],
    shipments: Array.isArray(shipmentsRaw)
      ? shipmentsRaw.map(mapShipment).filter((row): row is FulfillmentShipment => row !== null)
      : [],
  };
}

/** فهرست fulfillment را برای grid نگاشت می‌کند. */
export function mapFulfillmentList(value: unknown): FulfillmentListRow[] {
  const items = Array.isArray(value) ? value : [];
  return items.flatMap((raw): FulfillmentListRow[] => {
    const snapshot = mapFulfillmentSnapshot(raw);
    if (!snapshot) return [];
    const trackingReferences = snapshot.shipments
      .map((shipment) => shipment.trackingReference)
      .filter((tracking): tracking is string => Boolean(tracking));
    return [{
      id: snapshot.fulfillmentId,
      fulfillmentId: snapshot.fulfillmentId,
      sellerOrderId: snapshot.sellerOrderId,
      checkoutId: snapshot.checkoutId,
      status: snapshot.status,
      recipientName: snapshot.recipientName,
      cityName: snapshot.cityName,
      shipmentCount: snapshot.shipments.length,
      trackingReferences,
    }];
  });
}

/** وضعیت fulfillment را برای UI فارسی می‌کند. */
export function formatFulfillmentStatus(status: string): string {
  const labels: Record<string, string> = {
    ReadyToFulfill: "آماده ارسال",
    Processing: "در حال پردازش",
    Packed: "بسته‌بندی شده",
    Dispatched: "ارسال شده",
    InTransit: "در مسیر",
    Delivered: "تحویل شده",
    Failed: "ناموفق",
    Cancelled: "لغو شده",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** وضعیت محموله را فارسی می‌کند. */
export function formatShipmentStatus(status: string): string {
  const labels: Record<string, string> = {
    Created: "ایجاد شده",
    Dispatched: "ارسال شده",
    InTransit: "در مسیر",
    Delivered: "تحویل شده",
    Failed: "ناموفق",
    Cancelled: "لغو شده",
  };
  return labels[status] ?? (status || "نامشخص");
}

/** تاریخ ISO را برای نمایش فارسی برمی‌گرداند. */
export function formatFulfillmentDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" }).format(date);
}

function fulfillmentStatusClasses(status: string): string {
  switch (status) {
    case "Delivered":
      return "bg-emerald-50 text-emerald-700";
    case "Failed":
    case "Cancelled":
      return "bg-red-50 text-red-700";
    case "Dispatched":
    case "InTransit":
      return "bg-blue-50 text-[#2563EB]";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

/** badge وضعیت fulfillment با کلاس‌های موجود Shopeiva/Tooba. */
export function fulfillmentStatusBadgeClass(status: string): string {
  return `inline-flex rounded-xl px-3 py-1 text-xs font-bold ${fulfillmentStatusClasses(status)}`;
}

/** fulfillmentهای checkout مشتری را از BFF می‌خواند. */
export async function loadCustomerFulfillments(checkoutId: string): Promise<FulfillmentSnapshot[] | null> {
  try {
    const response = await fetch(`/api/customer/orders/${encodeURIComponent(checkoutId)}/fulfillments`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return [];
    if (!response.ok) return null;
    const payload = await response.json();
    if (!Array.isArray(payload)) return null;
    return payload.map(mapFulfillmentSnapshot).filter((row): row is FulfillmentSnapshot => row !== null);
  } catch {
    return null;
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

function adminActorHeader(): Record<string, string> {
  const actor = typeof window !== "undefined"
    ? window.localStorage.getItem("tooba.adminActorUserId") ?? ""
    : "";
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actor };
}

/** فهرست fulfillment فروشنده. */
export async function loadSellerFulfillments(
  sellerPartyId: string,
): Promise<{ source: HostReadSource; rows: FulfillmentListRow[]; message?: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/fulfillments", { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 401 || response.status === 403) {
      return { source: "error", rows: [], message: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) return { source: "error", rows: [], message: `seller-fulfillment-http-${response.status}` };
    return { source: "host", rows: mapFulfillmentList(await response.json()) };
  } catch {
    return { source: "error", rows: [], message: "host-unreachable" };
  }
}

/** جزئیات fulfillment فروشنده. */
export async function loadSellerFulfillmentDetail(
  sellerPartyId: string,
  fulfillmentId: string,
): Promise<{ source: HostReadSource; snapshot: FulfillmentSnapshot | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/fulfillments/${encodeURIComponent(fulfillmentId)}`, {
      headers: sellerHeaders(sellerPartyId),
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { source: "error", snapshot: null, message: "fulfillment.missing", denied: true };
    }
    if (!response.ok) return { source: "error", snapshot: null, message: `seller-fulfillment-http-${response.status}` };
    return { source: "host", snapshot: mapFulfillmentSnapshot(await response.json()) };
  } catch {
    return { source: "error", snapshot: null, message: "host-unreachable" };
  }
}

async function sellerMutate(
  sellerPartyId: string,
  path: string,
  body?: unknown,
): Promise<{ ok: true; snapshot: FulfillmentSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
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
      return { ok: false, errorCode: text(prop(payload ?? {}, "errorCode", "ErrorCode"), "fulfillment.rejected") };
    }
    const snapshot = mapFulfillmentSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "fulfillment.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export function sellerMarkProcessing(sellerPartyId: string, fulfillmentId: string) {
  return sellerMutate(sellerPartyId, `/v1/seller/fulfillments/${fulfillmentId}/processing`);
}

export function sellerMarkPacked(sellerPartyId: string, fulfillmentId: string) {
  return sellerMutate(sellerPartyId, `/v1/seller/fulfillments/${fulfillmentId}/packed`);
}

export function sellerCreateShipment(
  sellerPartyId: string,
  fulfillmentId: string,
  carrierDisplayName: string,
  items: FulfillmentShipmentLine[],
) {
  return sellerMutate(sellerPartyId, `/v1/seller/fulfillments/${fulfillmentId}/shipments`, {
    carrierDisplayName,
    items: items.map((item) => ({ orderLineId: item.orderLineId, quantity: item.quantity })),
  });
}

export function sellerAssignTracking(
  sellerPartyId: string,
  fulfillmentId: string,
  shipmentId: string,
  trackingReference: string,
) {
  return sellerMutate(
    sellerPartyId,
    `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/tracking`,
    { trackingReference },
  );
}

export function sellerDispatchShipment(sellerPartyId: string, fulfillmentId: string, shipmentId: string) {
  return sellerMutate(
    sellerPartyId,
    `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/dispatch`,
  );
}

export function sellerDeliverShipment(sellerPartyId: string, fulfillmentId: string, shipmentId: string) {
  return sellerMutate(
    sellerPartyId,
    `/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/deliver`,
  );
}

/** فهرست fulfillment برای Admin. */
export async function loadAdminFulfillments(): Promise<AdminResult<FulfillmentListRow[]>> {
  try {
    const response = await fetch("/v1/admin/fulfillments", { headers: adminActorHeader() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    return { state: "ok", data: mapFulfillmentList(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — fulfillment Admin. */
export function queryAdminFulfillmentsGrid(
  query: GridServerQuery,
): Promise<AdminGridQueryResult<FulfillmentListRow>> {
  return postAdminGridQuery("/v1/admin/fulfillments/query", query, adminActorHeader(), (item) => {
    const rows = mapFulfillmentList([item]);
    return rows[0] ?? null;
  });
}

/** جزئیات fulfillment برای Admin. */
export async function loadAdminFulfillmentDetail(fulfillmentId: string): Promise<AdminResult<FulfillmentSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/fulfillments/${encodeURIComponent(fulfillmentId)}`, {
      headers: adminActorHeader(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) {
      return { state: "error", data: null, status: response.status, message: "fulfillment.missing" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const snapshot = mapFulfillmentSnapshot(payload);
    return snapshot
      ? { state: "ok", data: snapshot, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** sellerPartyId فعال را برای صفحات fulfillment برمی‌گرداند. */
export function activeSellerPartyId(): string | null {
  if (typeof window === "undefined") return null;
  return readSellerPartyId(window.location.search);
}
