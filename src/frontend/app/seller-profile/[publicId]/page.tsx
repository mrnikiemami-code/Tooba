import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { loadStorefrontHome, loadStorefrontSeller } from "../../storefront/storefront-api.ts";
import { StorefrontMerchandisingGrid } from "../../storefront/storefront-merchandising.tsx";

export async function generateMetadata({ params }: { params: Promise<{ publicId: string }> }): Promise<Metadata> {
  const { publicId } = await params;
  const page = await loadStorefrontSeller(publicId);
  return page ? { title: `${page.seller.displayName} | فروشندگان توبا`, description: `کالاهای فعال ${page.seller.displayName} در توبا`, alternates: { canonical: `/seller-profile/${publicId}` } } : {};
}

/** پروفایل فروشندهٔ Shopeiva را فقط با هویت و Offerهای عمومی پر می‌کند. */
export default async function SellerProfilePage({ params }: { params: Promise<{ publicId: string }> }) {
  const { publicId } = await params;
  const [home, page] = await Promise.all([loadStorefrontHome(), loadStorefrontSeller(publicId)]);
  if (!page) notFound();
  return <StorefrontShell categories={home?.categories ?? []}><StorefrontMerchandisingGrid title={page.seller.displayName} description={`${page.seller.productCount.toLocaleString("fa-IR")} کالای دارای پیشنهاد فعال`} products={page.products} /></StorefrontShell>;
}
