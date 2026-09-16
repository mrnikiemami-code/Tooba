import type { Metadata } from "next";
import type { StorefrontLandingPage } from "./storefront-landing-api.ts";
import { buildLocaleAlternates, canonicalForLocale } from "../../lib/i18n/routing.ts";
import { openGraphLocaleFor, type Locale } from "../../lib/i18n/locale.ts";

export function storePagePublicPath(page: Pick<StorefrontLandingPage, "pageType" | "slug">): string {
  return page.pageType === "Home" ? "/" : `/landing/${page.slug}`;
}

export function buildStorePageMetadata(page: StorefrontLandingPage, locale: Locale): Metadata {
  const path = storePagePublicPath(page);
  const canonical = page.canonicalUrl?.trim() || canonicalForLocale(locale, path);
  const alternates = buildLocaleAlternates(path, { includeXDefault: true });
  const title = page.seoTitle || page.title;
  const description = page.seoDescription ?? undefined;
  const ogTitle = page.ogTitle || title;
  const ogDescription = page.ogDescription || description;
  return {
    title,
    description,
    alternates: {
      canonical,
      languages: alternates.languages,
    },
    robots: {
      index: page.robotsIndex,
      follow: page.robotsFollow,
    },
    openGraph: {
      title: ogTitle,
      description: ogDescription,
      locale: openGraphLocaleFor(locale),
      url: canonical,
      ...(page.ogImageUrl ? { images: [{ url: page.ogImageUrl }] } : {}),
      type: "website",
    },
  };
}

/** Typed structured data — no raw JSON editor for Admin users. */
export function buildStorePageStructuredData(
  page: StorefrontLandingPage,
  canonicalPath: string,
  options?: { siteName?: string },
) {
  const siteName = options?.siteName ?? "Tooba";
  if (page.pageType === "Home") {
    return {
      "@context": "https://schema.org",
      "@graph": [
        {
          "@type": "WebSite",
          name: siteName,
          url: canonicalPath,
          inLanguage: page.locale,
        },
        {
          "@type": "WebPage",
          name: page.seoTitle || page.title,
          description: page.seoDescription ?? undefined,
          url: canonicalPath,
          isPartOf: { "@type": "WebSite", name: siteName, url: canonicalPath },
          inLanguage: page.locale,
        },
      ],
    };
  }

  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "WebPage",
        name: page.seoTitle || page.title,
        description: page.seoDescription ?? undefined,
        url: canonicalPath,
        inLanguage: page.locale,
      },
      {
        "@type": "BreadcrumbList",
        itemListElement: [
          { "@type": "ListItem", position: 1, name: "Home", item: "/" },
          { "@type": "ListItem", position: 2, name: page.title, item: canonicalPath },
        ],
      },
    ],
  };
}
