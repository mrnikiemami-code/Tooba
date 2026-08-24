import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontBuyBox } from "../../storefront/storefront-buy-box.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontDetail, loadStorefrontHome, storefrontMediaUrl } from "../../storefront/storefront-api.ts";

/**
 * فرادادهٔ SEO از ترکیب Host؛ محتوا را از دمو نمی‌سازد.
 */
export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const detail = await loadStorefrontDetail(slug);
  if (!detail) {
    return { title: "کالا پیدا نشد" };
  }
  return {
    title: detail.seoTitle,
    description: detail.seoDescription,
    alternates: { canonical: `/products/${detail.slug}` },
    robots: { index: true, follow: true },
  };
}

/**
 * PDP زنده با گالری، Offer اصلی و فروشندگان دیگر.
 */
export default async function ProductDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const [detail, home] = await Promise.all([loadStorefrontDetail(slug), loadStorefrontHome()]);
  if (!detail) {
    notFound();
  }

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: detail.title,
    description: detail.seoDescription,
    brand: detail.brandName,
    offers: {
      "@type": "Offer",
      price: detail.primaryOffer.amountExclusiveOfTax,
      priceCurrency: detail.primaryOffer.currency,
      availability: detail.primaryOffer.availableUnits > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
      seller: { "@type": "Organization", name: detail.primaryOffer.sellerDisplayName },
    },
  };

  return (
    <StorefrontShell categories={home?.categories ?? []}>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <div className="sf-pdp">
        <div className="sf-gallery">
          {/* تصویر نمایشی Host خارج از بهینه‌ساز Next است و حقیقت Catalog نیست. */}
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={storefrontMediaUrl(detail.mediaAssetIds[0])} alt={detail.title} />
          {detail.description ? <p className="sf-meta">{detail.description}</p> : null}
        </div>
        <StorefrontBuyBox detail={detail} />
      </div>
    </StorefrontShell>
  );
}
