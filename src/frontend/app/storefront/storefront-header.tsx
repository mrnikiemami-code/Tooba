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
import type { StorefrontCategoryItem } from "./storefront-model.ts";
import { CART_CHANGED_EVENT, loadStorefrontCart } from "./storefront-cart-api.ts";

/**
 * هدر Shopeiva با نوار پرومو، جستجو، مگامنوی رده‌ای زنده Catalog و سبد.
 * مگامنو hierarchy رده است؛ کارت محصول و قیمت داخل مگامنو نمی‌آید.
 */
export function StorefrontShopeivaHeader({
  categories,
}: {
  categories: StorefrontCategoryItem[];
}) {
  const rootCategories = categories.filter((category) => category.parentCategoryId === null);
  const navigationRoots = rootCategories.length > 0 ? rootCategories : categories;
  const [query, setQuery] = useState("");
  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(navigationRoots[0]?.categoryId ?? null);
  const [cartCount, setCartCount] = useState(0);

  useEffect(() => {
    const refreshBadge = () => {
      void loadStorefrontCart()
        .then((cart) => setCartCount(cart?.itemCount ?? 0))
        .catch(() => setCartCount(0));
    };
    refreshBadge();
    window.addEventListener(CART_CHANGED_EVENT, refreshBadge);
    return () => window.removeEventListener(CART_CHANGED_EVENT, refreshBadge);
  }, []);

  useEffect(() => {
    if (categories.length === 0) {
      setSelectedCategoryId(null);
      return;
    }
    if (!selectedCategoryId || !navigationRoots.some((item) => item.categoryId === selectedCategoryId)) {
      setSelectedCategoryId(navigationRoots[0]!.categoryId);
    }
  }, [categories, navigationRoots, selectedCategoryId]);

  const selectedCategory = navigationRoots.find((item) => item.categoryId === selectedCategoryId) ?? navigationRoots[0] ?? null;
  const childCategories = categories.filter((item) => item.parentCategoryId === selectedCategory?.categoryId);

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
            <Link
              href="/cart"
              className="relative w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
              aria-label="سبد خرید"
            >
              <ShoppingBag className="w-5 h-5" />
              <span className="absolute -top-0.5 -left-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-[#2563EB] text-white text-[10px] font-bold flex items-center justify-center">
                {cartCount.toLocaleString("fa-IR")}
              </span>
            </Link>
          </div>
        </div>

        <nav className="hidden lg:flex items-center gap-1 pb-3" aria-label="ناوبری اصلی فروشگاه">
          <div className="relative" onMouseEnter={() => setMegaOpen(true)} onMouseLeave={() => setMegaOpen(false)}>
            <button
              type="button"
              className={`flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-sm font-medium ${
                megaOpen ? "bg-[#2563EB]/15 text-[#2563EB]" : "text-gray-600 hover:bg-gray-50"
              }`}
              aria-expanded={megaOpen}
              aria-controls="storefront-mega-menu"
              onClick={() => setMegaOpen((open) => !open)}
            >
              <Grid3X3 className="w-4 h-4" />
              دسته‌بندی‌ها
              <ChevronDown className={`w-3.5 h-3.5 ${megaOpen ? "rotate-180" : ""}`} />
            </button>
            {megaOpen ? (
              <div
                id="storefront-mega-menu"
                className="absolute right-0 top-full z-50 w-[min(1100px,90vw)] bg-white shadow-2xl border border-gray-200 rounded-2xl p-5"
              >
                <div className="grid grid-cols-12 gap-5">
                  <div className="col-span-3 space-y-1 max-h-[380px] overflow-auto">
                    {categories.length === 0 ? (
                      <p className="text-xs text-gray-400 px-3 py-2">رده‌ای از Catalog نیست.</p>
                    ) : (
                      navigationRoots.map((cat) => (
                        <button
                          key={cat.categoryId}
                          type="button"
                          onMouseEnter={() => setSelectedCategoryId(cat.categoryId)}
                          className={`w-full text-right px-3 py-2 rounded-lg text-xs ${
                            selectedCategory?.categoryId === cat.categoryId
                              ? "bg-[#2563EB] text-white"
                              : "text-gray-600 hover:bg-gray-50"
                          }`}
                        >
                          {cat.name}
                        </button>
                      ))
                    )}
                  </div>
                  <div className="col-span-6 border-x border-gray-100 px-4 max-h-[380px] overflow-auto">
                    <p className="font-bold text-sm mb-3">{selectedCategory?.name ?? "یک رده را انتخاب کنید"}</p>
                    {selectedCategory ? (
                      <Link
                        href={`/products?categoryId=${selectedCategory.categoryId}`}
                        className="inline-flex mb-4 text-[11px] font-bold text-[#2563EB]"
                      >
                        مشاهده همهٔ این رده
                      </Link>
                    ) : null}
                    <div className="grid grid-cols-2 gap-x-6 gap-y-4">
                      {childCategories.map((sub) => (
                        <div key={sub.categoryId}>
                          <Link
                            href={`/products?categoryId=${sub.categoryId}`}
                            className="text-xs font-bold text-gray-800 hover:text-[#2563EB]"
                          >
                            {sub.name}
                          </Link>
                          <div className="mt-1 space-y-1">
                            {categories
                              .filter((category) => category.parentCategoryId === sub.categoryId)
                              .map((child) => (
                                <Link
                                  key={child.categoryId}
                                  href={`/products?categoryId=${child.categoryId}`}
                                  className="block text-[11px] text-gray-400 hover:text-[#2563EB]"
                                >
                                  {child.name}
                                </Link>
                              ))}
                          </div>
                        </div>
                      ))}
                    </div>
                    <div className="mt-4 flex flex-wrap gap-2">
                      {childCategories.map((category) => (
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
                      <p className="text-[11px] opacity-80 mt-1">رده‌های زنده Catalog در پوستهٔ Shopeiva</p>
                      <Link href="/products" className="mt-3 inline-block px-4 py-1.5 bg-white text-[#2563EB] rounded-xl text-[11px] font-bold">
                        مشاهده کالاها
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            ) : null}
          </div>
          {navigationRoots.map((category) => (
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
            <form action="/products" method="get" className="mb-4">
              <div className="relative">
                <input
                  name="q"
                  placeholder="جستجو در کالاها..."
                  className="w-full bg-gray-50 border border-gray-200 rounded-2xl py-2.5 pr-10 pl-3 text-sm"
                  aria-label="جستجوی کالا در موبایل"
                />
                <Search className="absolute right-3 top-3 w-4 h-4 text-gray-400" />
              </div>
            </form>
            {navigationRoots.map((category) => (
              <div key={category.categoryId} className="border-b py-2">
                <Link
                  href={`/products?categoryId=${category.categoryId}`}
                  className="block text-sm font-bold"
                  onClick={() => setMobileOpen(false)}
                >
                  {category.name}
                </Link>
                {categories
                  .filter((child) => child.parentCategoryId === category.categoryId)
                  .map((child) => (
                    <Link
                      key={child.categoryId}
                      href={`/products?categoryId=${child.categoryId}`}
                      className="block py-1 pe-3 text-xs text-gray-500"
                      onClick={() => setMobileOpen(false)}
                    >
                      {child.name}
                    </Link>
                  ))}
              </div>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}
