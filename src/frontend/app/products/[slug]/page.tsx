import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShopeivaPdp } from "../../storefront/storefront-pdp.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontCategories, loadStorefrontDetail } from "../../storefront/storefront-api.ts";
import { buildProductStructuredData } from "../../storefront/storefront-product-seo.ts";
import { resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale.ts";
import { buildLocaleAlternates, canonicalForLocale } from "../../../lib/i18n/routing.ts";
import { openGraphLocaleFor } from "../../../lib/i18n/locale.ts";

/**
 * فرادادهٔ SEO از ترکیب Host؛ canonical locale-prefixed.
 */
export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const locale = await resolveRequestLocale();
  const detail = await loadStorefrontDetail(slug);
  if (!detail) {
    return { title: "کالا پیدا نشد" };
  }
  const internalPath = `/products/${detail.slug}`;
  const alternates = buildLocaleAlternates(internalPath, { includeXDefault: true });
  return {
    title: detail.seoTitle,
    description: detail.seoDescription,
    alternates: {
      canonical: canonicalForLocale(locale, internalPath),
      languages: alternates.languages,
    },
    openGraph: { locale: openGraphLocaleFor(locale) },
    robots: { index: true, follow: true },
  };
}

/**
 * PDP زنده با گالری، Offer اصلی و فروشندگان دیگر روی ترکیب Shopeiva.
 * Shell categories come from the light categories endpoint — not a full /home reload.
 */
export default async function ProductDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const locale = await resolveRequestLocale();
  const [detail, categories] = await Promise.all([loadStorefrontDetail(slug), loadStorefrontCategories()]);
  if (!detail) {
    notFound();
  }

  const canonicalPath = canonicalForLocale(locale, `/products/${detail.slug}`);
  const jsonLd = buildProductStructuredData(detail, canonicalPath);

  return (
    <StorefrontShell categories={categories ?? []}>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <StorefrontShopeivaPdp detail={detail} />
    </StorefrontShell>
  );
}
