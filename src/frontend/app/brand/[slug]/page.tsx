import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontBrand, loadStorefrontHome } from "../../storefront/storefront-api.ts";
import { StorefrontMerchandisingGrid } from "../../storefront/storefront-merchandising.tsx";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const page = await loadStorefrontBrand(slug);
  return page ? { title: `${page.brand.name} | توبا`, description: `کالاهای برند ${page.brand.name} در فروشگاه توبا`, alternates: { canonical: `/brand/${slug}` } } : {};
}

/** landing برند Shopeiva را بدون متن بازاریابی ساختگی به Catalog وصل می‌کند. */
export default async function BrandPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const [home, page] = await Promise.all([loadStorefrontHome(), loadStorefrontBrand(slug)]);
  if (!page) notFound();
  return <StorefrontShell categories={home?.categories ?? []}><StorefrontMerchandisingGrid title={page.brand.name} description="کالاهای منتشرشده و دارای Offer فعال این برند" products={page.products} /></StorefrontShell>;
}
