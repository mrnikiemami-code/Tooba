import type { Metadata } from "next";
import { StorefrontShopeivaCart } from "../storefront/storefront-cart.tsx";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";
import type { StorefrontProductCard } from "../storefront/storefront-model.ts";

export const metadata: Metadata = {
  title: "سبد خرید | توبا",
  robots: { index: false, follow: false },
};

function pickCartRecommendations(home: Awaited<ReturnType<typeof loadStorefrontHome>>): StorefrontProductCard[] {
  if (!home) {
    return [];
  }
  const seen = new Set<string>();
  const out: StorefrontProductCard[] = [];
  for (const card of [...home.newArrivals, ...home.featuredProducts, ...home.specialOffers]) {
    if (!card.primaryOfferId || seen.has(card.productId)) {
      continue;
    }
    seen.add(card.productId);
    out.push(card);
    if (out.length >= 12) {
      break;
    }
  }
  return out;
}

/**
 * صفحهٔ سبد زنده. ایندکس‌پذیر نیست و Checkout را جعل نمی‌کند.
 * پیشنهادها از feed واقعی خانه (newArrivals/featured) است نه کالای استاتیک.
 */
export default async function CartPage() {
  const home = await loadStorefrontHome();
  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <StorefrontShopeivaCart recommendations={pickCartRecommendations(home)} />
    </StorefrontShell>
  );
}
