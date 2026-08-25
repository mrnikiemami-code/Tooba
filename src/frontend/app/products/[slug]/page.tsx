import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShopeivaPdp } from "../../storefront/storefront-pdp.tsx";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontDetail, loadStorefrontHome } from "../../storefront/storefront-api.ts";
import { buildProductStructuredData } from "../../storefront/storefront-product-seo.ts";

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
 * PDP زنده با گالری، Offer اصلی و فروشندگان دیگر روی ترکیب Shopeiva.
 */
export default async function ProductDetailPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const [detail, home] = await Promise.all([loadStorefrontDetail(slug), loadStorefrontHome()]);
  if (!detail) {
    notFound();
  }

  const canonicalPath = `/products/${detail.slug}`;
  const jsonLd = buildProductStructuredData(detail, canonicalPath);

  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={home?.featuredProducts ?? []}>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
      <StorefrontShopeivaPdp detail={detail} />
    </StorefrontShell>
  );
}
