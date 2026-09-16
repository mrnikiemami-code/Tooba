import type { Metadata } from "next";
import { loadHomeComposition } from "./composition/composition-api.ts";
import { StorefrontShell } from "./storefront/storefront-shell.tsx";
import { StorefrontShopeivaHome } from "./storefront/storefront-home.tsx";
import { StorefrontLandingSections } from "./storefront/storefront-landing-sections.tsx";
import { loadLandingRenderContext, loadStorefrontHomeSelection } from "./storefront/storefront-landing-api.ts";
import { loadStorefrontHome, storefrontHostOrigin } from "./storefront/storefront-api.ts";
import { buildStorePageMetadata, buildStorePageStructuredData } from "./storefront/storefront-page-seo.ts";
import { resolveRequestLocale } from "../lib/i18n/resolve-request-locale.ts";
import { buildLocaleAlternates, canonicalForLocale, localeToContentApi } from "../lib/i18n/routing.ts";
import { openGraphLocaleFor } from "../lib/i18n/locale.ts";

/**
 * فرادادهٔ خانهٔ فروشگاه برای خزنده و اشتراک‌گذاری.
 */
export async function generateMetadata(): Promise<Metadata> {
  const locale = await resolveRequestLocale();
  const selection = await loadStorefrontHomeSelection();
  const landing = selection?.selectedPage;
  if (landing) {
    return buildStorePageMetadata({ ...landing, pageType: "Home" }, locale);
  }
  const alternates = buildLocaleAlternates("/", { includeXDefault: true });
  return {
    title: locale === "fa" ? "فروشگاه توبا | خانه" : "Tooba Store | Home",
    description:
      locale === "fa"
        ? "ویترین زنده Catalog با قیمت Offer و موجودی انبار"
        : "Live catalog storefront with offer pricing and inventory",
    alternates: {
      canonical: canonicalForLocale(locale, "/"),
      languages: alternates.languages,
    },
    openGraph: { locale: openGraphLocaleFor(locale), type: "website" },
    robots: { index: true, follow: true },
  };
}

/**
 * خانهٔ فروشگاه زنده با پوستهٔ Shopeiva. داده از Host می‌آید نه از JSON دمو.
 */
export default async function HomePage() {
  const locale = await resolveRequestLocale();
  const contentLocale = localeToContentApi(locale);
  const selection = await loadStorefrontHomeSelection();
  const [home, composition] = await Promise.all([
    loadStorefrontHome(contentLocale),
    loadHomeComposition(contentLocale),
  ]);
  if (!home) {
    return (
      <StorefrontShell categories={[]} fullBleed>
        <div className="py-16 text-center bg-surface rounded-2xl mt-6" data-storefront-surface-role="card">
          فروشگاه زنده در دسترس نیست. Host باید روی {storefrontHostOrigin()} پاسخ بدهد.
        </div>
      </StorefrontShell>
    );
  }

  if (selection?.selectedPage) {
    const context = await loadLandingRenderContext(contentLocale, selection.selectedPage);
    const page = { ...selection.selectedPage, pageType: "Home" as const };
    const jsonLd = buildStorePageStructuredData(page, "/");
    return (
      <StorefrontShell categories={home.categories} searchCatalog={home.featuredProducts} fullBleed>
        <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
        <div data-testid="storefront-custom-home" data-home-route="custom">
          <StorefrontLandingSections page={page} context={context} />
        </div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={home.categories} searchCatalog={home.featuredProducts} fullBleed>
      <div data-testid="storefront-default-home" data-home-route="default">
        <StorefrontShopeivaHome
          heroTitle={home.heroTitle}
          heroSubtitle={home.heroSubtitle}
          categories={home.categories}
          homeCategories={home.homeCategories}
          specialOffers={home.specialOffers}
          campaignProducts={home.campaignProducts}
          newArrivals={home.newArrivals}
          productRail={home.productRail}
          brands={home.brands}
          bestSellerColumns={home.bestSellerColumns}
          mostViewedProducts={home.mostViewedProducts}
          featuredReviews={home.featuredReviews}
          latestArticles={home.latestArticles}
          compositionSections={composition?.sections}
        />
      </div>
    </StorefrontShell>
  );
}
