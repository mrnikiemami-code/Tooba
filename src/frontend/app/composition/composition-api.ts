/**
 * کلاینت عمومی/ادمین Page Composition — فقط دادهٔ زندهٔ Host.
 */

import { adminHeaders, type AdminResult } from "../admin/admin-api.ts";
import { storefrontHostOrigin } from "../storefront/storefront-api.ts";

export const DEFAULT_HOME_SECTION_ORDER = [
  "hero",
  "stories",
  "category_grid",
  "product_rail_flash",
  "best_sellers",
  "product_rail_most_viewed",
  "middle_banners",
  "brands",
  "newest_products",
  "customer_reviews",
  "latest_articles",
] as const;

export type HomeSectionType = (typeof DEFAULT_HOME_SECTION_ORDER)[number];

export interface HomeCompositionSectionItem {
  pageSectionId: string;
  sectionType: string;
  displayOrder: number;
  variant: string;
  configurationJson: string;
}

export interface HomeCompositionSnapshot {
  pageKey: string;
  tenantId: string;
  locale: string | null;
  versionToken: number;
  sections: HomeCompositionSectionItem[];
}

export interface AdminHomeCompositionSectionItem extends HomeCompositionSectionItem {
  isVisible: boolean;
}

export interface AdminHomeCompositionSnapshot {
  pageDefinitionId: string;
  pageKey: string;
  tenantId: string;
  locale: string | null;
  versionToken: number;
  updatedAt: string;
  sections: AdminHomeCompositionSectionItem[];
}

export interface SectionCatalogEntry {
  sectionType: string;
  allowedVariants: string[];
  supportedConfigKeys: string[];
}

export interface SectionCatalogSnapshot {
  sectionTypes: SectionCatalogEntry[];
  configSchemaMetadata: Record<string, string[]>;
}

export interface SectionDisplayConfig {
  title?: string;
  href?: string;
  itemCount?: number;
  sourceKind?: string;
}

export const SECTION_TYPE_LABELS: Record<string, string> = {
  hero: "اسلایدر Hero",
  stories: "استوری‌ها",
  category_grid: "دسته‌بندی‌ها",
  product_rail_flash: "پیشنهاد شگفت‌انگیز",
  best_sellers: "پرفروش‌ترین‌ها",
  product_rail_most_viewed: "پربازدیدترین‌ها",
  middle_banners: "بنرهای میانی",
  brands: "برندها",
  newest_products: "جدیدترین محصولات",
  customer_reviews: "نظرات مشتریان",
  latest_articles: "آخرین مقالات",
};

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function readJsonOrigin(path: string): string {
  return typeof window === "undefined" ? `${storefrontHostOrigin()}${path}` : path;
}

async function readJson(path: string): Promise<unknown | null> {
  try {
    const response = await fetch(readJsonOrigin(path), { cache: "no-store" });
    if (!response.ok) return null;
    return await response.json();
  } catch {
    return null;
  }
}

function mapHomeSection(value: unknown): HomeCompositionSectionItem | null {
  const item = recordOf(value);
  if (!item) return null;
  const pageSectionId = text(prop(item, "pageSectionId", "PageSectionId"));
  const sectionType = text(prop(item, "sectionType", "SectionType"));
  if (!pageSectionId || !sectionType) return null;
  return {
    pageSectionId,
    sectionType,
    displayOrder: Number(prop(item, "displayOrder", "DisplayOrder") ?? 0),
    variant: text(prop(item, "variant", "Variant"), "default"),
    configurationJson: text(prop(item, "configurationJson", "ConfigurationJson"), "{}"),
  };
}

/** snapshot عمومی Home Composition را نگاشت می‌کند. */
export function mapHomeComposition(value: unknown): HomeCompositionSnapshot | null {
  const root = recordOf(value);
  if (!root) return null;
  const sectionsRaw = prop(root, "sections", "Sections");
  const sections = Array.isArray(sectionsRaw)
    ? sectionsRaw.map(mapHomeSection).filter((row): row is HomeCompositionSectionItem => row !== null)
    : [];
  return {
    pageKey: text(prop(root, "pageKey", "PageKey"), "home"),
    tenantId: text(prop(root, "tenantId", "TenantId")),
    locale: (() => {
      const locale = prop(root, "locale", "Locale");
      return locale == null ? null : text(locale);
    })(),
    versionToken: Number(prop(root, "versionToken", "VersionToken") ?? 0),
    sections: sections.sort((left, right) => left.displayOrder - right.displayOrder),
  };
}

/** snapshot admin Home Composition را نگاشت می‌کند. */
export function mapAdminHomeComposition(value: unknown): AdminHomeCompositionSnapshot | null {
  const root = recordOf(value);
  if (!root) return null;
  const sectionsRaw = prop(root, "sections", "Sections");
  const sections = Array.isArray(sectionsRaw)
    ? sectionsRaw
        .map((row) => {
          const mapped = mapHomeSection(row);
          const item = recordOf(row);
          if (!mapped || !item) return null;
          return {
            ...mapped,
            isVisible: Boolean(prop(item, "isVisible", "IsVisible")),
          } satisfies AdminHomeCompositionSectionItem;
        })
        .filter((row): row is AdminHomeCompositionSectionItem => row !== null)
    : [];
  return {
    pageDefinitionId: text(prop(root, "pageDefinitionId", "PageDefinitionId")),
    pageKey: text(prop(root, "pageKey", "PageKey"), "home"),
    tenantId: text(prop(root, "tenantId", "TenantId")),
    locale: (() => {
      const locale = prop(root, "locale", "Locale");
      return locale == null ? null : text(locale);
    })(),
    versionToken: Number(prop(root, "versionToken", "VersionToken") ?? 0),
    updatedAt: text(prop(root, "updatedAt", "UpdatedAt")),
    sections: sections.sort((left, right) => left.displayOrder - right.displayOrder),
  };
}

/** catalog section types را نگاشت می‌کند. */
export function mapSectionCatalog(value: unknown): SectionCatalogSnapshot | null {
  const root = recordOf(value);
  if (!root) return null;
  const typesRaw = prop(root, "sectionTypes", "SectionTypes");
  const metadataRaw = prop(root, "configSchemaMetadata", "ConfigSchemaMetadata");
  const sectionTypes = Array.isArray(typesRaw)
    ? typesRaw
        .map((row) => {
          const item = recordOf(row);
          if (!item) return null;
          const sectionType = text(prop(item, "sectionType", "SectionType"));
          if (!sectionType) return null;
          const variantsRaw = prop(item, "allowedVariants", "AllowedVariants");
          const keysRaw = prop(item, "supportedConfigKeys", "SupportedConfigKeys");
          return {
            sectionType,
            allowedVariants: Array.isArray(variantsRaw) ? variantsRaw.map((entry) => String(entry)) : ["default"],
            supportedConfigKeys: Array.isArray(keysRaw) ? keysRaw.map((entry) => String(entry)) : [],
          } satisfies SectionCatalogEntry;
        })
        .filter((row): row is SectionCatalogEntry => row !== null)
    : [];
  const configSchemaMetadata: Record<string, string[]> = {};
  const metadata = recordOf(metadataRaw);
  if (metadata) {
    for (const [key, value] of Object.entries(metadata)) {
      configSchemaMetadata[key] = Array.isArray(value) ? value.map((entry) => String(entry)) : [];
    }
  }
  return { sectionTypes, configSchemaMetadata };
}

/** JSON config امن section را parse می‌کند. */
export function parseSectionDisplayConfig(configurationJson?: string): SectionDisplayConfig {
  if (!configurationJson || configurationJson.trim() === "" || configurationJson.trim() === "{}") {
    return {};
  }
  try {
    const parsed = JSON.parse(configurationJson) as Record<string, unknown>;
    const config: SectionDisplayConfig = {};
    if (typeof parsed.title === "string" && parsed.title.trim()) config.title = parsed.title.trim();
    if (typeof parsed.href === "string" && parsed.href.trim()) config.href = parsed.href.trim();
    if (typeof parsed.itemCount === "number" && Number.isFinite(parsed.itemCount)) config.itemCount = parsed.itemCount;
    if (typeof parsed.sourceKind === "string" && parsed.sourceKind.trim()) config.sourceKind = parsed.sourceKind.trim();
    return config;
  } catch {
    return {};
  }
}

/** ترتیب section پیش‌فرض را برای fallback runtime برمی‌گرداند. */
export function defaultHomeCompositionSections(): HomeCompositionSectionItem[] {
  return DEFAULT_HOME_SECTION_ORDER.map((sectionType, displayOrder) => ({
    pageSectionId: `default-${sectionType}`,
    sectionType,
    displayOrder,
    variant: "default",
    configurationJson: "{}",
  }));
}

/** ترکیب عمومی خانه را از Host می‌خواند. */
export async function loadHomeComposition(locale?: string | null): Promise<HomeCompositionSnapshot | null> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  return mapHomeComposition(await readJson(`/v1/storefront/home/composition${params}`));
}

async function adminJson<T>(
  path: string,
  init?: RequestInit,
): Promise<AdminResult<T>> {
  try {
    const response = await fetch(readJsonOrigin(path), {
      cache: "no-store",
      ...init,
      headers: adminHeaders(init?.headers as Record<string, string> | undefined),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status };
    }
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      const message = recordOf(payload)?.detail as string | undefined;
      return { state: "error", data: null, status: response.status, message: message ?? `admin.http.${response.status}` };
    }
    const payload = await response.json();
    return { state: "ok", data: payload as T, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** نمای admin Home Composition. */
export async function loadAdminHomeComposition(locale?: string | null): Promise<AdminResult<AdminHomeCompositionSnapshot>> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  const result = await adminJson<unknown>(`/v1/admin/page-composition/home${params}`);
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapAdminHomeComposition(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

/** catalog section types admin. */
export async function loadSectionCatalog(): Promise<AdminResult<SectionCatalogSnapshot>> {
  const result = await adminJson<unknown>("/v1/admin/page-composition/home/catalog");
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapSectionCatalog(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

/** sectionهای خانه را مرتب می‌کند. */
export async function reorderAdminHomeSections(
  sectionIds: string[],
  locale?: string | null,
): Promise<AdminResult<AdminHomeCompositionSnapshot>> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  const result = await adminJson<unknown>(`/v1/admin/page-composition/home/reorder${params}`, {
    method: "PUT",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ sectionIds }),
  });
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapAdminHomeComposition(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

/** section را به‌روزرسانی می‌کند. */
export async function updateAdminHomeSection(
  sectionId: string,
  input: { isVisible?: boolean; configurationJson?: string; variant?: string },
  locale?: string | null,
): Promise<AdminResult<AdminHomeCompositionSnapshot>> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  const result = await adminJson<unknown>(`/v1/admin/page-composition/home/sections/${sectionId}${params}`, {
    method: "PUT",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(input),
  });
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapAdminHomeComposition(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}

/** ترکیب پیش‌فرض خانه را بازمی‌گرداند. */
export async function restoreDefaultAdminHomeComposition(
  locale?: string | null,
): Promise<AdminResult<AdminHomeCompositionSnapshot>> {
  const params = locale ? `?locale=${encodeURIComponent(locale)}` : "";
  const result = await adminJson<unknown>(`/v1/admin/page-composition/home/restore-default${params}`, {
    method: "POST",
  });
  if (result.state !== "ok") return { ...result, data: null };
  const mapped = mapAdminHomeComposition(result.data);
  return mapped
    ? { state: "ok", data: mapped, status: result.status }
    : { state: "error", data: null, status: result.status, message: "invalid-response" };
}
