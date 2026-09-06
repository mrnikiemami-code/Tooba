/**
 * کلاینت تکمیل عملیاتی جزئیات سفارش (یادداشت / تاریخچه / فاکتور / رسید).
 */
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "./admin-error-map.ts";
import { ADMIN_DEV_ACTOR_HEADER, adminHeaders, type AdminResult } from "./admin-api.ts";

export interface AdminOrderNote {
  noteId: string;
  checkoutId: string;
  body: string;
  createdByUserId: string;
  createdAt: string;
  actorDisplayFa: string;
  actorDisplayEn: string;
}

export interface AdminOperationalHistoryEntry {
  occurredAt: string;
  kind: string;
  labelFa: string;
  labelEn: string;
  actorDisplayFa: string;
  actorDisplayEn: string;
  summaryFa?: string | null;
  summaryEn?: string | null;
}

export interface AdminOperationalHistoryPage {
  checkoutId: string;
  page: number;
  pageSize: number;
  totalCount: number;
  items: AdminOperationalHistoryEntry[];
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function number(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function mapNote(value: unknown): AdminOrderNote | null {
  const item = record(value);
  if (!item) return null;
  const noteId = text(prop(item, "noteId", "NoteId"));
  if (!noteId) return null;
  return {
    noteId,
    checkoutId: text(prop(item, "checkoutId", "CheckoutId")),
    body: text(prop(item, "body", "Body")),
    createdByUserId: text(prop(item, "createdByUserId", "CreatedByUserId")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    actorDisplayFa: text(prop(item, "actorDisplayFa", "ActorDisplayFa"), "توسط سیستم"),
    actorDisplayEn: text(prop(item, "actorDisplayEn", "ActorDisplayEn"), "By system"),
  };
}

function mapHistoryEntry(value: unknown): AdminOperationalHistoryEntry | null {
  const item = record(value);
  if (!item) return null;
  const occurredAt = text(prop(item, "occurredAt", "OccurredAt"));
  const kind = text(prop(item, "kind", "Kind"));
  if (!occurredAt || !kind) return null;
  return {
    occurredAt,
    kind,
    labelFa: text(prop(item, "labelFa", "LabelFa")),
    labelEn: text(prop(item, "labelEn", "LabelEn")),
    actorDisplayFa: text(prop(item, "actorDisplayFa", "ActorDisplayFa"), "توسط سیستم"),
    actorDisplayEn: text(prop(item, "actorDisplayEn", "ActorDisplayEn"), "By system"),
    summaryFa: text(prop(item, "summaryFa", "SummaryFa")) || null,
    summaryEn: text(prop(item, "summaryEn", "SummaryEn")) || null,
  };
}

function mapHistoryPage(value: unknown): AdminOperationalHistoryPage | null {
  const root = record(value);
  if (!root) return null;
  const rawItems = prop(root, "items", "Items");
  if (!Array.isArray(rawItems)) return null;
  return {
    checkoutId: text(prop(root, "checkoutId", "CheckoutId")),
    page: Math.max(1, number(prop(root, "page", "Page")) || 1),
    pageSize: Math.max(1, number(prop(root, "pageSize", "PageSize")) || 20),
    totalCount: Math.max(0, number(prop(root, "totalCount", "TotalCount"))),
    items: rawItems.flatMap((row) => {
      const mapped = mapHistoryEntry(row);
      return mapped ? [mapped] : [];
    }),
  };
}

async function readJson(path: string, init?: RequestInit): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      ...init,
      headers: {
        Accept: "application/json",
        [ADMIN_DEV_ACTOR_HEADER]: actorId(),
        ...(init?.headers ?? {}),
      },
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "order.operation.denied" };
    }
    if (!response.ok) {
      const code = parseAdminProblemErrorCode(payload, response.status);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapAdminErrorMessage(code || `admin.http.${response.status}`, "fa"),
      };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

/** فهرست یادداشت‌های داخلی سفارش. */
export async function loadAdminOrderNotes(checkoutId: string): Promise<AdminResult<AdminOrderNote[]>> {
  const response = await readJson(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/notes`);
  if (response.state !== "ok") return { ...response, data: null };
  if (!Array.isArray(response.data)) {
    return { state: "error", data: null, status: response.status, message: mapAdminErrorMessage("admin.invalid-response", "fa") };
  }
  return {
    ...response,
    data: response.data.flatMap((row) => {
      const mapped = mapNote(row);
      return mapped ? [mapped] : [];
    }),
  };
}

/** افزودن یادداشت داخلی append-only. */
export async function addAdminOrderNote(
  checkoutId: string,
  body: string,
): Promise<AdminResult<AdminOrderNote>> {
  const response = await readJson(`/v1/admin/orders/${encodeURIComponent(checkoutId)}/notes`, {
    method: "POST",
    headers: adminHeaders({ "content-type": "application/json" }),
    body: JSON.stringify({ body }),
  });
  if (response.state !== "ok") return { ...response, data: null };
  const mapped = mapNote(response.data);
  return mapped
    ? { ...response, data: mapped }
    : { state: "error", data: null, status: response.status, message: mapAdminErrorMessage("admin.invalid-response", "fa") };
}

/** صفحهٔ تاریخچهٔ عملیاتی ترکیبی. */
export async function loadAdminOrderOperationalHistory(
  checkoutId: string,
  page = 1,
  pageSize = 20,
): Promise<AdminResult<AdminOperationalHistoryPage>> {
  const qs = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  const response = await readJson(
    `/v1/admin/orders/${encodeURIComponent(checkoutId)}/operational-history?${qs}`,
  );
  if (response.state !== "ok") {
    return {
      ...response,
      data: null,
      message: response.message || mapAdminErrorMessage("order.history.failed", "fa"),
    };
  }
  const mapped = mapHistoryPage(response.data);
  return mapped
    ? { ...response, data: mapped }
    : { state: "error", data: null, status: response.status, message: mapAdminErrorMessage("order.history.failed", "fa") };
}

/** مسیر HTML فاکتور قابل‌چاپ (rewrite به Host با همان هدر Actor). */
export function adminOrderInvoiceUrl(checkoutId: string): string {
  return `/v1/admin/orders/${encodeURIComponent(checkoutId)}/invoice.html`;
}

/** مسیر HTML رسید پرداخت. */
export function adminOrderReceiptUrl(checkoutId: string): string {
  return `/v1/admin/orders/${encodeURIComponent(checkoutId)}/receipt.html`;
}

/** باز کردن فاکتور/رسید HTML با هدر Actor توسعه (cookie session هم پاس می‌شود). */
export async function openAdminOrderHtmlDocument(
  url: string,
  unavailableCode: "order.invoice.unavailable" | "order.receipt.unavailable",
): Promise<AdminResult<true>> {
  try {
    const response = await fetch(url, {
      headers: {
        Accept: "text/html",
        [ADMIN_DEV_ACTOR_HEADER]: actorId(),
      },
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: mapAdminErrorMessage("order.operation.denied", "fa") };
    }
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      const code = parseAdminProblemErrorCode(payload, response.status) || unavailableCode;
      return {
        state: "error",
        data: null,
        status: response.status,
        message: mapAdminErrorMessage(code, "fa"),
      };
    }
    const html = await response.text();
    const blob = new Blob([html], { type: "text/html;charset=utf-8" });
    const objectUrl = URL.createObjectURL(blob);
    window.open(objectUrl, "_blank", "noopener,noreferrer");
    window.setTimeout(() => URL.revokeObjectURL(objectUrl), 60_000);
    return { state: "ok", data: true, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}
