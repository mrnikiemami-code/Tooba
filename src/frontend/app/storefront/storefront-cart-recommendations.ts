import type { StorefrontHomePage } from "./storefront-model.ts";
import type { StorefrontProductCard } from "./storefront-model.ts";

/**
 * TB-245-R4 — پیشنهادهای سبد از feed زندهٔ خانه؛ فقط پس از قابل‌استفاده‌شدن سبد اصلی بارگذاری می‌شوند.
 * هیچ کالای استاتیک/ساختگی ساخته نمی‌شود.
 */
export function pickCartRecommendations(home: StorefrontHomePage | null | undefined): StorefrontProductCard[] {
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
