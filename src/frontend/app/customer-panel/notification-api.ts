/**
 * API اعلان‌های تراکنشی — بدون دادهٔ جعلی؛ فقط Host.
 */

import { bffFetchHeaders } from "../../lib/auth/browser-session.ts";
import { CUSTOMER_DEV_ACTOR_HEADER, DEFAULT_CUSTOMER_DEV_ACTOR_ID } from "./customer-api";
import {
  ACTOR_STORAGE_KEY,
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  SELLER_PARTY_STORAGE_KEY,
} from "../vendor-panel/seller-api";

export type NotificationRecipient = "customer" | "seller";

export interface NotificationItem {
  id: string;
  type: string;
  category: string;
  title: string;
  body: string;
  targetRoute: string | null;
  isRead: boolean;
  createdAt: string;
  displayDate: string;
  displayTime: string;
}

function text(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function bool(value: unknown): boolean {
  return value === true;
}

function record(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function prop(obj: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (key in obj) return obj[key];
  }
  return undefined;
}

function actorHeaders(): HeadersInit {
  const headers: Record<string, string> = { ...bffFetchHeaders(), Accept: "application/json" };
  if (typeof window !== "undefined") {
    const stored = window.localStorage.getItem("tooba.customerActorUserId");
    headers[CUSTOMER_DEV_ACTOR_HEADER] = stored || DEFAULT_CUSTOMER_DEV_ACTOR_ID;
  }
  return headers;
}

function sellerHeaders(): HeadersInit {
  const headers: Record<string, string> = { ...bffFetchHeaders(), Accept: "application/json" };
  if (typeof window !== "undefined") {
    const actor = window.localStorage.getItem(ACTOR_STORAGE_KEY);
    const party = window.localStorage.getItem(SELLER_PARTY_STORAGE_KEY);
    if (actor) headers[DEV_ACTOR_HEADER] = actor;
    if (party) headers[SELLER_PARTY_HEADER] = party;
  }
  return headers;
}

function basePath(kind: NotificationRecipient): string {
  return kind === "seller" ? "/v1/seller/notifications" : "/v1/customer/notifications";
}

function headersFor(kind: NotificationRecipient): HeadersInit {
  return kind === "seller" ? sellerHeaders() : actorHeaders();
}

export function mapNotificationItem(value: unknown): NotificationItem | null {
  const item = record(value);
  if (!item) return null;
  const id = text(prop(item, "notificationId", "NotificationId", "id", "Id"));
  if (!id) return null;
  const createdAt = text(prop(item, "createdAt", "CreatedAt"));
  const created = createdAt ? new Date(createdAt) : null;
  return {
    id,
    type: text(prop(item, "type", "Type"), "order"),
    category: text(prop(item, "category", "Category"), text(prop(item, "type", "Type"), "order")),
    title: text(prop(item, "title", "Title"), "اعلان"),
    body: text(prop(item, "body", "Body"), ""),
    targetRoute: text(prop(item, "targetRoute", "TargetRoute")) || null,
    isRead: bool(prop(item, "isRead", "IsRead")),
    createdAt,
    displayDate: created ? created.toLocaleDateString("fa-IR") : "—",
    displayTime: created
      ? created.toLocaleTimeString("fa-IR", { hour: "2-digit", minute: "2-digit" })
      : "",
  };
}

export async function loadNotifications(kind: NotificationRecipient): Promise<NotificationItem[]> {
  const response = await fetch(`${basePath(kind)}?take=50&locale=fa`, {
    headers: headersFor(kind),
    cache: "no-store",
  });
  if (!response.ok) throw new Error(`notifications.list.${response.status}`);
  const payload = await response.json();
  const rows = Array.isArray(payload)
    ? payload
    : Array.isArray(payload?.items)
      ? payload.items
      : Array.isArray(payload?.Items)
        ? payload.Items
        : [];
  return rows.flatMap((row: unknown) => {
    const mapped = mapNotificationItem(row);
    return mapped ? [mapped] : [];
  });
}

export async function loadUnreadCount(kind: NotificationRecipient): Promise<number> {
  const response = await fetch(`${basePath(kind)}/unread-count`, {
    headers: headersFor(kind),
    cache: "no-store",
  });
  if (!response.ok) throw new Error(`notifications.unread.${response.status}`);
  const payload = await response.json();
  const n = payload?.unreadCount ?? payload?.UnreadCount ?? payload?.count ?? payload?.Count ?? 0;
  return typeof n === "number" ? n : Number(n) || 0;
}

export async function markNotificationRead(
  kind: NotificationRecipient,
  notificationId: string,
): Promise<void> {
  const response = await fetch(`${basePath(kind)}/${encodeURIComponent(notificationId)}/read`, {
    method: "POST",
    headers: headersFor(kind),
  });
  if (!response.ok) throw new Error(`notifications.read.${response.status}`);
}

export async function markAllNotificationsRead(kind: NotificationRecipient): Promise<void> {
  const response = await fetch(`${basePath(kind)}/read-all`, {
    method: "POST",
    headers: headersFor(kind),
  });
  if (!response.ok) throw new Error(`notifications.readAll.${response.status}`);
}

export async function dismissNotification(
  kind: NotificationRecipient,
  notificationId: string,
): Promise<void> {
  const response = await fetch(`${basePath(kind)}/${encodeURIComponent(notificationId)}`, {
    method: "DELETE",
    headers: headersFor(kind),
  });
  if (!response.ok && response.status !== 404) {
    throw new Error(`notifications.delete.${response.status}`);
  }
}
