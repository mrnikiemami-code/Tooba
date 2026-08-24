import { StorefrontShell } from "./storefront/storefront-shell.tsx";
import { StorefrontShopeivaHome } from "./storefront/storefront-home.tsx";
import { loadStorefrontHome } from "./storefront/storefront-api.ts";

/**
 * خانهٔ فروشگاه زنده با پوستهٔ Shopeiva. داده از Host می‌آید نه از JSON دمو.
 */
export default async function HomePage() {
  const home = await loadStorefrontHome();
  if (!home) {
    return (
      <StorefrontShell categories={[]}>
        <div className="py-16 text-center bg-white rounded-2xl mt-6">فروشگاه زنده در دسترس نیست. Host باید روی درگاه ۵۰۸۸ پاسخ بدهد.</div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={home.categories} searchCatalog={home.featuredProducts}>
      <StorefrontShopeivaHome
        heroTitle={home.heroTitle}
        heroSubtitle={home.heroSubtitle}
        categories={home.categories}
        products={home.featuredProducts}
      />
    </StorefrontShell>
  );
}
