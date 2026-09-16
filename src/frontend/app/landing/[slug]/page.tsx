import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { StorefrontLandingSections } from "../../storefront/storefront-landing-sections.tsx";
import {
  resolveLandingRouteModel,
  resolvePublishedStorePage,
} from "../../storefront/storefront-store-page-resolver.ts";
import { buildStorePageMetadata, buildStorePageStructuredData, storePagePublicPath } from "../../storefront/storefront-page-seo.ts";
import { resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale.ts";
import { isReservedLandingSlug } from "../../../lib/storefront-landing/reserved-slugs.ts";

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const locale = await resolveRequestLocale();
  if (isReservedLandingSlug(slug)) {
    return { robots: { index: false, follow: false } };
  }
  // Shares request-scoped resolver with the page body (LOCK-SF-325) — no second Host round-trip.
  const page = await resolvePublishedStorePage(slug, locale);
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
  const model = await resolveLandingRouteModel(slug, locale);
  if (!model) {
    notFound();
  }
  const { page, context } = model;
  const canonicalPath = storePagePublicPath(page);
  const jsonLd = buildStorePageStructuredData(page, canonicalPath);
  return (
    <StorefrontShell categories={context.categories} searchCatalog={context.products} fullBleed>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <div data-testid="landing-route" data-landing-canonical="1">
        <StorefrontLandingSections page={page} context={context} />
      </div>
    </StorefrontShell>
  );
}
