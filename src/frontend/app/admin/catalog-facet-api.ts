/**
 * کلاینت Admin برای پیکربندی facet فیلتر PLP رده.
 */

import { adminHeaders, type AdminResult } from "./admin-api.ts";
import type { CatalogAttributeValueKind } from "./catalog-attribute-api.ts";

export type CatalogFacetDisplayType =
  | "CheckboxList"
  | "SearchableSelect"
  | "Range"
  | "ColorSwatch"
  | "BooleanToggle";

const DISPLAY_TYPE_BY_NUMBER: Record<number, CatalogFacetDisplayType> = {
  0: "CheckboxList",
  1: "SearchableSelect",
  2: "Range",
  3: "ColorSwatch",
  4: "BooleanToggle",
};

export interface EffectiveCategoryFacet {
  definitionId: string;
  code: string;
  localizedName: string;
  valueKind: CatalogAttributeValueKind;
  displayType: CatalogFacetDisplayType;
  sortOrder: number;
  isVisible: boolean;
  isSearchable: boolean;
  isCollapsedByDefault: boolean;
  showCounts: boolean;
  sourceCategoryId: string;
  isInherited: boolean;
}

export interface UpsertCategoryFacetInput {
  displayType: CatalogFacetDisplayType;
  sortOrder: number;
  isVisible: boolean;
  isSearchable: boolean;
  isCollapsedByDefault: boolean;
  showCounts: boolean;
}

export const FACET_DISPLAY_LABELS: Record<CatalogFacetDisplayType, string> = {
  CheckboxList: "چندانتخابی",
  SearchableSelect: "انتخاب با جستجو",
  Range: "بازه",
  ColorSwatch: "رنگ",
  BooleanToggle: "روشن/خاموش",
};

export function displayTypeLabel(type: CatalogFacetDisplayType): string {
  return FACET_DISPLAY_LABELS[type] ?? type;
}

export function suggestFacetDisplayType(valueKind: CatalogAttributeValueKind): CatalogFacetDisplayType {
  if (valueKind === "Boolean") return "BooleanToggle";
  if (valueKind === "Number") return "Range";
  if (valueKind === "Text") return "SearchableSelect";
  return "CheckboxList";
}

export function isSearchableDisplayType(type: CatalogFacetDisplayType): boolean {
  return type === "CheckboxList" || type === "SearchableSelect";
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

function errorMessage(payload: unknown, status: number): string {
  const item = recordOf(payload);
  if (item) {
    const title = text(prop(item, "title", "Title"));
    const code = text(prop(item, "errorCode", "ErrorCode"));
    if (title) return code ? `${title} (${code})` : title;
    if (code) return code;
  }
  return `admin.http.${status}`;
}

async function adminRead(path: string): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

async function adminWrite(
  path: string,
  method: string,
  body?: unknown,
): Promise<AdminResult<unknown>> {
  try {
    const response = await fetch(path, {
      method,
      headers: adminHeaders(body === undefined ? undefined : { "Content-Type": "application/json" }),
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: errorMessage(payload, response.status) };
    }
    return { state: "ok", data: payload, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

function mapDisplayType(raw: unknown): CatalogFacetDisplayType {
  if (typeof raw === "number") return DISPLAY_TYPE_BY_NUMBER[raw] ?? "CheckboxList";
  if (typeof raw === "string" && raw in FACET_DISPLAY_LABELS) return raw as CatalogFacetDisplayType;
  return "CheckboxList";
}

function mapEffectiveFacet(raw: Record<string, unknown>): EffectiveCategoryFacet {
  return {
    definitionId: String(prop(raw, "definitionId", "DefinitionId")),
    code: String(prop(raw, "code", "Code") ?? ""),
    localizedName: String(prop(raw, "localizedName", "LocalizedName") ?? prop(raw, "code", "Code") ?? ""),
    valueKind: String(prop(raw, "valueKind", "ValueKind") ?? "Text") as CatalogAttributeValueKind,
    displayType: mapDisplayType(prop(raw, "displayType", "DisplayType")),
    sortOrder: Number(prop(raw, "sortOrder", "SortOrder") ?? 0),
    isVisible: Boolean(prop(raw, "isVisible", "IsVisible") ?? true),
    isSearchable: Boolean(prop(raw, "isSearchable", "IsSearchable") ?? false),
    isCollapsedByDefault: Boolean(prop(raw, "isCollapsedByDefault", "IsCollapsedByDefault") ?? false),
    showCounts: Boolean(prop(raw, "showCounts", "ShowCounts") ?? false),
    sourceCategoryId: String(prop(raw, "sourceCategoryId", "SourceCategoryId") ?? ""),
    isInherited: Boolean(prop(raw, "isInherited", "IsInherited") ?? false),
  };
}

export async function loadEffectiveCategoryFacets(
  categoryId: string,
  locale = "fa-IR",
): Promise<AdminResult<EffectiveCategoryFacet[]>> {
  const response = await adminRead(
    `/v1/admin/catalog/categories/${categoryId}/facets/effective?locale=${encodeURIComponent(locale)}`,
  );
  if (response.state !== "ok" || !Array.isArray(response.data)) {
    return { ...response, data: null };
  }
  return {
    ...response,
    data: response.data.map((row) => mapEffectiveFacet(recordOf(row) ?? {})),
  };
}

export async function upsertCategoryFacet(
  categoryId: string,
  definitionId: string,
  input: UpsertCategoryFacetInput,
): Promise<AdminResult<null>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/facets/${definitionId}`,
    "PUT",
    input,
  );
  return { ...response, data: response.state === "ok" ? null : null };
}

export async function removeCategoryFacetOverride(
  categoryId: string,
  definitionId: string,
): Promise<AdminResult<null>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/facets/${definitionId}`,
    "DELETE",
  );
  return { ...response, data: response.state === "ok" ? null : null };
}

export async function reorderCategoryFacets(
  categoryId: string,
  orderedDefinitionIds: string[],
): Promise<AdminResult<null>> {
  const response = await adminWrite(
    `/v1/admin/catalog/categories/${categoryId}/facets/order`,
    "PUT",
    { orderedDefinitionIds },
  );
  return { ...response, data: response.state === "ok" ? null : null };
}

export function partitionEffectiveFacets(
  facets: EffectiveCategoryFacet[],
): { inherited: EffectiveCategoryFacet[]; local: EffectiveCategoryFacet[] } {
  const inherited: EffectiveCategoryFacet[] = [];
  const local: EffectiveCategoryFacet[] = [];
  for (const row of facets) {
    if (row.isInherited) inherited.push(row);
    else local.push(row);
  }
  return { inherited, local };
}

export const FACET_DISPLAY_TYPES: CatalogFacetDisplayType[] = [
  "CheckboxList",
  "SearchableSelect",
  "Range",
  "BooleanToggle",
];
