import type { MetadataRoute } from "next";
import { LOCALES } from "../lib/i18n/locale.ts";
import { localePath } from "../lib/i18n/routing.ts";
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

/** sitemap با URLهای locale-prefixed و hreflang واقعی fa/en. */
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

  const articlesPayload = await fetchJson("/v1/content/articles?page=1&pageSize=50");
  const articles = (articlesPayload as { items?: { slug?: string }[] } | null)?.items ?? [];
  for (const article of articles) {
    const slug = article?.slug;
    if (!slug) continue;
    const internal = `/blogs/${slug}`;
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

  return entries;
}
