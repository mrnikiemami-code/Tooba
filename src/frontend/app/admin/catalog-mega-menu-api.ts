/**
 * کلاینت Admin برای پیکربندی مگامنو رده.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import { parseAdminProblemErrorCode } from "./admin-error-map.ts";

export interface CategoryMegaMenuConfiguration {
  categoryId: string;
  isBound: boolean;
  megaMenuItemId: string | null;
  parentMegaMenuItemId: string | null;
  parentMenuPath: string | null;
  sortOrder: number;
  isVisible: boolean;
  isFeatured: boolean;
  imageMediaAssetId: string | null;
  iconMediaAssetId: string | null;
  displayTitle: string;
  titleOverride: string | null;
  badgeText: string | null;
  shortLabel: string | null;
  destinationPreview: string;
  presentationLevel: number;
  categoryPublished: boolean;
  categoryVisible: boolean;
}

export interface MegaMenuPlacementOption {
  megaMenuItemId: string;
  categoryId: string;
  label: string;
  menuPath: string;
  level: number;
}

export interface UpsertCategoryMegaMenuInput {
  parentMegaMenuItemId: string | null;
  sortOrder: number;
  isVisible: boolean;
  isFeatured: boolean;
  imageMediaAssetId: string | null;
  iconMediaAssetId: string | null;
  titleOverride: string | null;
  badgeText: string | null;
  shortLabel: string | null;
}

export interface StorefrontMegaMenuItem {
  megaMenuItemId: string;
  parentMegaMenuItemId: string | null;
  categoryId: string;
  title: string;
  destination: string;
  isFeatured: boolean;
  iconMediaAssetId: string | null;
  imageMediaAssetId: string | null;
  sortOrder: number;
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function prop(item: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (key in item) return item[key];
  }
  return undefined;
}

function nullableText(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  const s = String(value);
  return s.length ? s : null;
}

function mapConfiguration(raw: Record<string, unknown>): CategoryMegaMenuConfiguration {
  return {
    categoryId: String(prop(raw, "categoryId", "CategoryId")),
    isBound: Boolean(prop(raw, "isBound", "IsBound")),
    megaMenuItemId: nullableText(prop(raw, "megaMenuItemId", "MegaMenuItemId")),
    parentMegaMenuItemId: nullableText(prop(raw, "parentMegaMenuItemId", "ParentMegaMenuItemId")),
    parentMenuPath: nullableText(prop(raw, "parentMenuPath", "ParentMenuPath")),
    sortOrder: Number(prop(raw, "sortOrder", "SortOrder") ?? 0),
    isVisible: Boolean(prop(raw, "isVisible", "IsVisible") ?? true),
    isFeatured: Boolean(prop(raw, "isFeatured", "IsFeatured") ?? false),
    imageMediaAssetId: nullableText(prop(raw, "imageMediaAssetId", "ImageMediaAssetId")),
    iconMediaAssetId: nullableText(prop(raw, "iconMediaAssetId", "IconMediaAssetId")),
    displayTitle: String(prop(raw, "displayTitle", "DisplayTitle") ?? "—"),
    titleOverride: nullableText(prop(raw, "titleOverride", "TitleOverride")),
    badgeText: nullableText(prop(raw, "badgeText", "BadgeText")),
    shortLabel: nullableText(prop(raw, "shortLabel", "ShortLabel")),
    destinationPreview: String(prop(raw, "destinationPreview", "DestinationPreview") ?? ""),
    presentationLevel: Number(prop(raw, "presentationLevel", "PresentationLevel") ?? 0),
    categoryPublished: Boolean(prop(raw, "categoryPublished", "CategoryPublished") ?? false),
    categoryVisible: Boolean(prop(raw, "categoryVisible", "CategoryVisible") ?? false),
  };
}

function mapPlacement(raw: Record<string, unknown>): MegaMenuPlacementOption {
  return {
    megaMenuItemId: String(prop(raw, "megaMenuItemId", "MegaMenuItemId")),
    categoryId: String(prop(raw, "categoryId", "CategoryId")),
    label: String(prop(raw, "label", "Label") ?? "—"),
    menuPath: String(prop(raw, "menuPath", "MenuPath") ?? "—"),
    level: Number(prop(raw, "level", "Level") ?? 0),
  };
}

export function mapStorefrontMegaMenuItem(raw: Record<string, unknown>): StorefrontMegaMenuItem {
  return {
    megaMenuItemId: String(prop(raw, "megaMenuItemId", "MegaMenuItemId")),
    parentMegaMenuItemId: nullableText(prop(raw, "parentMegaMenuItemId", "ParentMegaMenuItemId")),
    categoryId: String(prop(raw, "categoryId", "CategoryId")),
    title: String(prop(raw, "title", "Title") ?? "—"),
    destination: String(prop(raw, "destination", "Destination") ?? ""),
    isFeatured: Boolean(prop(raw, "isFeatured", "IsFeatured") ?? false),
    iconMediaAssetId: nullableText(prop(raw, "iconMediaAssetId", "IconMediaAssetId")),
    imageMediaAssetId: nullableText(prop(raw, "imageMediaAssetId", "ImageMediaAssetId")),
    sortOrder: Number(prop(raw, "sortOrder", "SortOrder") ?? 0),
  };
}

async function adminRead(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: parseAdminProblemErrorCode(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function adminWrite(path: string, method: string, body?: unknown): Promise<AdminResult<null>> {
  try {
    const response = await fetch(path, {
      method,
      headers: adminHeaders(body === undefined ? undefined : { "Content-Type": "application/json" }),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      return { state: "error", data: null, status: response.status, message: parseAdminProblemErrorCode(payload, response.status) };
    }
    return { state: "ok", data: null, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function loadCategoryMegaMenuConfiguration(
  categoryId: string,
  locale = "fa-IR",
): Promise<AdminResult<CategoryMegaMenuConfiguration>> {
  const response = await adminRead(
    `/v1/admin/catalog/categories/${categoryId}/mega-menu?locale=${encodeURIComponent(locale)}`,
  );
  const row = recordOf(response.data);
  if (response.state !== "ok" || !row) {
    return { ...response, data: null };
  }
  return { ...response, data: mapConfiguration(row) };
}

export async function loadMegaMenuPlacementOptions(
  categoryId: string,
  locale = "fa-IR",
): Promise<AdminResult<MegaMenuPlacementOption[]>> {
  const response = await adminRead(
    `/v1/admin/catalog/categories/${categoryId}/mega-menu/placement-options?locale=${encodeURIComponent(locale)}`,
  );
  if (response.state !== "ok" || !Array.isArray(response.data)) {
    return { ...response, data: null };
  }
  return { ...response, data: response.data.map((row) => mapPlacement(recordOf(row) ?? {})) };
}

export async function upsertCategoryMegaMenu(
  categoryId: string,
  input: UpsertCategoryMegaMenuInput,
  locale = "fa-IR",
): Promise<AdminResult<null>> {
  return adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/mega-menu?locale=${encodeURIComponent(locale)}`,
    "PUT",
    input,
  );
}

export async function removeCategoryMegaMenuBinding(categoryId: string): Promise<AdminResult<null>> {
  return adminWrite(`/v1/admin/catalog/categories/${categoryId}/mega-menu`, "DELETE");
}

export async function loadStorefrontMegaMenu(locale = "fa-IR"): Promise<StorefrontMegaMenuItem[]> {
  try {
    const response = await fetch(`/v1/storefront/mega-menu?locale=${encodeURIComponent(locale)}`, { cache: "no-store" });
    if (!response.ok) return [];
    const payload = await response.json();
    if (!Array.isArray(payload)) return [];
    return payload.map((row) => mapStorefrontMegaMenuItem(recordOf(row) ?? {}));
  } catch {
    return [];
  }
}

export function presentationLevelLabel(level: number): string {
  if (level <= 1) return "سطح اول";
  if (level === 2) return "سطح دوم";
  return "سطح سوم";
}
