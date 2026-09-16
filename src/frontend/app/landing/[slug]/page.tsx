import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import { loadLandingRenderContext, loadPublishedLandingPage } from "../../storefront/storefront-landing-api.ts";
import { loadStorefrontHome } from "../../storefront/storefront-api.ts";
import { buildStorePageMetadata, buildStorePageStructuredData, storePagePublicPath } from "../../storefront/storefront-page-seo.ts";
import { resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale.ts";
import { localeToContentApi } from "../../../lib/i18n/routing.ts";
import { isReservedLandingSlug } from "../../../lib/storefront-landing/reserved-slugs.ts";

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const locale = await resolveRequestLocale();
  if (isReservedLandingSlug(slug)) {
    return { robots: { index: false, follow: false } };
  }
  const page = await loadPublishedLandingPage(slug, locale);
  if (!page) {
    return { title: locale === "fa" ? "صفحه پیدا نشد | توبا" : "Page not found | Tooba", robots: { index: false, follow: false } };
  }
  return buildStorePageMetadata(page, locale);
}

/**
 * مسیر کاننیکال Landing: /landing/{slug}
 */
export default async function StoreLandingCanonicalRoute({ params }: Props) {
  const { slug } = await params;
  if (isReservedLandingSlug(slug)) {
    notFound();
  }
  const locale = await resolveRequestLocale();
  const page = await loadPublishedLandingPage(slug, locale);
  if (!page) {
    notFound();
  }
  const contentLocale = localeToContentApi(locale);
  const [home, context] = await Promise.all([
    loadStorefrontHome(contentLocale),
    loadLandingRenderContext(contentLocale, page),
  ]);
  const canonicalPath = storePagePublicPath(page);
  const jsonLd = buildStorePageStructuredData(page, canonicalPath);
  return (
    <StorefrontShell categories={home?.categories ?? []} fullBleed>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <div data-testid="landing-route" data-landing-canonical="1">
        <StorefrontLandingSections page={page} context={context} />
      </div>
    </StorefrontShell>
  );
}
