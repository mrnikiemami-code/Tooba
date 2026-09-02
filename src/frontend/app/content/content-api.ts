/**
 * کلاینت عمومی/ادمین Content — فقط دادهٔ زندهٔ Host.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";

export interface ContentArticleCard {
  articleId: string;
  slug: string;
  title: string;
  excerpt: string;
  coverMediaAssetId: string | null;
  publishDate: string;
  authorDisplayName: string;
  tags: string[];
  isFeatured: boolean;
  body: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  category: string | null;
  seoImageMediaAssetId: string | null;
  canonicalPath: string | null;
  locale: string;
}

export interface ContentArticlePage {
  items: ContentArticleCard[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface AdminContentArticle {
  articleId: string;
  slug: string;
  title: string;
  excerpt: string;
  body: string;
  locale: string;
  seoTitle: string | null;
  seoDescription: string | null;
  category: string | null;
  categoryId: string | null;
  authorId: string | null;
  coverMediaAssetId: string | null;
  authorDisplayName: string;
  tags: string[];
  isFeatured: boolean;
  status: string;
  publishDate: string;
  createdAt: string;
  updatedAt: string;
  id: string;
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

function tagsOf(value: unknown): string[] {
  if (Array.isArray(value)) return value.map((tag) => String(tag)).filter(Boolean);
  if (typeof value === "string" && value.trim()) {
    return value.split(",").map((tag) => tag.trim()).filter(Boolean);
  }
  return [];
}

export function mapContentArticle(value: unknown): ContentArticleCard | null {
  const item = recordOf(value);
  if (!item) return null;
  const id = text(prop(item, "articleId", "ArticleId"));
  const slug = text(prop(item, "slug", "Slug"));
  if (!id || !slug) return null;
  const cover = prop(item, "coverMediaAssetId", "CoverMediaAssetId");
  return {
    articleId: id,
    slug,
    title: text(prop(item, "title", "Title")),
    excerpt: text(prop(item, "excerpt", "Excerpt")),
    coverMediaAssetId: cover == null ? null : text(cover),
    publishDate: text(prop(item, "publishDate", "PublishDate")),
    authorDisplayName: text(prop(item, "authorDisplayName", "AuthorDisplayName")),
    tags: tagsOf(prop(item, "tags", "Tags")),
    isFeatured: Boolean(prop(item, "isFeatured", "IsFeatured")),
    body: (() => {
      const body = prop(item, "body", "Body");
      return body == null ? null : text(body);
    })(),
    seoTitle: (() => {
      const seo = prop(item, "seoTitle", "SeoTitle");
      return seo == null ? null : text(seo);
    })(),
    seoDescription: (() => {
      const seo = prop(item, "seoDescription", "SeoDescription");
      return seo == null ? null : text(seo);
    })(),
    category: (() => {
      const cat = prop(item, "category", "Category");
      return cat == null || cat === "" ? null : text(cat);
    })(),
    categoryId: (() => {
      const id = prop(item, "categoryId", "CategoryId");
      return id == null || id === "" ? null : text(id);
    })(),
    authorId: (() => {
      const id = prop(item, "authorId", "AuthorId");
      return id == null || id === "" ? null : text(id);
    })(),
    locale: text(prop(item, "locale", "Locale"), "fa-IR"),
    seoImageMediaAssetId: (() => {
      const id = prop(item, "seoImageMediaAssetId", "SeoImageMediaAssetId");
      return id == null || id === "" ? null : text(id);
    })(),
    canonicalPath: (() => {
      const path = prop(item, "canonicalPath", "CanonicalPath");
      return path == null || path === "" ? null : text(path);
    })(),
  };
}

export function mapAdminContentArticle(value: unknown): AdminContentArticle | null {
  const mapped = mapContentArticle(value);
  const item = recordOf(value);
  if (!mapped || !item) return null;
  return {
    ...mapped,
    body: mapped.body ?? "",
    status: text(prop(item, "status", "Status")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    id: mapped.articleId,
  };
}

export function contentCoverUrl(coverMediaAssetId: string | null): string {
  if (!coverMediaAssetId) return "/images/blogs/blog-1.jpg";
  const known = ["d0d0d0d0-0001-4000-8000-000000000001", "d0d0d0d0-0002-4000-8000-000000000002", "d0d0d0d0-0003-4000-8000-000000000003", "d0d0d0d0-0004-4000-8000-000000000004"];
  const index = known.indexOf(coverMediaAssetId);
  return `/images/blogs/blog-${index >= 0 ? index + 1 : 1}.jpg`;
}

export function formatContentDate(iso: string): string {
  if (!iso) return "—";
  try {
    return new Intl.DateTimeFormat("fa-IR", { dateStyle: "medium" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

const ARTICLE_LOCALE_LABELS: Record<string, string> = {
  "fa-IR": "فارسی",
  "en-US": "English",
  en: "English",
};

/** برچسب نمایشی زبان مقاله. */
export function formatArticleLocaleLabel(locale: string): string {
  return ARTICLE_LOCALE_LABELS[locale] ?? locale;
}

/** جهت ویرایشگر بر اساس locale مقاله. */
export function articleEditorDirection(locale: string): "rtl" | "ltr" {
  return locale.startsWith("fa") ? "rtl" : "ltr";
}

/** قالب تاریخ انتشار بر اساس زبان مقاله. */
export function formatArticleDate(iso: string, locale: string): string {
  if (!iso) return "—";
  try {
    const calendar = locale.startsWith("fa") ? "persian" : "gregory";
    return new Intl.DateTimeFormat(locale.startsWith("fa") ? "fa-IR" : "en-US", {
      dateStyle: "medium",
      timeStyle: "short",
      calendar,
    }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function contentBase(): string {
  if (typeof window === "undefined") {
    return process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088";
  }
  return "";
}

export async function loadPublishedArticles(
  page = 1,
  pageSize = 12,
  category?: string,
  locale?: string,
): Promise<ContentArticlePage> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (category) params.set("category", category);
  if (locale) params.set("locale", locale);
  try {
    const response = await fetch(`${contentBase()}/v1/content/articles?${params}`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) return { items: [], page, pageSize, totalCount: 0 };
    const payload = await response.json();
    const root = recordOf(payload);
    const itemsRaw = root ? prop(root, "items", "Items") : null;
    const items = Array.isArray(itemsRaw)
      ? itemsRaw.map(mapContentArticle).filter((row): row is ContentArticleCard => row !== null)
      : [];
    return {
      items,
      page: Number(prop(root ?? {}, "page", "Page") ?? page) || page,
      pageSize: Number(prop(root ?? {}, "pageSize", "PageSize") ?? pageSize) || pageSize,
      totalCount: Number(prop(root ?? {}, "totalCount", "TotalCount") ?? items.length) || items.length,
    };
  } catch {
    return { items: [], page, pageSize, totalCount: 0 };
  }
}

export async function loadPublishedArticleBySlug(slug: string, locale: string): Promise<ContentArticleCard | null> {
  const params = `?locale=${encodeURIComponent(locale)}`;
  try {
    const response = await fetch(`${contentBase()}/v1/content/articles/${encodeURIComponent(slug)}${params}`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) return null;
    return mapContentArticle(await response.json());
  } catch {
    return null;
  }
}

function adminHeaders(json = false): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (json) headers["Content-Type"] = "application/json";
  const actor = typeof window !== "undefined" ? localStorage.getItem("tooba.adminActorUserId") : null;
  if (actor) headers[ADMIN_DEV_ACTOR_HEADER] = actor;
  return headers;
}

export async function loadAdminContentArticles(): Promise<AdminResult<AdminContentArticle[]>> {
  try {
    const response = await fetch("/v1/admin/content/articles?page=1&pageSize=100", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const payload = await response.json();
    const root = recordOf(payload);
    const itemsRaw = root ? prop(root, "items", "Items") : Array.isArray(payload) ? payload : null;
    const rows = Array.isArray(itemsRaw)
      ? itemsRaw.map(mapAdminContentArticle).filter((row): row is AdminContentArticle => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — مقالات Admin. */
export function queryAdminContentArticlesGrid(
  query: GridServerQuery,
): Promise<AdminGridQueryResult<AdminContentArticle>> {
  return postAdminGridQuery("/v1/admin/content/articles/query", query, adminHeaders(), (item) =>
    mapAdminContentArticle(item),
  );
}

export async function publishAdminArticle(articleId: string): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/content/articles/${articleId}/publish`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function unpublishAdminArticle(articleId: string): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/content/articles/${articleId}/unpublish`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function loadAdminArticle(articleId: string): Promise<AdminResult<AdminContentArticle>> {
  try {
    const response = await fetch(`/v1/admin/content/articles/${encodeURIComponent(articleId)}`, {
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) {
      return { state: "error", data: null, status: response.status, message: "content.article.missing" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const article = mapAdminContentArticle(await response.json());
    return article
      ? { state: "ok", data: article, status: response.status }
      : { state: "error", data: null, status: response.status, message: "invalid-response" };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function updateAdminArticle(
  articleId: string,
  input: {
    title: string;
    excerpt: string;
    body: string;
    authorId?: string | null;
    category?: string | null;
    categoryId?: string | null;
    coverMediaAssetId?: string | null;
    seoTitle?: string | null;
    seoDescription?: string | null;
    tags?: string[];
    isFeatured?: boolean;
    locale?: string;
    publishDate?: string | null;
  },
): Promise<{ ok: boolean; article?: AdminContentArticle; message?: string }> {
  try {
    const response = await fetch(`/v1/admin/content/articles/${encodeURIComponent(articleId)}`, {
      method: "PUT",
      headers: adminHeaders(true),
      body: JSON.stringify({
        title: input.title,
        excerpt: input.excerpt,
        body: input.body,
        authorId: input.authorId ?? null,
        category: input.category ?? null,
        categoryId: input.categoryId ?? null,
        coverMediaAssetId: input.coverMediaAssetId ?? null,
        seoTitle: input.seoTitle ?? null,
        seoDescription: input.seoDescription ?? null,
        tags: input.tags ?? [],
        isFeatured: input.isFeatured ?? false,
        locale: input.locale ?? null,
        publishDate: input.publishDate ?? null,
      }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      const root = recordOf(payload);
      return {
        ok: false,
        message: text(root?.errorCode ?? root?.detail ?? root?.title, `update-http-${response.status}`),
      };
    }
    const article = mapAdminContentArticle(payload);
    return article ? { ok: true, article } : { ok: false, message: "invalid-response" };
  } catch {
    return { ok: false, message: "host-unreachable" };
  }
}

/** آیا تغییر locale پس از انتشار یا ارجاع مجاز نیست. */
export function isArticleLocaleLocked(article: Pick<AdminContentArticle, "status" | "authorId" | "categoryId">): boolean {
  const published = article.status === "Published" || article.status === "1";
  return published || Boolean(article.authorId) || Boolean(article.categoryId);
}

export async function createAdminArticle(input: {
  slug: string;
  title: string;
  excerpt: string;
  body: string;
  authorDisplayName: string;
  authorId?: string | null;
  category?: string;
  categoryId?: string | null;
  seoTitle?: string;
  seoDescription?: string;
  locale?: string;
  publishDate?: string | null;
}): Promise<{ ok: boolean; article?: AdminContentArticle; message?: string }> {
  try {
    const response = await fetch("/v1/admin/content/articles", {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        slug: input.slug,
        title: input.title,
        excerpt: input.excerpt,
        body: input.body,
        authorDisplayName: input.authorDisplayName,
        authorId: input.authorId ?? null,
        tags: [],
        isFeatured: false,
        category: input.category ?? null,
        categoryId: input.categoryId ?? null,
        seoTitle: input.seoTitle ?? null,
        seoDescription: input.seoDescription ?? null,
        locale: input.locale ?? "fa-IR",
        publishDate: input.publishDate ?? null,
      }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { ok: false, message: recordOf(payload)?.title as string ?? `create-http-${response.status}` };
    }
    const article = mapAdminContentArticle(payload);
    return article ? { ok: true, article } : { ok: false, message: "invalid-response" };
  } catch {
    return { ok: false, message: "host-unreachable" };
  }
}
