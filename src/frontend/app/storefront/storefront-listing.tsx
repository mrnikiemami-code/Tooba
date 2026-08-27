"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useMemo, useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Layers,
  Package,
  Search,
  SlidersHorizontal,
  X,
} from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type {
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontListingSort,
  StorefrontProductCard,
  StorefrontSellerFilterItem,
} from "./storefront-model.ts";

/**
 * PLP خانوادهٔ Shopeiva: سایدبار فیلتر، نوار مرتب‌سازی، شبکه کارت و صفحه‌بندی؛ داده فقط از Host.
 */
export function StorefrontShopeivaListing({
  title,
  categories,
  brands = [],
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
  brands?: StorefrontBrandItem[];
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
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  const activeCategory = useMemo(
    () => categories.find((category) => category.categoryId === activeCategoryId) ?? null,
    [categories, activeCategoryId],
  );
  const childCategories = useMemo(() => {
    if (!activeCategoryId) {
      return categories.filter((category) => category.parentCategoryId == null).slice(0, 24);
    }
    return categories.filter((category) => category.parentCategoryId === activeCategoryId);
  }, [categories, activeCategoryId]);

  const listingHref = (overrides: Record<string, string | undefined>) => {
    const params = new URLSearchParams();
    const values = {
      q: query,
      categoryId: activeCategoryId,
      sellerPartyId: activeSellerPartyId,
      inStock: inStock === undefined ? undefined : String(inStock),
      sort: sort === "default" ? undefined : sort,
      page: page > 1 ? String(page) : undefined,
      ...overrides,
    };
    Object.entries(values).forEach(([key, value]) => {
      if (value) params.set(key, value);
    });
    const suffix = params.toString();
    return suffix ? `/products?${suffix}` : "/products";
  };

  const filterPanel = (
    <div className="space-y-4" data-testid="listing-filter-sidebar">
      <h2 className="font-bold text-sm flex items-center gap-2 text-gray-900">
        <Layers className="w-4 h-4 text-[#2563EB]" />
        {activeCategory ? "زیرمجموعه‌ها" : "فیلترها"}
      </h2>

      <div className="space-y-1">
        <Link
          href={listingHref({ categoryId: undefined, page: undefined })}
          className={`flex items-center justify-between px-3 py-2.5 rounded-xl text-sm transition-colors ${
            !activeCategoryId ? "bg-[#2563EB] text-white shadow-lg shadow-[#2563EB]/20" : "text-gray-700 hover:bg-gray-50"
          }`}
          onClick={() => setMobileFiltersOpen(false)}
        >
          <span>همه</span>
          <span className="text-[10px] opacity-70">({categories.length.toLocaleString("fa-IR")})</span>
        </Link>
        {childCategories.map((category) => (
          <Link
            key={category.categoryId}
            href={listingHref({ categoryId: category.categoryId, page: undefined })}
            data-testid="listing-category-filter"
            className={`flex items-center justify-between px-3 py-2.5 rounded-xl text-sm transition-colors ${
              activeCategoryId === category.categoryId
                ? "bg-[#2563EB] text-white shadow-lg shadow-[#2563EB]/20"
                : "text-gray-700 hover:bg-gray-50"
            }`}
            onClick={() => setMobileFiltersOpen(false)}
          >
            <span>{category.name}</span>
            <ChevronLeft className="w-3 h-3 opacity-50" />
          </Link>
        ))}
      </div>

      <div className="border-t border-gray-100 pt-4">
        <h3 className="text-xs font-bold mb-2 text-gray-800">موجودی</h3>
        <Link
          href={listingHref({ inStock: inStock ? undefined : "true", page: undefined })}
          className={`block px-3 py-2.5 rounded-xl text-sm ${inStock ? "bg-[#2563EB] text-white" : "hover:bg-gray-50 text-gray-700"}`}
          onClick={() => setMobileFiltersOpen(false)}
        >
          فقط کالاهای موجود
        </Link>
      </div>

      {brands.length > 0 ? (
        <div className="border-t border-gray-100 pt-4">
          <h3 className="text-xs font-bold mb-2 text-gray-800">برند</h3>
          <div className="max-h-48 overflow-y-auto space-y-1">
            {brands.slice(0, 16).map((brand) => (
              <Link
                key={brand.brandId}
                href={`/brand/${brand.slug}`}
                className="block px-3 py-2 rounded-xl text-sm text-gray-700 hover:bg-gray-50"
                onClick={() => setMobileFiltersOpen(false)}
              >
                {brand.name}
                <span className="text-[10px] text-gray-400 mr-1">({brand.productCount.toLocaleString("fa-IR")})</span>
              </Link>
            ))}
          </div>
        </div>
      ) : null}

      {sellers.length > 1 ? (
        <div className="border-t border-gray-100 pt-4">
          <h3 className="text-xs font-bold mb-2 text-gray-800">فروشنده</h3>
          <div className="max-h-40 overflow-y-auto space-y-1">
            {sellers.map((seller) => (
              <Link
                key={seller.sellerPartyId}
                href={listingHref({
                  sellerPartyId: activeSellerPartyId === seller.sellerPartyId ? undefined : seller.sellerPartyId,
                  page: undefined,
                })}
                className={`block px-3 py-2 rounded-xl text-sm ${
                  activeSellerPartyId === seller.sellerPartyId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50 text-gray-700"
                }`}
                onClick={() => setMobileFiltersOpen(false)}
              >
                {seller.displayName}
              </Link>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );

  const pageWindow = useMemo(() => {
    const start = Math.max(1, page - 2);
    const end = Math.min(pageCount, start + 4);
    return Array.from({ length: end - start + 1 }, (_, index) => start + index);
  }, [page, pageCount]);

  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const rangeEnd = Math.min(page * pageSize, totalCount);

  return (
    <div className="min-h-[50vh] bg-white" data-testid="storefront-listing">
      <div className="max-w-[1800px] mx-auto px-2 sm:px-4 py-6">
        <nav className="text-xs text-gray-500 mb-6 flex flex-wrap gap-2" aria-label="مسیر صفحه" data-testid="listing-breadcrumb">
          <Link href="/" className="hover:text-[#2563EB]">
            خانه
          </Link>
          <span>/</span>
          <Link href="/products" className="hover:text-[#2563EB]">
            کالاها
          </Link>
          {activeCategory ? (
            <>
              <span>/</span>
              <span className="text-gray-800">{activeCategory.name}</span>
            </>
          ) : null}
          {query ? (
            <>
              <span>/</span>
              <span className="text-gray-800">جستجو</span>
            </>
          ) : null}
        </nav>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
          <aside className="hidden lg:block lg:col-span-1">
            <div className="bg-white rounded-2xl border border-gray-200 p-4 sticky top-24">{filterPanel}</div>
          </aside>

          <section className="lg:col-span-3 space-y-4">
            {activeCategory || query ? (
              <div
                className="bg-gradient-to-br from-gray-50 to-white rounded-2xl p-4 md:p-6 border border-gray-200"
                data-testid="listing-category-header"
              >
                <div className="flex flex-col md:flex-row items-center gap-4">
                  <div className="w-20 h-20 md:w-24 md:h-24 rounded-2xl bg-gray-100 flex items-center justify-center shrink-0">
                    <Package className="w-8 h-8 text-[#2563EB]" />
                  </div>
                  <div className="flex-1 text-center md:text-right space-y-2">
                    <div className="flex items-center justify-center md:justify-start gap-2 flex-wrap">
                      <h1 className="text-xl md:text-2xl font-extrabold text-gray-900">{title}</h1>
                      <span className="text-xs bg-[#2563EB]/10 text-[#2563EB] px-2.5 py-1 rounded-full font-bold">
                        {totalCount.toLocaleString("fa-IR")} محصول
                      </span>
                    </div>
                    {query ? <p className="text-sm text-gray-500">نتایج زنده برای «{query}»</p> : null}
                  </div>
                </div>
              </div>
            ) : (
              <h1 className="text-xl md:text-2xl font-extrabold text-gray-900" data-testid="listing-title">
                {title}
              </h1>
            )}

            <div
              className="bg-white rounded-2xl border border-gray-200 p-3 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
              data-testid="listing-sort-toolbar"
            >
              <div className="flex items-center gap-2 flex-wrap">
                <button
                  type="button"
                  className="lg:hidden inline-flex items-center gap-2 px-3 py-2 rounded-xl border border-gray-200 text-sm font-medium"
                  data-testid="listing-mobile-filter-open"
                  onClick={() => setMobileFiltersOpen(true)}
                >
                  <SlidersHorizontal className="w-4 h-4 text-[#2563EB]" />
                  فیلترها
                </button>
                <span className="text-xs text-gray-500 whitespace-nowrap" data-testid="listing-result-count">
                  {totalCount.toLocaleString("fa-IR")} محصول
                  {query ? ` برای «${query}»` : ""}
                </span>
              </div>
              <form action="/products" method="get" className="flex items-center gap-2">
                {query ? <input type="hidden" name="q" value={query} /> : null}
                {activeCategoryId ? <input type="hidden" name="categoryId" value={activeCategoryId} /> : null}
                {activeSellerPartyId ? <input type="hidden" name="sellerPartyId" value={activeSellerPartyId} /> : null}
                {inStock ? <input type="hidden" name="inStock" value="true" /> : null}
                <label className="sr-only" htmlFor="listing-sort">
                  مرتب‌سازی
                </label>
                <select
                  id="listing-sort"
                  name="sort"
                  className="px-3 py-2.5 bg-white rounded-xl text-sm text-gray-700 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB]"
                  defaultValue={sort}
                  aria-label="مرتب‌سازی"
                  data-testid="listing-sort-select"
                >
                  <option value="default">پیش‌فرض</option>
                  <option value="newest">جدیدترین</option>
                  <option value="price-asc">ارزان‌ترین</option>
                  <option value="price-desc">گران‌ترین</option>
                </select>
                <button type="submit" className="px-3 py-2.5 rounded-xl bg-gray-100 text-sm font-medium">
                  اعمال
                </button>
              </form>
            </div>

            {products.length === 0 ? (
              <div className="text-center py-12 border border-gray-200 rounded-2xl" data-testid="listing-empty">
                <div className="w-20 h-20 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-4">
                  <Search className="w-10 h-10 text-gray-400" />
                </div>
                <h2 className="text-lg font-bold text-gray-900">محصولی یافت نشد</h2>
                <p className="text-sm text-gray-500 mt-2">
                  {query ? `برای «${query}» کالایی پیدا نشد.` : "کالایی با این فیلتر پیدا نشد."}
                </p>
              </div>
            ) : (
              <div
                className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-3 md:gap-4"
                data-testid="listing-product-grid"
              >
                {products.map((card) => (
                  <StorefrontProductCardView key={card.productId} card={card} />
                ))}
              </div>
            )}

            {pageCount > 1 ? (
              <nav className="flex flex-col items-center gap-3 mt-2" aria-label="صفحه‌بندی نتایج" data-testid="listing-pagination">
                <div className="flex items-center gap-1.5">
                  {page > 1 ? (
                    <Link
                      href={listingHref({ page: String(page - 1) })}
                      className="flex items-center justify-center w-10 h-10 rounded-lg text-gray-500 hover:bg-gray-100"
                      aria-label="صفحه قبل"
                    >
                      <ChevronRight className="w-5 h-5" />
                    </Link>
                  ) : null}
                  {pageWindow.map((pageNumber) => (
                    <Link
                      key={pageNumber}
                      href={listingHref({ page: pageNumber === 1 ? undefined : String(pageNumber) })}
                      className={`flex items-center justify-center w-8 h-8 rounded-lg text-sm font-medium ${
                        pageNumber === page ? "bg-[#2563EB] text-white shadow-lg shadow-[#2563EB]/30" : "text-gray-700 hover:bg-gray-100"
                      }`}
                    >
                      {pageNumber.toLocaleString("fa-IR")}
                    </Link>
                  ))}
                  {page < pageCount ? (
                    <Link
                      href={listingHref({ page: String(page + 1) })}
                      className="flex items-center justify-center w-10 h-10 rounded-lg text-gray-500 hover:bg-gray-100"
                      aria-label="صفحه بعد"
                    >
                      <ChevronLeft className="w-5 h-5" />
                    </Link>
                  ) : null}
                </div>
                <p className="text-xs text-gray-500">
                  نمایش {rangeStart.toLocaleString("fa-IR")} تا {rangeEnd.toLocaleString("fa-IR")} از{" "}
                  {totalCount.toLocaleString("fa-IR")} محصول
                </p>
              </nav>
            ) : null}
          </section>
        </div>
      </div>

      {mobileFiltersOpen ? (
        <div className="fixed inset-0 z-[60] lg:hidden" data-testid="listing-mobile-filter-drawer">
          <button type="button" className="absolute inset-0 bg-black/50" aria-label="بستن فیلترها" onClick={() => setMobileFiltersOpen(false)} />
          <div className="absolute inset-y-0 right-0 w-[85%] max-w-sm bg-white shadow-2xl p-4 overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <strong className="text-sm">فیلترها</strong>
              <button type="button" className="w-9 h-9 rounded-full bg-gray-100 flex items-center justify-center" onClick={() => setMobileFiltersOpen(false)} aria-label="بستن">
                <X className="w-4 h-4" />
              </button>
            </div>
            {filterPanel}
          </div>
        </div>
      ) : null}
    </div>
  );
}
