"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  ChevronDown,
  Grid3X3,
  Heart,
  Menu,
  Search,
  ShoppingBag,
  Sparkles,
  Tag,
  User,
  X,
} from "lucide-react";
import type { StorefrontCategoryItem, StorefrontProductCard } from "./storefront-model.ts";
import { formatOfferAmount } from "./storefront-api.ts";

type MegaCategory = {
  id: number;
  name: string;
  subcategories?: { id: number; name: string; items?: string[] }[];
};

/**
 * هدر Shopeiva با نوار پرومو، جستجو، مگامنو و سبد خالی. رده و پیشنهاد جستجو از Tooba زنده است.
 */
export function StorefrontShopeivaHeader({
  categories,
  searchCatalog,
}: {
  categories: StorefrontCategoryItem[];
  searchCatalog: StorefrontProductCard[];
}) {
  const [query, setQuery] = useState("");
  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [cartOpen, setCartOpen] = useState(false);
  const [chromeCategories, setChromeCategories] = useState<MegaCategory[]>([]);
  const [selectedMega, setSelectedMega] = useState<MegaCategory | null>(null);

  useEffect(() => {
    void fetch("/jsons/menuCategories.json")
      .then((response) => (response.ok ? response.json() : null))
      .then((payload: { categories?: Array<MegaCategory & { subcategories?: MegaCategory["subcategories"] }> } | MegaCategory[] | null) => {
        const raw = Array.isArray(payload) ? payload : payload?.categories ?? [];
        const list = raw.map((cat) => ({
          id: cat.id,
          name: cat.name,
          subcategories: cat.subcategories ?? (cat as { subcategories?: MegaCategory["subcategories"] }).subcategories,
        }));
        setChromeCategories(list);
        setSelectedMega(list[0] ?? null);
      })
      .catch(() => undefined);
  }, []);

  const matches = query.trim()
    ? searchCatalog.filter((item) => item.title.includes(query.trim())).slice(0, 6)
    : [];

  return (
    <div className="w-full bg-white border-b border-gray-200 sticky top-0 z-50">
      <div className="bg-[#2563EB] text-white text-[11px] sm:text-xs">
        <div className="max-w-[1800px] mx-auto px-4 sm:px-6 h-10 flex items-center justify-center gap-3">
          <Sparkles className="w-3.5 h-3.5" />
          <span>ارسال سریع سفارش‌های فروشگاهی · پشتیبانی خرید</span>
        </div>
      </div>

      <div className="max-w-[1800px] mx-auto px-4 sm:px-6">
        <div className="flex items-center gap-2 sm:gap-3 h-16 md:h-20">
          <button
            type="button"
            className="lg:hidden w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
            onClick={() => setMobileOpen(true)}
            aria-label="منوی موبایل"
          >
            <Menu className="w-5 h-5" />
          </button>

          <Link href="/" className="shrink-0 flex items-center gap-2">
            {/* لوگوی قالب خریداری‌شده؛ نام فروشگاه Tooba است. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img src="/images/logos/logo.svg" alt="توبا" className="h-9 sm:h-11 w-auto" />
            <span className="hidden md:block font-black text-xl text-[#2563EB]">توبا</span>
          </Link>

          <form action="/products" method="get" className="hidden lg:block flex-1 max-w-xl mx-4">
            <div className="relative">
              <input
                name="q"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder="جستجو در کالاهای فروشگاه..."
                className="w-full bg-gray-50 border border-gray-200 rounded-2xl py-2.5 pr-11 pl-4 text-sm focus:outline-none focus:border-[#2563EB] focus:ring-2 focus:ring-[#2563EB]/20"
                aria-label="جستجوی کالا"
              />
              <Search className="absolute right-3.5 top-2.5 w-4 h-4 text-gray-400" />
              {matches.length > 0 ? (
                <div className="absolute top-full mt-2 right-0 left-0 bg-white border border-gray-200 rounded-2xl shadow-xl z-50 overflow-hidden">
                  {matches.map((item) => (
                    <Link
                      key={item.productId}
                      href={`/products/${item.slug}`}
                      className="flex items-center justify-between px-4 py-3 text-sm hover:bg-gray-50 border-b last:border-0"
                    >
                      <span>{item.title}</span>
                      <span className="text-[#2563EB] font-bold text-xs">
                        {formatOfferAmount(item.offerAmountExclusiveOfTax, item.currency)}
                      </span>
                    </Link>
                  ))}
                </div>
              ) : null}
            </div>
          </form>

          <div className="ms-auto flex items-center gap-1 sm:gap-2">
            <Link
              href="/products"
              className="lg:hidden w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
              aria-label="جستجو"
            >
              <Search className="w-5 h-5" />
            </Link>
            <button type="button" className="w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600" aria-label="علاقه‌مندی">
              <Heart className="w-5 h-5" />
            </button>
            <Link href="/admin/products" className="hidden sm:flex items-center gap-1 px-3 py-2 rounded-xl text-sm text-gray-600 hover:bg-gray-50">
              <User className="w-4 h-4" />
              میزکار
            </Link>
            <button
              type="button"
              onClick={() => setCartOpen(true)}
              className="relative w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
              aria-label="سبد خرید"
            >
              <ShoppingBag className="w-5 h-5" />
              <span className="absolute -top-0.5 -left-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-[#2563EB] text-white text-[10px] font-bold flex items-center justify-center">
                ۰
              </span>
            </button>
          </div>
        </div>

        <nav className="hidden lg:flex items-center gap-1 pb-3">
          <div className="relative" onMouseEnter={() => setMegaOpen(true)} onMouseLeave={() => setMegaOpen(false)}>
            <button
              type="button"
              className={`flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-sm font-medium ${
                megaOpen ? "bg-[#2563EB]/15 text-[#2563EB]" : "text-gray-600 hover:bg-gray-50"
              }`}
            >
              <Grid3X3 className="w-4 h-4" />
              دسته‌بندی‌ها
              <ChevronDown className={`w-3.5 h-3.5 ${megaOpen ? "rotate-180" : ""}`} />
            </button>
            {megaOpen ? (
              <div className="absolute right-0 top-full z-50 w-[min(1100px,90vw)] bg-white shadow-2xl border border-gray-200 rounded-2xl p-5">
                <div className="grid grid-cols-12 gap-5">
                  <div className="col-span-3 space-y-1 max-h-[380px] overflow-auto">
                    {(chromeCategories.length > 0
                      ? chromeCategories
                      : categories.map((item, index) => ({ id: index, name: item.name }))
                    ).map((cat) => (
                      <button
                        key={cat.id}
                        type="button"
                        onMouseEnter={() => setSelectedMega(cat)}
                        className={`w-full text-right px-3 py-2 rounded-lg text-xs ${
                          selectedMega?.id === cat.id ? "bg-[#2563EB] text-white" : "text-gray-600 hover:bg-gray-50"
                        }`}
                      >
                        {cat.name}
                      </button>
                    ))}
                  </div>
                  <div className="col-span-6 border-x border-gray-100 px-4 max-h-[380px] overflow-auto">
                    <p className="font-bold text-sm mb-3">{selectedMega?.name ?? "یک رده را انتخاب کنید"}</p>
                    <div className="grid grid-cols-2 gap-3">
                      {(selectedMega?.subcategories ?? []).slice(0, 8).map((sub) => (
                        <div key={sub.id}>
                          <Link href="/products" className="text-xs font-bold text-gray-800 hover:text-[#2563EB]">
                            {sub.name}
                          </Link>
                          <div className="mt-1 space-y-1">
                            {(sub.items ?? []).slice(0, 4).map((item) => (
                              <Link key={item} href="/products" className="block text-[11px] text-gray-400 hover:text-[#2563EB]">
                                {item}
                              </Link>
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                    <div className="mt-4 flex flex-wrap gap-2">
                      {categories.map((category) => (
                        <Link
                          key={category.categoryId}
                          href={`/products?categoryId=${category.categoryId}`}
                          className="px-2.5 py-1 rounded-lg bg-blue-50 text-[#2563EB] text-[11px] font-medium"
                        >
                          {category.name}
                        </Link>
                      ))}
                    </div>
                  </div>
                  <div className="col-span-3">
                    <div className="rounded-2xl overflow-hidden bg-gradient-to-br from-[#2563EB] to-[#1e40af] text-white p-4 text-center">
                      <Tag className="w-5 h-5 mx-auto mb-2" />
                      <p className="font-bold text-sm">پیشنهادهای فروشگاه</p>
                      <p className="text-[11px] opacity-80 mt-1">کالاهای زنده Tooba در ویترین Shopeiva</p>
                      <Link href="/products" className="mt-3 inline-block px-4 py-1.5 bg-white text-[#2563EB] rounded-xl text-[11px] font-bold">
                        مشاهده کالاها
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            ) : null}
          </div>
          {categories.map((category) => (
            <Link
              key={category.categoryId}
              href={`/products?categoryId=${category.categoryId}`}
              className="px-2.5 py-1.5 rounded-lg text-xs text-gray-600 hover:bg-gray-50 whitespace-nowrap"
            >
              {category.name}
            </Link>
          ))}
          <Link href="/products" className="px-2.5 py-1.5 rounded-lg text-xs font-medium text-[#2563EB]">
            همه کالاها
          </Link>
        </nav>
      </div>

      {mobileOpen ? (
        <div className="fixed inset-0 z-[80] bg-black/40 lg:hidden" onClick={() => setMobileOpen(false)}>
          <div className="absolute right-0 top-0 bottom-0 w-80 bg-white p-4" onClick={(event) => event.stopPropagation()}>
            <div className="flex justify-between items-center mb-4">
              <strong>منو</strong>
              <button type="button" onClick={() => setMobileOpen(false)} aria-label="بستن">
                <X className="w-5 h-5" />
              </button>
            </div>
            {categories.map((category) => (
              <Link
                key={category.categoryId}
                href={`/products?categoryId=${category.categoryId}`}
                className="block py-2 text-sm border-b"
                onClick={() => setMobileOpen(false)}
              >
                {category.name}
              </Link>
            ))}
          </div>
        </div>
      ) : null}

      {cartOpen ? (
        <div className="fixed inset-0 z-[90] bg-black/40" onClick={() => setCartOpen(false)}>
          <div className="absolute left-0 top-0 bottom-0 w-full max-w-sm bg-white flex flex-col" onClick={(event) => event.stopPropagation()}>
            <div className="p-4 border-b flex justify-between items-center">
              <h2 className="font-bold flex items-center gap-2">
                <ShoppingBag className="w-5 h-5 text-[#2563EB]" /> سبد خرید (۰)
              </h2>
              <button type="button" onClick={() => setCartOpen(false)} aria-label="بستن سبد">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="flex-1 p-6 text-center text-gray-500 text-sm">سبد خرید هنوز به API سبد Tooba وصل نشده است.</div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
