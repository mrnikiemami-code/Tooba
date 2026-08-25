import type { StorefrontProductDetailPage } from "./storefront-model.ts";

/**
 * مرز دادهٔ ساختاریافتهٔ Product را فقط از اطلاعات عمومی PDP می‌سازد.
 * شناسه‌های داخلی Product، Variant، Offer و Party عمداً وارد خروجی SEO نمی‌شوند.
 */
export function buildProductStructuredData(detail: StorefrontProductDetailPage, canonicalPath: string) {
  return {
    "@context": "https://schema.org",
    "@type": "Product",
    name: detail.title,
    description: detail.seoDescription,
    url: canonicalPath,
    ...(detail.brandName ? { brand: { "@type": "Brand", name: detail.brandName } } : {}),
    offers: {
      "@type": "Offer",
      url: canonicalPath,
      price: detail.primaryOffer.amountExclusiveOfTax,
      priceCurrency: detail.primaryOffer.currency,
      availability:
        detail.primaryOffer.availableUnits > 0
          ? "https://schema.org/InStock"
          : "https://schema.org/OutOfStock",
      seller: { "@type": "Organization", name: detail.primaryOffer.sellerDisplayName },
    },
  };
}
