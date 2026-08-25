import type { Metadata } from "next";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome, loadStorefrontSellers } from "../storefront/storefront-api.ts";
import { StorefrontDirectoryCard } from "../storefront/storefront-merchandising.tsx";

export const metadata: Metadata = { title: "فروشندگان | توبا", description: "فروشندگان دارای Offer فعال در توبا", alternates: { canonical: "/sellers" } };

/** دایرکتوری عمومی فروشندگان Shopeiva را بدون افشای PartyId می‌سازد. */
export default async function SellersPage() {
  const [home, sellers] = await Promise.all([loadStorefrontHome(), loadStorefrontSellers()]);
  return (
    <StorefrontShell categories={home?.categories ?? []}>
      <div className="py-6">
        <h1 className="text-2xl font-black mb-2">فروشندگان</h1>
        <p className="text-sm text-gray-500 mb-5">فروشندگان دارای پیشنهاد فعال و قابل نمایش</p>
        {sellers?.length ? <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {sellers.map((seller) => <StorefrontDirectoryCard key={seller.publicId} href={`/seller-profile/${seller.publicId}`} title={seller.displayName} meta={`${seller.productCount.toLocaleString("fa-IR")} کالا · ${seller.activeOfferCount.toLocaleString("fa-IR")} پیشنهاد فعال`} />)}
        </div> : <div className="sf-empty">فروشندهٔ عمومی فعالی وجود ندارد.</div>}
      </div>
    </StorefrontShell>
  );
}
