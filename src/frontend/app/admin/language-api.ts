/**
 * Admin client for canonical Language/Locale registry.
 */
import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "./admin-api.ts";
import { mapSupportedLocale, type SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

export async function loadAdminLanguages(): Promise<AdminResult<SupportedLocaleDefinition[]>> {
  try {
    const response = await fetch("/v1/admin/languages", { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.languages.load_failed" };
    }
    const rows = Array.isArray(payload) ? payload : [];
    return {
      state: "ok",
      data: rows.map(mapSupportedLocale).filter((row): row is SupportedLocaleDefinition => row != null),
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function patchAdminLanguage(
  code: string,
  patch: Partial<Pick<SupportedLocaleDefinition, "active" | "default" | "sortOrder">>,
): Promise<AdminResult<SupportedLocaleDefinition>> {
  try {
    const response = await fetch(`/v1/admin/languages/${encodeURIComponent(code)}`, {
      method: "PATCH",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({
        active: patch.active,
        isDefault: patch.default,
        sortOrder: patch.sortOrder,
      }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.languages.patch_failed" };
    }
    const mapped = mapSupportedLocale(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "admin.languages.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
