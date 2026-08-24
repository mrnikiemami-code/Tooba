import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { StorefrontShopeivaListing } from "../storefront/storefront-listing.tsx";
import { loadStorefrontListing } from "../storefront/storefront-api.ts";

/**
 * فهرست زندهٔ کالا با الگوی PLP قالب. فیلتر رده و جستجو فقط روی دادهٔ Host است.
 */
export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<{ q?: string; categoryId?: string }>;
}) {
  const params = await searchParams;
  const listing = await loadStorefrontListing(params.q, params.categoryId);
  if (!listing) {
    return (
      <StorefrontShell categories={[]}>
        <div className="py-16 text-center bg-white rounded-2xl mt-6">فهرست زنده در دسترس نیست.</div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={listing.categories} searchCatalog={listing.products}>
      <StorefrontShopeivaListing
        title={params.q ? `نتیجه جستجو برای «${params.q}»` : "همه کالاها"}
        categories={listing.categories}
        products={listing.products}
        activeCategoryId={params.categoryId}
        query={params.q}
      />
    </StorefrontShell>
  );
}
