import Link from "next/link";
import { SlidersHorizontal } from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";

/**
 * فهرست Shopeiva با نوار فیلتر/مرتب‌سازی و شبکه کارت. داده فقط از Host است.
 */
export function StorefrontShopeivaListing({
  title,
  categories,
  products,
  activeCategoryId,
  query,
}: {
  title: string;
  categories: StorefrontCategoryItem[];
  products: StorefrontProductCard[];
  activeCategoryId?: string;
  query?: string;
}) {
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
          <p className="text-[11px] text-gray-500 mb-3">رده از Catalog زنده است. Facet قیمت هنوز به Pricing وصل نیست.</p>
          <div className="space-y-1">
            <Link href="/products" className={`block px-3 py-2 rounded-xl text-sm ${!activeCategoryId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"}`}>
              همه کالاها
            </Link>
            {categories.map((category) => (
              <Link
                key={category.categoryId}
                href={`/products?categoryId=${category.categoryId}`}
                className={`block px-3 py-2 rounded-xl text-sm ${
                  activeCategoryId === category.categoryId ? "bg-[#2563EB] text-white" : "hover:bg-gray-50"
                }`}
              >
                {category.name}
              </Link>
            ))}
          </div>
        </aside>
        <section className="lg:col-span-9">
          <div className="bg-white rounded-2xl border border-gray-200 p-3 mb-4 flex flex-wrap items-center justify-between gap-3">
            <h1 className="text-base md:text-lg font-black">{title}</h1>
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <span>{products.length.toLocaleString("fa-IR")} کالا</span>
              <select className="border border-gray-200 rounded-xl px-2 py-1 bg-white" defaultValue="default" aria-label="مرتب‌سازی ظاهری">
                <option value="default">پیش‌فرض</option>
                <option value="new">جدیدترین</option>
                <option value="price">قیمت</option>
              </select>
            </div>
          </div>
          {query ? <p className="text-sm text-gray-500 mb-3">جستجو برای «{query}»</p> : null}
          {products.length === 0 ? (
            <div className="bg-white rounded-2xl border p-10 text-center text-gray-500">کالایی با این فیلتر پیدا نشد.</div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-3">
              {products.map((card) => (
                <StorefrontProductCardView key={card.productId} card={card} />
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
