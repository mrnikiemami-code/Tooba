import type { MetadataRoute } from "next";
import { LOCALES } from "../lib/i18n/locale.ts";
import { localePath, localeToContentApi } from "../lib/i18n/routing.ts";
import { loadPublicAuthors, loadPublicCategories } from "./content/content-api.ts";
import { storefrontHostOrigin } from "./storefront/storefront-api.ts";

const STATIC_INTERNAL_PATHS = [
  "/",
  "/products",
  "/blogs",
  "/offers",
  "/sale",
  "/new-products",
  "/most-viewed",
  "/best-seller",
  "/brands",
  "/sellers",
  "/trending",
] as const;

async function fetchJson(path: string): Promise<unknown> {
  try {
    const response = await fetch(`${storefrontHostOrigin()}${path}`, { cache: "no-store" });
    if (!response.ok) return null;
    return await response.json();
  } catch {
    return null;
  }
}

/** sitemap — URLهای locale-prefixed؛ مقالات فقط در locale واقعی خود. */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = process.env.NEXT_PUBLIC_SITE_ORIGIN ?? "http://127.0.0.1:3000";
  const entries: MetadataRoute.Sitemap = [];

  for (const internal of STATIC_INTERNAL_PATHS) {
    for (const locale of LOCALES) {
      const path = localePath(locale, internal);
      entries.push({
        url: `${base}${path}`,
        alternates: {
          languages: Object.fromEntries(
            LOCALES.map((alt) => [alt === "fa" ? "fa-IR" : "en", `${base}${localePath(alt, internal)}`]),
          ),
        },
      });
    }
  }

  const listing = await fetchJson("/v1/storefront/listing?page=1&pageSize=50");
  const items = (listing as { items?: { slug?: string }[] } | null)?.items ?? [];
  for (const item of items) {
    const slug = item?.slug;
    if (!slug) continue;
    const internal = `/products/${slug}`;
    for (const locale of LOCALES) {
      entries.push({
        url: `${base}${localePath(locale, internal)}`,
        alternates: {
          languages: Object.fromEntries(
            LOCALES.map((alt) => [alt === "fa" ? "fa-IR" : "en", `${base}${localePath(alt, internal)}`]),
          ),
        },
      });
    }
  }

  for (const locale of LOCALES) {
    const contentLocale = localeToContentApi(locale);
    const articlesPayload = await fetchJson(
      `/v1/content/articles?page=1&pageSize=50&locale=${encodeURIComponent(contentLocale)}`,
    );
    const articles = (articlesPayload as { items?: { slug?: string; canonicalPath?: string }[] } | null)?.items ?? [];
    for (const article of articles) {
      const slug = article?.slug;
      if (!slug) continue;
      const path = article.canonicalPath ?? localePath(locale, `/blogs/${slug}`);
      entries.push({ url: `${base}${path}` });
    }
  }

  // دسته‌ها زبان‌وابسته‌اند؛ نویسندگان با مسیر locale جدا — بدون hreflang ساختگی.
  for (const locale of LOCALES) {
    const contentLocale = localeToContentApi(locale);
    const [categories, authors] = await Promise.all([
      loadPublicCategories(contentLocale),
      loadPublicAuthors(contentLocale),
    ]);
    for (const category of categories) {
      const path = category.canonicalPath ?? localePath(locale, `/blogs/category/${category.slug}`);
      entries.push({ url: `${base}${path}` });
    }
    for (const author of authors) {
      const path = author.canonicalPath ?? localePath(locale, `/blogs/author/${author.slug}`);
      entries.push({ url: `${base}${path}` });
    }
  }

  return entries;
}
