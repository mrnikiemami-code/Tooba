import { adminHeaders } from "../admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "../admin-error-map.ts";
import type { StorePageRevalidatePayload } from "../../storefront/storefront-store-page-cache.ts";

export type StorePageType = "Home" | "Landing";

export type AdminLandingPage = {
  pageId: string;
  pageType: StorePageType;
  locale: string;
  slug: string;
  title: string;
  seoTitle: string | null;
  seoDescription: string | null;
  robotsIndex: boolean;
  robotsFollow: boolean;
  canonicalUrl: string | null;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageUrl: string | null;
  primaryH1: string | null;
  status: "Draft" | "Published";
  updatedAt: string;
};

export type AdminLandingSection = {
  pageSectionId: string;
  pageId: string;
  sectionType: string;
  sortOrder: number;
  isEnabled: boolean;
  config: string;
  updatedAt: string;
};

export type AdminLandingHomeSelection = {
  homePageId: string | null;
  usesCanonicalHome: boolean;
};

export type AdminLandingPageWriteInput = {
  title: string;
  slug: string;
  locale: string;
  pageType?: StorePageType;
  seoTitle?: string;
  seoDescription?: string;
  robotsIndex?: boolean;
  robotsFollow?: boolean;
  canonicalUrl?: string;
  ogTitle?: string;
  ogDescription?: string;
  ogImageUrl?: string;
  primaryH1?: string;
};

export type AdminResult<T> = { ok: true; data: T } | { ok: false; message: string; denied?: boolean };

function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) return value;
  }
  return "";
}

function readBool(record: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") return value;
  }
  return undefined;
}

function mapPage(raw: unknown): AdminLandingPage | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const pageId = readString(record, "pageId", "PageId");
  const title = readString(record, "title", "Title");
  const slug = readString(record, "slug", "Slug");
  if (!pageId || !title || !slug) return null;
  const status = readString(record, "status", "Status") === "Published" ? "Published" : "Draft";
  const pageTypeRaw = readString(record, "pageType", "PageType");
  const pageType: StorePageType = pageTypeRaw === "Home" ? "Home" : "Landing";
  return {
    pageId,
    pageType,
    locale: readString(record, "locale", "Locale") || "fa",
    slug,
    title,
    seoTitle: readString(record, "seoTitle", "SeoTitle") || null,
    seoDescription: readString(record, "seoDescription", "SeoDescription") || null,
    robotsIndex: readBool(record, "robotsIndex", "RobotsIndex") ?? true,
    robotsFollow: readBool(record, "robotsFollow", "RobotsFollow") ?? true,
    canonicalUrl: readString(record, "canonicalUrl", "CanonicalUrl") || null,
    ogTitle: readString(record, "ogTitle", "OgTitle") || null,
    ogDescription: readString(record, "ogDescription", "OgDescription") || null,
    ogImageUrl: readString(record, "ogImageUrl", "OgImageUrl") || null,
    primaryH1: readString(record, "primaryH1", "PrimaryH1") || null,
    status,
    updatedAt: readString(record, "updatedAt", "UpdatedAt"),
  };
}

function mapSection(raw: unknown): AdminLandingSection | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const pageSectionId = readString(record, "pageSectionId", "PageSectionId");
  const pageId = readString(record, "pageId", "PageId");
  const sectionType = readString(record, "sectionType", "SectionType");
  if (!pageSectionId || !pageId || !sectionType) return null;
  return {
    pageSectionId,
    pageId,
    sectionType,
    sortOrder: typeof record.sortOrder === "number" ? record.sortOrder : Number(record.SortOrder ?? 0),
    isEnabled: Boolean(record.isEnabled ?? record.IsEnabled ?? true),
    config: readString(record, "config", "Config") || "{}",
    updatedAt: readString(record, "updatedAt", "UpdatedAt"),
  };
}

async function readJson(path: string, init?: RequestInit): Promise<{ status: number; body: unknown }> {
  const response = await fetch(path, {
    ...init,
    headers: { ...adminHeaders(init?.headers as Record<string, string> | undefined), ...(init?.headers ?? {}) },
  });
  const text = await response.text();
  let body: unknown = null;
  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      body = { title: text };
    }
  }
  return { status: response.status, body };
}

function fail(status: number, body: unknown): AdminResult<never> {
  if (status === 401 || status === 403) {
    return { ok: false, message: mapAdminErrorMessage("admin.authorization.denied", "fa"), denied: true };
  }
  const code = parseAdminProblemErrorCode(body, status);
  return { ok: false, message: mapAdminErrorMessage(code, "fa") };
}

/** Bust Store+locale+page scoped public SSR cache after publish / Home selection (LOCK-SF-326). */
function currentStoreScope(): string {
  if (typeof document !== "undefined") {
    const scope = document.documentElement.getAttribute("data-storefront-scope");
    if (scope && scope.trim()) return scope.trim();
  }
  return "default";
}

async function invalidatePublicStorePageCache(payload: Omit<StorePageRevalidatePayload, "storeScope"> & { storeScope?: string }): Promise<void> {
  try {
    await fetch("/api/storefront/revalidate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ...payload, storeScope: payload.storeScope || currentStoreScope() }),
      cache: "no-store",
    });
  } catch {
    // Host write already succeeded; SSR TTL still bounds staleness.
  }
}

async function invalidateFromPage(page: Pick<AdminLandingPage, "locale" | "slug" | "pageType">, homeSelection = false): Promise<void> {
  await invalidatePublicStorePageCache({
    locale: page.locale,
    slug: page.pageType === "Home" ? "home" : page.slug,
    homeSelection: homeSelection || page.pageType === "Home",
  });
}

async function invalidateStorePagesNamespace(): Promise<void> {
  await invalidatePublicStorePageCache({ homeSelection: true });
}

export async function listAdminLandingPages(): Promise<AdminResult<AdminLandingPage[]>> {
  try {
    const { status, body } = await readJson("/v1/admin/pages");
    if (status < 200 || status >= 300) return fail(status, body);
    const rows = Array.isArray(body) ? body.map(mapPage).filter((row): row is AdminLandingPage => row !== null) : [];
    return { ok: true, data: rows };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function getAdminLandingPage(pageId: string): Promise<AdminResult<AdminLandingPage>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}`);
    if (status < 200 || status >= 300) return fail(status, body);
    const page = mapPage(body);
    return page ? { ok: true, data: page } : { ok: false, message: mapAdminErrorMessage("landing.page.missing", "fa") };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function createAdminLandingPage(input: AdminLandingPageWriteInput): Promise<AdminResult<AdminLandingPage>> {
  return writePage("/v1/admin/pages", "POST", input);
}

export async function updateAdminLandingPage(pageId: string, input: AdminLandingPageWriteInput): Promise<AdminResult<AdminLandingPage>> {
  return writePage(`/v1/admin/pages/${pageId}`, "PUT", input);
}

async function writePage(path: string, method: string, input: Record<string, unknown>): Promise<AdminResult<AdminLandingPage>> {
  try {
    const { status, body } = await readJson(path, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(input),
    });
    if (status < 200 || status >= 300) return fail(status, body);
    const page = mapPage(body);
    if (!page) return { ok: false, message: mapAdminErrorMessage(null, "fa") };
    await invalidateFromPage(page, page.pageType === "Home");
    return { ok: true, data: page };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function setAdminLandingPageStatus(pageId: string, statusValue: "Draft" | "Published"): Promise<AdminResult<AdminLandingPage>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}/status`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status: statusValue }),
    });
    if (status < 200 || status >= 300) return fail(status, body);
    const page = mapPage(body);
    if (!page) return { ok: false, message: mapAdminErrorMessage(null, "fa") };
    await invalidateFromPage(page, true);
    return { ok: true, data: page };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function getAdminLandingHome(): Promise<AdminResult<AdminLandingHomeSelection>> {
  try {
    const { status, body } = await readJson("/v1/admin/pages/home");
    if (status < 200 || status >= 300) return fail(status, body);
    const record = body && typeof body === "object" ? body as Record<string, unknown> : {};
    return {
      ok: true,
      data: {
        homePageId: readString(record, "homePageId", "HomePageId") || null,
        usesCanonicalHome: Boolean(record.usesCanonicalHome ?? record.UsesCanonicalHome ?? true),
      },
    };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function setAdminLandingHome(homePageId: string | null): Promise<AdminResult<AdminLandingHomeSelection>> {
  try {
    const { status, body } = await readJson("/v1/admin/pages/home", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ homePageId }),
    });
    if (status < 200 || status >= 300) return fail(status, body);
    const record = body && typeof body === "object" ? body as Record<string, unknown> : {};
    await invalidatePublicStorePageCache({
      storeScope: readString(record, "storeScope", "StoreScope") || currentStoreScope(),
      homeSelection: true,
      locale: "fa",
      slug: "home",
    });
    return {
      ok: true,
      data: {
        homePageId: readString(record, "homePageId", "HomePageId") || null,
        usesCanonicalHome: Boolean(record.usesCanonicalHome ?? record.UsesCanonicalHome ?? true),
      },
    };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

/** بازگردانی صفحه اصلی پیش‌فرض — فقط انتخاب Home؛ دادهٔ Catalog دست‌نخورده می‌ماند. */
export async function restoreDefaultAdminHome(): Promise<AdminResult<AdminLandingHomeSelection>> {
  return setAdminLandingHome(null);
}

export async function listAdminLandingSections(pageId: string): Promise<AdminResult<AdminLandingSection[]>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}/sections`);
    if (status < 200 || status >= 300) return fail(status, body);
    const rows = Array.isArray(body) ? body.map(mapSection).filter((row): row is AdminLandingSection => row !== null) : [];
    return { ok: true, data: rows };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function addAdminLandingSection(
  pageId: string,
  sectionType: string,
  config: Record<string, unknown>,
): Promise<AdminResult<AdminLandingSection>> {
  return writeSection(`/v1/admin/pages/${pageId}/sections`, "POST", { sectionType, config: JSON.stringify(config) });
}

export async function updateAdminLandingSection(
  pageId: string,
  sectionId: string,
  config: Record<string, unknown>,
): Promise<AdminResult<AdminLandingSection>> {
  return writeSection(`/v1/admin/pages/${pageId}/sections/${sectionId}`, "PUT", { config: JSON.stringify(config) });
}

export async function setAdminLandingSectionEnabled(
  pageId: string,
  sectionId: string,
  isEnabled: boolean,
): Promise<AdminResult<AdminLandingSection>> {
  return writeSection(`/v1/admin/pages/${pageId}/sections/${sectionId}/enabled`, "PUT", { isEnabled });
}

async function writeSection(path: string, method: string, bodyValue: unknown): Promise<AdminResult<AdminLandingSection>> {
  try {
    const { status, body } = await readJson(path, {
      method,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(bodyValue),
    });
    if (status < 200 || status >= 300) return fail(status, body);
    const row = mapSection(body);
    if (!row) return { ok: false, message: mapAdminErrorMessage(null, "fa") };
    await invalidateStorePagesNamespace();
    return { ok: true, data: row };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function reorderAdminLandingSections(pageId: string, sectionIds: string[]): Promise<AdminResult<AdminLandingSection[]>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}/sections/reorder`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sectionIds }),
    });
    if (status < 200 || status >= 300) return fail(status, body);
    const rows = Array.isArray(body) ? body.map(mapSection).filter((row): row is AdminLandingSection => row !== null) : [];
    await invalidateStorePagesNamespace();
    return { ok: true, data: rows };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function deleteAdminLandingSection(pageId: string, sectionId: string): Promise<AdminResult<true>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}/sections/${sectionId}`, { method: "DELETE" });
    if (status < 200 || status >= 300) return fail(status, body);
    await invalidateStorePagesNamespace();
    return { ok: true, data: true };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export async function loadAdminLandingPreview(pageId: string): Promise<AdminResult<unknown>> {
  try {
    const { status, body } = await readJson(`/v1/admin/pages/${pageId}/preview`);
    if (status < 200 || status >= 300) return fail(status, body);
    return { ok: true, data: body };
  } catch {
    return { ok: false, message: mapAdminErrorMessage("host-unreachable", "fa") };
  }
}

export function publicPathForStorePage(page: Pick<AdminLandingPage, "pageType" | "slug">): string {
  return page.pageType === "Home" ? "/" : `/landing/${page.slug}`;
}
