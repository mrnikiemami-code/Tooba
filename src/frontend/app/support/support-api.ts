/**
 * کلاینت تیکت پشتیبانی — Customer BFF / Seller+Admin Host.
 * قرارداد Host: /v1/{audience}/support/tickets
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import { customerAuthHeaders } from "../customer-panel/customer-api.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  type HostReadSource,
} from "../vendor-panel/seller-api.ts";

export type TicketStatus =
  | "Open"
  | "InProgress"
  | "WaitingForCustomer"
  | "WaitingForSeller"
  | "Resolved"
  | "Closed";

export type TicketPriority = "Low" | "Normal" | "High";

export type TicketCategory = "Order" | "Payment" | "Return" | "Product" | "Other";

export type TicketRequesterKind = "Customer" | "Seller" | "Admin";

export type TicketAuthorKind = "Customer" | "Seller" | "Admin" | "System";

export interface TicketMessage {
  messageId: string;
  ticketId: string;
  authorKind: TicketAuthorKind;
  authorActorUserId: string;
  body: string;
  createdAt: string;
  isInternalNote: boolean;
}

export interface TicketSnapshot {
  ticketId: string;
  requesterKind: TicketRequesterKind;
  requesterActorUserId: string;
  requesterPartyId: string | null;
  sellerPartyId: string | null;
  subject: string;
  category: string;
  priority: string;
  status: string;
  assignedOperatorActorUserId: string | null;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
  lastMessageAt: string | null;
  messageCount: number;
  messages: TicketMessage[];
}

export interface TicketListRow {
  id: string;
  ticketId: string;
  subject: string;
  category: string;
  priority: string;
  status: string;
  requesterKind: string;
  messageCount: number;
  createdAt: string;
  lastMessageAt: string | null;
}

export interface TicketListPage {
  items: TicketListRow[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateTicketInput {
  subject: string;
  category: TicketCategory | string;
  priority?: TicketPriority | string;
  body: string;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  idempotencyKey?: string;
}

export interface ReplyTicketInput {
  body: string;
  isInternalNote?: boolean;
  idempotencyKey?: string;
}

export interface AdminTicketPatch {
  status?: string | null;
  priority?: string | null;
  assignedOperatorActorUserId?: string | null;
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

function mapMessage(value: unknown): TicketMessage | null {
  const item = record(value);
  if (!item) return null;
  const messageId = text(prop(item, "messageId", "MessageId"));
  const ticketId = text(prop(item, "ticketId", "TicketId"));
  if (!messageId || !ticketId) return null;
  return {
    messageId,
    ticketId,
    authorKind: text(prop(item, "authorKind", "AuthorKind"), "Customer") as TicketAuthorKind,
    authorActorUserId: text(prop(item, "authorActorUserId", "AuthorActorUserId")),
    body: text(prop(item, "body", "Body")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    isInternalNote: Boolean(prop(item, "isInternalNote", "IsInternalNote")),
  };
}

/** نگاشت snapshot تیکت از JSON Host. */
export function mapTicketSnapshot(value: unknown): TicketSnapshot | null {
  const item = record(value);
  if (!item) return null;
  const ticketId = text(prop(item, "ticketId", "TicketId"));
  if (!ticketId) return null;
  const messagesRaw = prop(item, "messages", "Messages");
  const messages = Array.isArray(messagesRaw)
    ? messagesRaw.map(mapMessage).filter((row): row is TicketMessage => row !== null)
    : [];
  const messageCount = number(prop(item, "messageCount", "MessageCount")) || messages.length;
  return {
    ticketId,
    requesterKind: text(prop(item, "requesterKind", "RequesterKind"), "Customer") as TicketRequesterKind,
    requesterActorUserId: text(prop(item, "requesterActorUserId", "RequesterActorUserId")),
    requesterPartyId: nullableText(prop(item, "requesterPartyId", "RequesterPartyId")),
    sellerPartyId: nullableText(prop(item, "sellerPartyId", "SellerPartyId")),
    subject: text(prop(item, "subject", "Subject")),
    category: text(prop(item, "category", "Category")),
    priority: text(prop(item, "priority", "Priority"), "Normal"),
    status: text(prop(item, "status", "Status"), "Open"),
    assignedOperatorActorUserId: nullableText(
      prop(item, "assignedOperatorActorUserId", "AssignedOperatorActorUserId"),
    ),
    relatedEntityType: nullableText(prop(item, "relatedEntityType", "RelatedEntityType")),
    relatedEntityId: nullableText(prop(item, "relatedEntityId", "RelatedEntityId")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    closedAt: nullableText(prop(item, "closedAt", "ClosedAt")),
    lastMessageAt: nullableText(prop(item, "lastMessageAt", "LastMessageAt")),
    messageCount,
    messages,
  };
}

function mapListRow(value: unknown): TicketListRow | null {
  const snapshot = mapTicketSnapshot(value);
  if (snapshot) {
    return {
      id: snapshot.ticketId,
      ticketId: snapshot.ticketId,
      subject: snapshot.subject,
      category: snapshot.category,
      priority: snapshot.priority,
      status: snapshot.status,
      requesterKind: snapshot.requesterKind,
      messageCount: snapshot.messageCount,
      createdAt: snapshot.createdAt,
      lastMessageAt: snapshot.lastMessageAt,
    };
  }
  const item = record(value);
  if (!item) return null;
  const ticketId = text(prop(item, "ticketId", "TicketId"));
  if (!ticketId) return null;
  return {
    id: ticketId,
    ticketId,
    subject: text(prop(item, "subject", "Subject")),
    category: text(prop(item, "category", "Category")),
    priority: text(prop(item, "priority", "Priority"), "Normal"),
    status: text(prop(item, "status", "Status"), "Open"),
    requesterKind: text(prop(item, "requesterKind", "RequesterKind")),
    messageCount: number(prop(item, "messageCount", "MessageCount")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    lastMessageAt: nullableText(prop(item, "lastMessageAt", "LastMessageAt")),
  };
}

/** فهرست تیکت‌ها (آرایه یا صفحهٔ صفحه‌بندی‌شده). */
export function mapTicketList(value: unknown): TicketListPage {
  if (Array.isArray(value)) {
    const items = value.map(mapListRow).filter((row): row is TicketListRow => row !== null);
    return { items, total: items.length, page: 1, pageSize: items.length || 20 };
  }
  const page = record(value);
  if (!page) return { items: [], total: 0, page: 1, pageSize: 20 };
  const rawItems = prop(page, "items", "Items") ?? prop(page, "tickets", "Tickets");
  const items = Array.isArray(rawItems)
    ? rawItems.map(mapListRow).filter((row): row is TicketListRow => row !== null)
    : [];
  return {
    items,
    total: number(prop(page, "total", "Total")) || items.length,
    page: number(prop(page, "page", "Page")) || 1,
    pageSize: number(prop(page, "pageSize", "PageSize")) || 20,
  };
}

export const TICKET_STATUS_LABELS: Record<string, string> = {
  Open: "باز",
  InProgress: "در حال بررسی",
  WaitingForCustomer: "در انتظار مشتری",
  WaitingForSeller: "در انتظار فروشنده",
  Resolved: "حل‌شده",
  Closed: "بسته‌شده",
};

export const TICKET_PRIORITY_LABELS: Record<string, string> = {
  Low: "پایین",
  Normal: "متوسط",
  High: "بالا",
};

export const TICKET_CATEGORY_LABELS: Record<string, string> = {
  Order: "سفارش",
  Payment: "پرداخت",
  Return: "مرجوعی",
  Product: "محصول",
  Other: "سایر",
};

export const TICKET_CATEGORIES = [
  { value: "Order", label: TICKET_CATEGORY_LABELS.Order },
  { value: "Payment", label: TICKET_CATEGORY_LABELS.Payment },
  { value: "Return", label: TICKET_CATEGORY_LABELS.Return },
  { value: "Product", label: TICKET_CATEGORY_LABELS.Product },
  { value: "Other", label: TICKET_CATEGORY_LABELS.Other },
] as const;

export const TICKET_PRIORITIES = [
  { value: "Low", label: TICKET_PRIORITY_LABELS.Low },
  { value: "Normal", label: TICKET_PRIORITY_LABELS.Normal },
  { value: "High", label: TICKET_PRIORITY_LABELS.High },
] as const;

/** وضعیت تیکت را فارسی می‌کند. */
export function formatTicketStatus(status: string): string {
  return TICKET_STATUS_LABELS[status] ?? (status || "نامشخص");
}

/** اولویت را فارسی می‌کند. */
export function formatTicketPriority(priority: string): string {
  return TICKET_PRIORITY_LABELS[priority] ?? (priority || "نامشخص");
}

/** دسته را فارسی می‌کند. */
export function formatTicketCategory(category: string): string {
  return TICKET_CATEGORY_LABELS[category] ?? (category || "نامشخص");
}

/** تاریخ ISO برای نمایش فارسی. */
export function formatTicketDate(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      }).format(date);
}

/** فقط تاریخ کوتاه. */
export function formatTicketDateShort(value: string | null | undefined): string {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : new Intl.DateTimeFormat("fa-IR", { year: "numeric", month: "2-digit", day: "2-digit" }).format(date);
}

export function toPersianDigits(value: string | number): string {
  return String(value).replace(/\d/g, (d) => "۰۱۲۳۴۵۶۷۸۹"[Number(d)] ?? d);
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
  const actor =
    typeof window !== "undefined" ? (window.localStorage.getItem("tooba.adminActorUserId") ?? "") : "";
  const headers: Record<string, string> = { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actor };
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

function errorCodeFrom(payload: unknown, fallback: string): string {
  const item = record(payload);
  return text(prop(item ?? {}, "errorCode", "ErrorCode"), fallback);
}

function buildQuery(params: Record<string, string | number | undefined | null>): string {
  const qs = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    qs.set(key, String(value));
  }
  const raw = qs.toString();
  return raw ? `?${raw}` : "";
}

/** فهرست تیکت مشتری. */
export async function loadCustomerTickets(opts?: {
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<TicketListPage | null> {
  try {
    const query = buildQuery({ status: opts?.status, page: opts?.page, pageSize: opts?.pageSize });
    const response = await fetch(`/api/customer/support/tickets${query}`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (response.status === 404) return { items: [], total: 0, page: 1, pageSize: 20 };
    if (!response.ok) return null;
    return mapTicketList(await response.json());
  } catch {
    return null;
  }
}

/** جزئیات تیکت مشتری. */
export async function loadCustomerTicketDetail(ticketId: string): Promise<TicketSnapshot | null> {
  try {
    const response = await fetch(`/api/customer/support/tickets/${encodeURIComponent(ticketId)}`, {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (!response.ok) return null;
    return mapTicketSnapshot(await response.json());
  } catch {
    return null;
  }
}

/** ایجاد تیکت مشتری. */
export async function createCustomerTicket(
  input: CreateTicketInput,
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch("/api/customer/support/tickets", {
      method: "POST",
      credentials: "include",
      headers: {
        ...customerAuthHeaders(true),
        ...(input.idempotencyKey ? { "Idempotency-Key": input.idempotencyKey } : {}),
      },
      body: JSON.stringify({
        subject: input.subject,
        category: input.category,
        priority: input.priority ?? "Normal",
        body: input.body,
        relatedEntityType: input.relatedEntityType ?? null,
        relatedEntityId: input.relatedEntityId ?? null,
      }),
    });
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** پاسخ مشتری. */
export async function replyCustomerTicket(
  ticketId: string,
  input: ReplyTicketInput,
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch(`/api/customer/support/tickets/${encodeURIComponent(ticketId)}/replies`, {
      method: "POST",
      credentials: "include",
      headers: {
        ...customerAuthHeaders(true),
        ...(input.idempotencyKey ? { "Idempotency-Key": input.idempotencyKey } : {}),
      },
      body: JSON.stringify({ body: input.body }),
    });
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.reply.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

async function customerTicketAction(
  ticketId: string,
  action: "close" | "reopen",
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string }> {
  try {
    const response = await fetch(`/api/customer/support/tickets/${encodeURIComponent(ticketId)}/${action}`, {
      method: "POST",
      credentials: "include",
      headers: customerAuthHeaders(true),
    });
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.action.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export function closeCustomerTicket(ticketId: string) {
  return customerTicketAction(ticketId, "close");
}

export function reopenCustomerTicket(ticketId: string) {
  return customerTicketAction(ticketId, "reopen");
}

/** فهرست تیکت فروشنده. */
export async function loadSellerTickets(
  sellerPartyId: string,
  opts?: { status?: string; page?: number; pageSize?: number },
): Promise<{ source: HostReadSource; page: TicketListPage; message?: string; denied?: boolean }> {
  try {
    const query = buildQuery({ status: opts?.status, page: opts?.page, pageSize: opts?.pageSize });
    const response = await fetch(`/v1/seller/support/tickets${query}`, { headers: sellerHeaders(sellerPartyId) });
    if (response.status === 401 || response.status === 403) {
      return {
        source: "error",
        page: { items: [], total: 0, page: 1, pageSize: 20 },
        message: "seller.authorization.denied",
        denied: true,
      };
    }
    if (!response.ok) {
      return {
        source: "error",
        page: { items: [], total: 0, page: 1, pageSize: 20 },
        message: `seller-support-http-${response.status}`,
      };
    }
    return { source: "host", page: mapTicketList(await response.json()) };
  } catch {
    return {
      source: "error",
      page: { items: [], total: 0, page: 1, pageSize: 20 },
      message: "host-unreachable",
    };
  }
}

/** جزئیات تیکت فروشنده. */
export async function loadSellerTicketDetail(
  sellerPartyId: string,
  ticketId: string,
): Promise<{ source: HostReadSource; snapshot: TicketSnapshot | null; message?: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/support/tickets/${encodeURIComponent(ticketId)}`, {
      headers: sellerHeaders(sellerPartyId),
    });
    if (response.status === 401 || response.status === 403 || response.status === 404) {
      return { source: "error", snapshot: null, message: "support.missing", denied: true };
    }
    if (!response.ok) {
      return { source: "error", snapshot: null, message: `seller-support-http-${response.status}` };
    }
    return { source: "host", snapshot: mapTicketSnapshot(await response.json()) };
  } catch {
    return { source: "error", snapshot: null, message: "host-unreachable" };
  }
}

/** ایجاد تیکت فروشنده. */
export async function createSellerTicket(
  sellerPartyId: string,
  input: CreateTicketInput,
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch("/v1/seller/support/tickets", {
      method: "POST",
      headers: {
        ...sellerHeaders(sellerPartyId, true),
        ...(input.idempotencyKey ? { "Idempotency-Key": input.idempotencyKey } : {}),
      },
      body: JSON.stringify({
        subject: input.subject,
        category: input.category,
        priority: input.priority ?? "Normal",
        body: input.body,
        relatedEntityType: input.relatedEntityType ?? null,
        relatedEntityId: input.relatedEntityId ?? null,
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

/** پاسخ فروشنده. */
export async function replySellerTicket(
  sellerPartyId: string,
  ticketId: string,
  input: ReplyTicketInput,
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/support/tickets/${encodeURIComponent(ticketId)}/replies`, {
      method: "POST",
      headers: {
        ...sellerHeaders(sellerPartyId, true),
        ...(input.idempotencyKey ? { "Idempotency-Key": input.idempotencyKey } : {}),
      },
      body: JSON.stringify({ body: input.body }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.reply.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

async function sellerTicketAction(
  sellerPartyId: string,
  ticketId: string,
  action: "close" | "reopen",
): Promise<{ ok: true; snapshot: TicketSnapshot } | { ok: false; errorCode: string; denied?: boolean }> {
  try {
    const response = await fetch(`/v1/seller/support/tickets/${encodeURIComponent(ticketId)}/${action}`, {
      method: "POST",
      headers: sellerHeaders(sellerPartyId, true),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, errorCode: "seller.authorization.denied", denied: true };
    }
    if (!response.ok) {
      return { ok: false, errorCode: errorCodeFrom(await response.json().catch(() => null), "support.action.rejected") };
    }
    const snapshot = mapTicketSnapshot(await response.json());
    if (!snapshot) return { ok: false, errorCode: "support.invalid-response" };
    return { ok: true, snapshot };
  } catch {
    return { ok: false, errorCode: "host-unreachable" };
  }
}

export function closeSellerTicket(sellerPartyId: string, ticketId: string) {
  return sellerTicketAction(sellerPartyId, ticketId, "close");
}

export function reopenSellerTicket(sellerPartyId: string, ticketId: string) {
  return sellerTicketAction(sellerPartyId, ticketId, "reopen");
}

/** فهرست تیکت Admin. */
export async function loadAdminTickets(opts?: {
  status?: string;
  requesterKind?: string;
  category?: string;
  priority?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}): Promise<AdminResult<TicketListPage>> {
  try {
    const query = buildQuery({
      status: opts?.status,
      requesterKind: opts?.requesterKind,
      category: opts?.category,
      priority: opts?.priority,
      q: opts?.q,
      page: opts?.page,
      pageSize: opts?.pageSize,
    });
    const response = await fetch(`/v1/admin/support/tickets${query}`, { headers: adminActorHeader() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: mapTicketList(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** جزئیات تیکت Admin (شامل یادداشت داخلی). */
export async function loadAdminTicketDetail(ticketId: string): Promise<AdminResult<TicketSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/support/tickets/${encodeURIComponent(ticketId)}`, {
      headers: adminActorHeader(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) return { state: "ok", data: null, status: 404 };
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: mapTicketSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** پاسخ Admin (عمومی یا یادداشت داخلی). */
export async function replyAdminTicket(
  ticketId: string,
  input: ReplyTicketInput,
): Promise<AdminResult<TicketSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/support/tickets/${encodeURIComponent(ticketId)}/replies`, {
      method: "POST",
      headers: {
        ...adminActorHeader(true),
        ...(input.idempotencyKey ? { "Idempotency-Key": input.idempotencyKey } : {}),
      },
      body: JSON.stringify({
        body: input.body,
        isInternalNote: Boolean(input.isInternalNote),
      }),
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
        message: errorCodeFrom(payload, "support.reply.rejected"),
      };
    }
    return { state: "ok", data: mapTicketSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** به‌روزرسانی وضعیت/اولویت/ارجاع Admin. */
export async function patchAdminTicket(
  ticketId: string,
  patch: AdminTicketPatch,
): Promise<AdminResult<TicketSnapshot>> {
  try {
    const response = await fetch(`/v1/admin/support/tickets/${encodeURIComponent(ticketId)}`, {
      method: "PATCH",
      headers: adminActorHeader(true),
      body: JSON.stringify(patch),
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
        message: errorCodeFrom(payload, "support.patch.rejected"),
      };
    }
    return { state: "ok", data: mapTicketSnapshot(payload), status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
