import type { Metadata } from "next";
import { StorefrontShell } from "./storefront/storefront-shell.tsx";
import { StorefrontShopeivaHome } from "./storefront/storefront-home.tsx";
import { loadStorefrontHome, storefrontHostOrigin } from "./storefront/storefront-api.ts";

/**
 * فرادادهٔ خانهٔ فروشگاه برای خزنده و اشتراک‌گذاری.
 */
export const metadata: Metadata = {
  title: "فروشگاه توبا | خانه",
  description: "ویترین زنده Catalog با قیمت Offer و موجودی انبار",
  alternates: {
    canonical: "/",
  },
};

/**
 * خانهٔ فروشگاه زنده با پوستهٔ Shopeiva. داده از Host می‌آید نه از JSON دمو.
 */
export default async function HomePage() {
  const home = await loadStorefrontHome();
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
      />
    </StorefrontShell>
  );
}
