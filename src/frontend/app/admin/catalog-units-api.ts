/**
 * Admin client for Unit of Measure — translations keyed by LanguageId.
 */
import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "./admin-api.ts";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

export interface UnitTranslationWrite {
  languageId: string;
  name: string;
  shortName: string;
}

export interface UnitOfMeasureListItem {
  id: string;
  unitOfMeasureId: string;
  code: string;
  dimension: string;
  name: string;
  shortName: string;
  isActive: boolean;
  sortOrder: number;
  isReferenced: boolean;
}

export interface UnitOfMeasureDetail {
  unitOfMeasureId: string;
  code: string;
  dimension: string;
  isActive: boolean;
  sortOrder: number;
  isReferenced: boolean;
  translations: UnitTranslationWrite[];
}

export interface UnitOfMeasureWrite {
  code: string;
  dimension: string;
  isActive: boolean;
  sortOrder: number;
  translations: UnitTranslationWrite[];
}

function asString(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : value == null ? fallback : String(value);
}

function asBool(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function asNumber(value: unknown, fallback = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function readProp(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

export function mapUnitListItem(payload: unknown): UnitOfMeasureListItem | null {
  if (!payload || typeof payload !== "object") return null;
  const item = payload as Record<string, unknown>;
  const unitOfMeasureId = asString(readProp(item, "unitOfMeasureId", "UnitOfMeasureId"));
  if (!unitOfMeasureId) return null;
  return {
    id: unitOfMeasureId,
    unitOfMeasureId,
    code: asString(readProp(item, "code", "Code")),
    dimension: asString(readProp(item, "dimension", "Dimension")),
    name: asString(readProp(item, "name", "Name")),
    shortName: asString(readProp(item, "shortName", "ShortName")),
    isActive: asBool(readProp(item, "isActive", "IsActive"), true),
    sortOrder: asNumber(readProp(item, "sortOrder", "SortOrder")),
    isReferenced: asBool(readProp(item, "isReferenced", "IsReferenced")),
  };
}

export function mapUnitDetail(payload: unknown): UnitOfMeasureDetail | null {
  if (!payload || typeof payload !== "object") return null;
  const item = payload as Record<string, unknown>;
  const unitOfMeasureId = asString(readProp(item, "unitOfMeasureId", "UnitOfMeasureId"));
  if (!unitOfMeasureId) return null;
  const translationsRaw = readProp(item, "translations", "Translations");
  const translations = Array.isArray(translationsRaw)
    ? translationsRaw
        .filter((row): row is Record<string, unknown> => !!row && typeof row === "object")
        .map((row) => ({
          languageId: asString(readProp(row, "languageId", "LanguageId")),
          name: asString(readProp(row, "name", "Name")),
          shortName: asString(readProp(row, "shortName", "ShortName")),
        }))
        .filter((row) => row.languageId.length > 0)
    : [];
  return {
    unitOfMeasureId,
    code: asString(readProp(item, "code", "Code")),
    dimension: asString(readProp(item, "dimension", "Dimension")),
    isActive: asBool(readProp(item, "isActive", "IsActive"), true),
    sortOrder: asNumber(readProp(item, "sortOrder", "SortOrder")),
    isReferenced: asBool(readProp(item, "isReferenced", "IsReferenced")),
    translations,
  };
}

export async function loadAdminUnits(language?: string): Promise<AdminResult<UnitOfMeasureListItem[]>> {
  try {
    const query = language ? `?language=${encodeURIComponent(language)}` : "";
    const response = await fetch(`/v1/admin/catalog/units/${query}`, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.units.load_failed" };
    }
    const rows = Array.isArray(payload) ? payload : [];
    return {
      state: "ok",
      data: rows.map(mapUnitListItem).filter((row): row is UnitOfMeasureListItem => row != null),
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function loadAdminUnit(unitId: string): Promise<AdminResult<UnitOfMeasureDetail>> {
  try {
    const response = await fetch(`/v1/admin/catalog/units/${encodeURIComponent(unitId)}`, {
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.units.load_failed" };
    }
    const mapped = mapUnitDetail(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "admin.units.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function writeAdminUnit(
  unitId: string | null,
  body: UnitOfMeasureWrite,
): Promise<AdminResult<{ unitOfMeasureId: string }>> {
  try {
    const response = await fetch(
      unitId ? `/v1/admin/catalog/units/${encodeURIComponent(unitId)}` : "/v1/admin/catalog/units/",
      {
        method: unitId ? "PUT" : "POST",
        headers: adminHeaders({ "Content-Type": "application/json" }),
        body: JSON.stringify(body),
      },
    );
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const code = payload && typeof payload === "object"
        ? String((payload as Record<string, unknown>).errorCode ?? "admin.units.save_failed")
        : "admin.units.save_failed";
      return { state: "error", data: null, status: response.status, message: code };
    }
    const id = payload && typeof payload === "object"
      ? String((payload as Record<string, unknown>).unitOfMeasureId ?? (payload as Record<string, unknown>).UnitOfMeasureId ?? "")
      : "";
    return { state: "ok", data: { unitOfMeasureId: id || unitId || "" }, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function deactivateAdminUnit(unitId: string): Promise<AdminResult<{ unitOfMeasureId: string }>> {
  try {
    const response = await fetch(`/v1/admin/catalog/units/${encodeURIComponent(unitId)}/deactivate`, {
      method: "POST",
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "admin.units.deactivate_failed" };
    }
    const id = payload && typeof payload === "object"
      ? String((payload as Record<string, unknown>).unitOfMeasureId ?? unitId)
      : unitId;
    return { state: "ok", data: { unitOfMeasureId: id }, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
