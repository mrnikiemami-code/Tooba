import type { Metadata } from "next";
import { loadHomeComposition } from "./composition/composition-api.ts";
import { StorefrontShell } from "./storefront/storefront-shell.tsx";
import { StorefrontShopeivaHome } from "./storefront/storefront-home.tsx";
import { loadStorefrontHome, storefrontHostOrigin } from "./storefront/storefront-api.ts";
import { resolveRequestLocale } from "../lib/i18n/resolve-request-locale.ts";
import { buildLocaleAlternates, canonicalForLocale, localeToContentApi } from "../lib/i18n/routing.ts";
import { openGraphLocaleFor } from "../lib/i18n/locale.ts";

/**
 * فرادادهٔ خانهٔ فروشگاه برای خزنده و اشتراک‌گذاری.
 */
export async function generateMetadata(): Promise<Metadata> {
  const locale = await resolveRequestLocale();
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
    openGraph: { locale: openGraphLocaleFor(locale) },
  };
}

/**
 * خانهٔ فروشگاه زنده با پوستهٔ Shopeiva. داده از Host می‌آید نه از JSON دمو.
 */
export default async function HomePage() {
  const locale = await resolveRequestLocale();
  const contentLocale = localeToContentApi(locale);
  const [home, composition] = await Promise.all([
    loadStorefrontHome(contentLocale),
    loadHomeComposition(contentLocale),
  ]);
  if (!home) {
    return (
      <StorefrontShell categories={[]}>
        <div className="py-16 text-center bg-white rounded-2xl mt-6">
          فروشگاه زنده در دسترس نیست. Host باید روی {storefrontHostOrigin()} پاسخ بدهد.
        </div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={home.categories} searchCatalog={home.featuredProducts}>
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
    </StorefrontShell>
  );
}
