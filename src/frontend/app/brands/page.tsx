import type { Metadata } from "next";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontBrands, loadStorefrontHome } from "../storefront/storefront-api.ts";
import { StorefrontDirectoryCard } from "../storefront/storefront-merchandising.tsx";

export const metadata: Metadata = { title: "برندها | توبا", description: "برندهای منتشرشدهٔ فروشگاه توبا", alternates: { canonical: "/brands" } };

/** فهرست برند Shopeiva را با برندهای منتشرشدهٔ Catalog پر می‌کند. */
export default async function BrandsPage() {
  const [home, brands] = await Promise.all([loadStorefrontHome(), loadStorefrontBrands()]);
  return (
    <StorefrontShell categories={home?.categories ?? []}>
      <div className="py-6">
        <h1 className="text-2xl font-black mb-2">برندها</h1>
        <p className="text-sm text-gray-500 mb-5">برندهای منتشرشده و کالاهای زندهٔ آن‌ها</p>
        {brands?.length ? <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
          {brands.map((brand) => <StorefrontDirectoryCard key={brand.brandId} href={`/brand/${brand.slug}`} title={brand.name} meta={`${brand.productCount.toLocaleString("fa-IR")} کالا`} />)}
        </div> : <div className="sf-empty">برند منتشرشده‌ای وجود ندارد.</div>}
      </div>
    </StorefrontShell>
  );
}
