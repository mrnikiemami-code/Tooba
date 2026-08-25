import type { Metadata } from "next";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { StorefrontShopeivaListing } from "../storefront/storefront-listing.tsx";
import { loadStorefrontListing } from "../storefront/storefront-api.ts";
import type { StorefrontListingSort } from "../storefront/storefront-model.ts";

type ProductsSearchParams = {
  q?: string;
  categoryId?: string;
  sellerPartyId?: string;
  inStock?: string;
  sort?: string;
  page?: string;
};

function readSort(value?: string): StorefrontListingSort {
  return value === "newest" || value === "price-asc" || value === "price-desc" ? value : "default";
}

function canonicalPath(params: ProductsSearchParams): string {
  const canonical = new URLSearchParams();
  if (params.categoryId) canonical.set("categoryId", params.categoryId);
  const page = Number(params.page);
  if (Number.isInteger(page) && page > 1) canonical.set("page", String(page));
  const suffix = canonical.toString();
  return suffix ? `/products?${suffix}` : "/products";
}

/**
 * جستجو و facetهای تجاری صفحهٔ نتیجه index نمی‌شوند؛ فهرست عمومی و landing رده canonical خود را حفظ می‌کنند.
 */
export async function generateMetadata({
  searchParams,
}: {
  searchParams: Promise<ProductsSearchParams>;
}): Promise<Metadata> {
  const params = await searchParams;
  const filtered = Boolean(params.q || params.sellerPartyId || params.inStock || (params.sort && params.sort !== "default"));
  return {
    title: params.q ? `نتیجه جستجوی ${params.q} | توبا` : "فهرست کالاها | توبا",
    description: params.q ? `نتایج زندهٔ جستجوی ${params.q} در فروشگاه توبا` : "کالاهای منتشرشده و قابل عرضه در فروشگاه توبا",
    alternates: { canonical: canonicalPath(params) },
    robots: filtered ? { index: false, follow: true } : { index: true, follow: true },
  };
}

/**
 * فهرست زندهٔ کالا با الگوی PLP قالب. فیلتر رده و جستجو فقط روی دادهٔ Host است.
 */
export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<ProductsSearchParams>;
}) {
  const params = await searchParams;
  const page = Math.max(1, Number.parseInt(params.page ?? "1", 10) || 1);
  const listing = await loadStorefrontListing({
    query: params.q,
    categoryId: params.categoryId,
    sellerPartyId: params.sellerPartyId,
    inStock: params.inStock === "true" ? true : undefined,
    sort: readSort(params.sort),
    page,
  });
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
        sellers={listing.sellers}
        products={listing.products}
        activeCategoryId={params.categoryId}
        activeSellerPartyId={params.sellerPartyId}
        inStock={params.inStock === "true" ? true : undefined}
        sort={listing.sort}
        query={params.q}
        page={listing.page}
        pageSize={listing.pageSize}
        totalCount={listing.totalCount}
      />
    </StorefrontShell>
  );
}
