/**
 * پروفایل و ترجیحات اپراتور Admin — `/v1/admin/operator/*`.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "./admin-api.ts";
import { parseLocale, type Locale } from "../../lib/i18n/locale.ts";
import { writeBrowserLocaleCookie } from "../../lib/i18n/locale-cookie.ts";

export interface OperatorProfile {
  actorUserId: string;
  displayName: string;
  firstName: string;
  lastName: string;
  bio: string;
  editable: boolean;
}

export interface OperatorPreferences {
  locale: Locale;
}

export interface OperatorProfileWriteInput {
  displayName: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
}

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

/** نگاشت پروفایل اپراتور از Host. */
export function mapOperatorProfile(payload: unknown): OperatorProfile | null {
  const item = recordOf(payload);
  if (!item) return null;
  const displayName = text(prop(item, "displayName", "DisplayName"));
  if (!displayName.trim()) return null;
  // Host OperatorProfileEndpoints omits actorUserId (own-only); fall back to Dev actor storage.
  const actorUserId = text(prop(item, "actorUserId", "ActorUserId")) || actorId();
  const editableRaw = prop(item, "editable", "Editable");
  return {
    actorUserId,
    displayName,
    firstName: text(prop(item, "firstName", "FirstName")),
    lastName: text(prop(item, "lastName", "LastName")),
    bio: text(prop(item, "bio", "Bio")),
    editable: typeof editableRaw === "boolean" ? editableRaw : true,
  };
}

/** نگاشت ترجیح locale اپراتور. */
export function mapOperatorPreferences(payload: unknown): OperatorPreferences | null {
  const item = recordOf(payload);
  if (!item) return null;
  const raw = prop(item, "locale", "Locale");
  if (raw == null || String(raw).trim() === "") return null;
  return { locale: parseLocale(String(raw)) };
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

/** پروفایل اپراتور جاری را می‌خواند. */
export async function loadOperatorProfile(): Promise<AdminResult<OperatorProfile>> {
  const response = await read("/v1/admin/operator/profile");
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapOperatorProfile(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** ترجیح locale اپراتور را می‌خواند. */
export async function loadOperatorPreferences(): Promise<AdminResult<OperatorPreferences>> {
  const response = await read("/v1/admin/operator/preferences");
  if (response.state !== "ok") return { ...response, data: null };
  const data = mapOperatorPreferences(response.data);
  return data
    ? { ...response, data }
    : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
}

/** پروفایل اپراتور را ذخیره می‌کند. */
export async function saveOperatorProfile(
  input: OperatorProfileWriteInput,
): Promise<AdminResult<OperatorProfile>> {
  try {
    const body: Record<string, string> = { displayName: input.displayName.trim() };
    if (input.firstName?.trim()) body.firstName = input.firstName.trim();
    if (input.lastName?.trim()) body.lastName = input.lastName.trim();
    if (input.bio?.trim()) body.bio = input.bio.trim();
    else if (input.bio === "") body.bio = "";

    const response = await fetch("/v1/admin/operator/profile", {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const data = mapOperatorProfile(payload);
    return data
      ? { state: "ok", data, status: response.status }
      : { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** ترجیح locale اپراتور را ذخیره و کوکی را هم‌تراز می‌کند. */
export async function saveOperatorPreferences(locale: Locale): Promise<AdminResult<OperatorPreferences>> {
  try {
    const response = await fetch("/v1/admin/operator/preferences", {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ locale }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const data = mapOperatorPreferences(payload) ?? { locale: parseLocale(locale) };
    writeBrowserLocaleCookie(data.locale);
    return { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
