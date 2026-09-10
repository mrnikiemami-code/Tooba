/**
 * کلاینت و نگاشت fulfillment مشترک بین Customer/Seller/Admin.
 * فقط دادهٔ واقعی Host؛ بدون shipment/tracking ساختگی.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import { readCartSession } from "../storefront/storefront-cart-api.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
  type HostReadSource,
} from "../vendor-panel/seller-api.ts";
import { formatQuantityDisplay } from "../../lib/quantity-display.ts";

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
  preferredTrackingReference?: string | null;
}

export interface FulfillmentListRow {
  id: string;
  fulfillmentId: string;
  sellerOrderId: string;
  checkoutId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  orderReference: string;
  status: string;
  recipientName: string;
  cityName: string;
  shippingMethodCode: string;
  shippingMethodLabel: string;
  itemCount: number;
  quantityOrdered: number;
  quantityShipped: number;
  shipmentCount: number;
  primaryShipmentId: string | null;
  trackingSummary: string;
  trackingReferences: string[];
  createdAt: string;
  updatedAt: string;
  availableActionCodes: string[];
}

export type FulfillmentQueueFilter =
  | "all"
  | "needs_action"
  | "ready_to_process"
  | "ready_to_pack"
  | "ready_to_ship"
  | "missing_tracking"
  | "in_transit"
  | "delivered"
  | "problem";

export const FULFILLMENT_QUEUE_FILTERS: Array<{ id: FulfillmentQueueFilter; labelFa: string; labelEn: string }> = [
  { id: "all", labelFa: "همه", labelEn: "All" },
  { id: "needs_action", labelFa: "نیازمند اقدام", labelEn: "Needs action" },
  { id: "ready_to_process", labelFa: "آماده پردازش", labelEn: "Ready to process" },
  { id: "ready_to_pack", labelFa: "آماده بسته‌بندی", labelEn: "Ready to pack" },
  { id: "ready_to_ship", labelFa: "آماده ارسال", labelEn: "Ready to ship" },
  { id: "missing_tracking", labelFa: "بدون کد رهگیری", labelEn: "Missing tracking" },
  { id: "in_transit", labelFa: "در مسیر", labelEn: "In transit" },
  { id: "delivered", labelFa: "تحویل‌شده", labelEn: "Delivered" },
  { id: "problem", labelFa: "مشکل‌دار", labelEn: "Problem" },
];

export const FULFILLMENT_SAFE_BULK_ACTION_CODES = [
  "mark_processing",
  "mark_packed",
  "dispatch_shipment",
  "deliver_shipment",
] as const;

export const FULFILLMENT_BULK_ACTION_LABELS: Record<string, { fa: string; en: string }> = {
  mark_processing: { fa: "شروع پردازش گروهی", en: "Bulk mark processing" },
  mark_packed: { fa: "بسته‌بندی گروهی", en: "Bulk pack" },
  dispatch_shipment: { fa: "ارسال گروهی", en: "Bulk dispatch" },
  deliver_shipment: { fa: "ثبت تحویل گروهی", en: "Bulk deliver" },
};

/** نمایش مقدار صف کار بدون صفر ذخیره‌سازی و بدون جمع واحدهای ناهمگن به‌عنوان یک واحد. */
export function formatFulfillmentQueueQuantity(row: Pick<FulfillmentListRow, "itemCount" | "quantityOrdered" | "quantityShipped">): string {
  const lines = row.itemCount.toLocaleString("fa-IR");
  const ordered = formatQuantityDisplay(row.quantityOrdered);
  const remaining = formatQuantityDisplay(Math.max(0, row.quantityOrdered - row.quantityShipped));
  return `${lines} قلم · مقدار ${ordered} · باقی ${remaining}`;
}

/** برچسب انسانی مرسوله — بدون GUID خام. */
export function formatFulfillmentShipmentSummary(row: Pick<FulfillmentListRow, "shipmentCount">): string {
  if (row.shipmentCount <= 0) return "—";
  return `${row.shipmentCount.toLocaleString("fa-IR")} مرسوله`;
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
    preferredTrackingReference: nullableText(prop(item, "preferredTrackingReference", "PreferredTrackingReference")),
  };
}

/** فهرست fulfillment را برای grid نگاشت می‌کند. */
export function mapFulfillmentList(value: unknown): FulfillmentListRow[] {
  const items = Array.isArray(value) ? value : [];
  return items.flatMap((raw): FulfillmentListRow[] => {
    const item = record(raw);
    if (!item) return [];

    // Work-queue row shape (AdminFulfillmentWorkQueueRow)
    const fulfillmentId = text(prop(item, "fulfillmentId", "FulfillmentId"));
    if (fulfillmentId && (prop(item, "orderReference", "OrderReference") != null || prop(item, "availableActionCodes", "AvailableActionCodes") != null)) {
      const trackingSummary = text(prop(item, "trackingSummary", "TrackingSummary"));
      const codesRaw = prop(item, "availableActionCodes", "AvailableActionCodes");
      const availableActionCodes = Array.isArray(codesRaw)
        ? codesRaw.map((c) => text(c)).filter(Boolean)
        : [];
      return [{
        id: fulfillmentId,
        fulfillmentId,
        sellerOrderId: text(prop(item, "sellerOrderId", "SellerOrderId")),
        checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
        sellerPartyId: text(prop(item, "sellerPartyId", "SellerPartyId")),
        sellerDisplayName: text(prop(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
        orderReference: text(prop(item, "orderReference", "OrderReference")),
        status: normalizeFulfillmentStatus(prop(item, "status", "Status")),
        recipientName: text(prop(item, "recipientName", "RecipientName")),
        cityName: text(prop(item, "cityName", "CityName")),
        shippingMethodCode: text(prop(item, "shippingMethodCode", "ShippingMethodCode")),
        shippingMethodLabel: text(prop(item, "shippingMethodLabel", "ShippingMethodLabel")),
        itemCount: number(prop(item, "itemCount", "ItemCount")),
        quantityOrdered: number(prop(item, "quantityOrdered", "QuantityOrdered")),
        quantityShipped: number(prop(item, "quantityShipped", "QuantityShipped")),
        shipmentCount: number(prop(item, "shipmentCount", "ShipmentCount")),
        primaryShipmentId: nullableText(prop(item, "primaryShipmentId", "PrimaryShipmentId")),
        trackingSummary,
        trackingReferences: trackingSummary ? trackingSummary.split(" · ").filter(Boolean) : [],
        createdAt: text(prop(item, "createdAt", "CreatedAt")),
        updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
        availableActionCodes,
      }];
    }

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
      sellerPartyId: snapshot.sellerPartyId,
      sellerDisplayName: "",
      orderReference: snapshot.checkoutId.slice(0, 8),
      status: snapshot.status,
      recipientName: snapshot.recipientName,
      cityName: snapshot.cityName,
      shippingMethodCode: snapshot.shippingMethodCode,
      shippingMethodLabel: snapshot.shippingMethodLabel,
      itemCount: snapshot.items.length,
      quantityOrdered: snapshot.items.reduce((sum, row) => sum + row.quantityOrdered, 0),
      quantityShipped: snapshot.items.reduce((sum, row) => sum + row.quantityShipped, 0),
      shipmentCount: snapshot.shipments.length,
      primaryShipmentId: snapshot.shipments[0]?.shipmentId ?? null,
      trackingSummary: trackingReferences.join(" · "),
      trackingReferences,
      createdAt: "",
      updatedAt: "",
      availableActionCodes: [],
    }];
  });
}

/** آیا انتخاب گروهی برای یک کد عملیات سازگار است (همان فروشنده + همه دارای کد). */
export function areFulfillmentBulkCompatible(rows: FulfillmentListRow[], actionCode: string): boolean {
  if (rows.length === 0 || !actionCode) return false;
  const seller = rows[0]!.sellerPartyId;
  if (rows.some((row) => row.sellerPartyId !== seller)) return false;
  return rows.every((row) => row.availableActionCodes.includes(actionCode));
}

/** وضعیت fulfillment را برای UI فارسی می‌کند. */
export function formatFulfillmentStatus(status: string): string {
  const labels: Record<string, string> = {
    ReadyToFulfill: "آماده پردازش",
    Processing: "در حال پردازش",
    Packed: "بسته‌بندی‌شده",
    PartialDispatched: "ارسال جزئی",
    Dispatched: "ارسال‌شده",
    InTransit: "در مسیر تحویل",
    Delivered: "تحویل‌شده",
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
export async function loadCustomerFulfillments(checkoutId: string): Promise<{
  snapshots: FulfillmentSnapshot[];
  preferredTrackingReference: string | null;
  preferredTrackingPackageNumber: string | null;
} | null> {
  try {
    const headers: Record<string, string> = { ...customerAuthHeaders() };
    const guestSecret = readCartSession().guestSecret;
    if (guestSecret) {
      headers["X-Tooba-Guest-Secret"] = guestSecret;
    }
    const response = await fetch(`/api/customer/orders/${encodeURIComponent(checkoutId)}/fulfillments`, {
      credentials: "include",
      headers,
    });
    if (response.status === 404) {
      return { snapshots: [], preferredTrackingReference: null, preferredTrackingPackageNumber: null };
    }
    if (!response.ok) return null;
    const payload = await response.json();
    if (Array.isArray(payload)) {
      return {
        snapshots: payload.map(mapFulfillmentSnapshot).filter((row): row is FulfillmentSnapshot => row !== null),
        preferredTrackingReference: null,
        preferredTrackingPackageNumber: null,
      };
    }
    if (!payload || typeof payload !== "object") return null;
    const row = payload as Record<string, unknown>;
    const list = Array.isArray(row.fulfillments)
      ? row.fulfillments
      : Array.isArray(row.Fulfillments)
        ? row.Fulfillments
        : null;
    if (!list) return null;
    const preferred =
      (typeof row.preferredCustomerTrackingReference === "string" && row.preferredCustomerTrackingReference)
      || (typeof row.PreferredCustomerTrackingReference === "string" && row.PreferredCustomerTrackingReference)
      || null;
    const packageNumber =
      (typeof row.preferredCustomerTrackingPackageNumber === "string" && row.preferredCustomerTrackingPackageNumber)
      || (typeof row.PreferredCustomerTrackingPackageNumber === "string" && row.PreferredCustomerTrackingPackageNumber)
      || null;
    return {
      snapshots: list
        .map(mapFulfillmentSnapshot)
        .filter((row): row is FulfillmentSnapshot => row !== null)
        .map((snapshot) => ({
          ...snapshot,
          preferredTrackingReference: preferred ?? snapshot.preferredTrackingReference ?? null,
        })),
      preferredTrackingReference: preferred,
      preferredTrackingPackageNumber: packageNumber,
    };
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

/** Server GridQuery — صف کار ارسال و تحویل Admin. */
export function queryAdminFulfillmentsGrid(
  query: GridServerQuery,
): Promise<AdminGridQueryResult<FulfillmentListRow>> {
  return postAdminGridQuery("/v1/admin/fulfillments/work-queue/query", query, adminActorHeader(), (item) => {
    const rows = mapFulfillmentList([item]);
    return rows[0] ?? null;
  });
}

/** اجرای گروهی عملیات صف کار — همان فرمان‌های Order Operations. */
export async function executeAdminFulfillmentWorkQueueBulk(body: {
  actionCode: string;
  items: Array<{
    checkoutId: string;
    fulfillmentId: string;
    sellerOrderId: string;
    shipmentId?: string | null;
  }>;
  trackingReference?: string | null;
  carrierDisplayName?: string | null;
  shippingMethodCode?: string | null;
}): Promise<AdminResult<{ attempted: number; succeeded: number }>> {
  try {
    const response = await fetch("/v1/admin/fulfillments/work-queue/bulk", {
      method: "POST",
      headers: { ...adminActorHeader(), "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const recordPayload = record(payload);
      const message =
        text(prop(recordPayload ?? {}, "detail", "Detail"))
        || text(prop(recordPayload ?? {}, "title", "Title"))
        || text(prop(recordPayload ?? {}, "errorCode", "ErrorCode"))
        || `admin.http.${response.status}`;
      return { state: "error", data: null, status: response.status, message };
    }
    const row = record(payload) ?? {};
    return {
      state: "ok",
      data: {
        attempted: number(prop(row, "attempted", "Attempted")),
        succeeded: number(prop(row, "succeeded", "Succeeded")),
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
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
