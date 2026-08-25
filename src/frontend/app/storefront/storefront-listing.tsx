import Link from "next/link";
import { SlidersHorizontal } from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type {
  StorefrontCategoryItem,
  StorefrontListingSort,
  StorefrontProductCard,
  StorefrontSellerFilterItem,
} from "./storefront-model.ts";

/**
 * فهرست Shopeiva با نوار فیلتر/مرتب‌سازی و شبکه کارت. داده فقط از Host است.
 */
export function StorefrontShopeivaListing({
  title,
  categories,
  sellers,
  products,
  activeCategoryId,
  activeSellerPartyId,
  inStock,
  sort,
  query,
  page,
  pageSize,
  totalCount,
}: {
  title: string;
  categories: StorefrontCategoryItem[];
  sellers: StorefrontSellerFilterItem[];
  products: StorefrontProductCard[];
  activeCategoryId?: string;
  activeSellerPartyId?: string;
  inStock?: boolean;
  sort: StorefrontListingSort;
  query?: string;
  page: number;
  pageSize: number;
  totalCount: number;
}) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  const listingHref = (overrides: Record<string, string | undefined>) => {
    const params = new URLSearchParams();
    const values = {
      q: query,
      categoryId: activeCategoryId,
      sellerPartyId: activeSellerPartyId,
      inStock: inStock === undefined ? undefined : String(inStock),
      sort: sort === "default" ? undefined : sort,
      ...overrides,
    };
    Object.entries(values).forEach(([key, value]) => {
      if (value) params.set(key, value);
    });
    const suffix = params.toString();
    return suffix ? `/products?${suffix}` : "/products";
  };

  return (
    <div className="py-4 md:py-6">
      <div className="text-xs text-gray-500 mb-3 flex gap-2">
        <Link href="/" className="hover:text-[#2563EB]">
          خانه
        </Link>
        <span>/</span>
        <span>کالاها</span>
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
        <aside className="lg:col-span-3 bg-white rounded-2xl border border-gray-200 p-4 h-fit">
          <h2 className="font-bold text-sm mb-3 flex items-center gap-2">
            <SlidersHorizontal className="w-4 h-4 text-[#2563EB]" />
            فیلترها
          </h2>
          <p className="text-[11px] text-gray-500 mb-3">فقط فیلترهای دارای دادهٔ واقعی نمایش داده می‌شوند.</p>
          <div className="space-y-1">
            <Link href={listingHref({ categoryId: undefined, page: undefined })} className={`block px-3 py-2 rounded-xl text-sm ${!activeCategoryId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"}`}>
              همه کالاها
            </Link>
            {categories.map((category) => (
              <Link
                key={category.categoryId}
                href={listingHref({ categoryId: category.categoryId, page: undefined })}
                className={`block px-3 py-2 rounded-xl text-sm ${
                  activeCategoryId === category.categoryId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"
                }`}
              >
                {category.name}
              </Link>
            ))}
          </div>
          <div className="border-t border-gray-100 mt-4 pt-4">
            <h3 className="text-xs font-bold mb-2">موجودی</h3>
            <Link
              href={listingHref({ inStock: inStock ? undefined : "true", page: undefined })}
              className={`block px-3 py-2 rounded-xl text-sm ${inStock ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"}`}
            >
              فقط کالاهای موجود
            </Link>
          </div>
          {sellers.length > 1 ? (
            <div className="border-t border-gray-100 mt-4 pt-4">
              <h3 className="text-xs font-bold mb-2">فروشنده</h3>
              {sellers.map((seller) => (
                <Link
                  key={seller.sellerPartyId}
                  href={listingHref({
                    sellerPartyId: activeSellerPartyId === seller.sellerPartyId ? undefined : seller.sellerPartyId,
                    page: undefined,
                  })}
                  className={`block px-3 py-2 rounded-xl text-sm ${
                    activeSellerPartyId === seller.sellerPartyId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"
                  }`}
                >
                  {seller.displayName}
                </Link>
              ))}
            </div>
          ) : null}
        </aside>
        <section className="lg:col-span-9">
          <div className="bg-white rounded-2xl border border-gray-200 p-3 mb-4 flex flex-wrap items-center justify-between gap-3">
            <h1 className="text-base md:text-lg font-black">{title}</h1>
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <span>{totalCount.toLocaleString("fa-IR")} کالا</span>
              <form action="/products" method="get" className="flex items-center gap-1">
                {query ? <input type="hidden" name="q" value={query} /> : null}
                {activeCategoryId ? <input type="hidden" name="categoryId" value={activeCategoryId} /> : null}
                {activeSellerPartyId ? <input type="hidden" name="sellerPartyId" value={activeSellerPartyId} /> : null}
                {inStock ? <input type="hidden" name="inStock" value="true" /> : null}
                <select name="sort" className="border border-gray-200 rounded-xl px-2 py-1 bg-white" defaultValue={sort} aria-label="مرتب‌سازی">
                  <option value="default">پیش‌فرض</option>
                  <option value="newest">جدیدترین</option>
                  <option value="price-asc">ارزان‌ترین</option>
                  <option value="price-desc">گران‌ترین</option>
                </select>
                <button type="submit" className="px-2 py-1 rounded-lg bg-gray-100">اعمال</button>
              </form>
            </div>
          </div>
          {query ? <p className="text-sm text-gray-500 mb-3">جستجو برای «{query}»</p> : null}
          {products.length === 0 ? (
            <div className="bg-white rounded-2xl border p-10 text-center text-gray-500">
              {query ? `برای «${query}» کالایی پیدا نشد.` : "کالایی با این فیلتر پیدا نشد."}
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-3">
              {products.map((card) => (
                <StorefrontProductCardView key={card.productId} card={card} />
              ))}
            </div>
          )}
          {pageCount > 1 ? (
            <nav className="mt-5 flex justify-center gap-2" aria-label="صفحه‌بندی نتایج">
              {page > 1 ? <Link href={listingHref({ page: String(page - 1) })} className="px-4 py-2 bg-white border rounded-xl">قبلی</Link> : null}
              <span className="px-4 py-2 text-sm">صفحه {page.toLocaleString("fa-IR")} از {pageCount.toLocaleString("fa-IR")}</span>
              {page < pageCount ? <Link href={listingHref({ page: String(page + 1) })} className="px-4 py-2 bg-white border rounded-xl">بعدی</Link> : null}
            </nav>
          ) : null}
        </section>
      </div>
    </div>
  );
}
