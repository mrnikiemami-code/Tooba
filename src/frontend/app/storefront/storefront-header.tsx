"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  Award,
  Coffee,
  ChevronDown,
  ChevronLeft,
  Gift,
  Grid3X3,
  Heart,
  Menu,
  Package,
  Search,
  Shirt,
  ShoppingBag,
  Smartphone,
  Sparkles,
  Star,
  Store,
  Tag,
  TrendingUp,
  Truck,
  User,
  Watch,
  Wrench,
  X,
} from "lucide-react";
import type { StorefrontBrandItem, StorefrontCategoryItem } from "./storefront-model.ts";
import { loadStorefrontMegaMenu, type StorefrontMegaMenuItem } from "../admin/catalog-mega-menu-api.ts";
import { CART_CHANGED_EVENT, loadStorefrontCart } from "./storefront-cart-api.ts";
import { LocaleSwitcher } from "../../lib/i18n/LocaleSwitcher.tsx";

/**
 * هدر Shopeiva با نوار پرومو، جستجو، مگامنوی رده‌ای زنده Catalog و سبد.
 * مگامنو hierarchy رده است؛ کارت محصول و قیمت داخل مگامنو نمی‌آید.
 */
export function StorefrontShopeivaHeader({
  categories,
}: {
  categories: StorefrontCategoryItem[];
}) {
  const lp = useLocalizedPath();
  const [query, setQuery] = useState("");
  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [mobileOpenCategories, setMobileOpenCategories] = useState<Record<string, boolean>>({ main: true });
  const [megaMenuItems, setMegaMenuItems] = useState<StorefrontMegaMenuItem[]>([]);
  const useConfiguredMenu = megaMenuItems.length > 0;

  useEffect(() => {
    void loadStorefrontMegaMenu("fa-IR").then(setMegaMenuItems);
  }, []);

  type NavRow = StorefrontCategoryItem & { href: string };

  const navigationRoots: NavRow[] = useConfiguredMenu
    ? megaMenuItems
        .filter((item) => item.parentMegaMenuItemId === null)
        .map((item) => ({
          categoryId: item.megaMenuItemId,
          parentCategoryId: item.parentMegaMenuItemId,
          name: item.title,
          href: item.destination,
        }))
    : (() => {
        const roots = categories.filter((category) => category.parentCategoryId === null);
        const list = roots.length > 0 ? roots : categories;
        return list.map((item) => ({
          ...item,
          href: `${lp("/products")}?categoryId=${item.categoryId}`,
        }));
      })();

  const [selectedCategoryId, setSelectedCategoryId] = useState<string | null>(navigationRoots[0]?.categoryId ?? null);
  const [cartCount, setCartCount] = useState(0);
  const [brands, setBrands] = useState<StorefrontBrandItem[]>([]);
  const megaCloseTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

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
    void fetch("/v1/storefront/brands", { cache: "no-store" })
      .then(async (response) => response.ok ? await response.json() as unknown : null)
      .then((payload) => {
        if (!Array.isArray(payload)) return setBrands([]);
        setBrands(payload.flatMap((item): StorefrontBrandItem[] => {
          if (!item || typeof item !== "object") return [];
          const row = item as Record<string, unknown>;
          const brandId = String(row.brandId ?? row.BrandId ?? "");
          const slug = String(row.slug ?? row.Slug ?? "");
          const name = String(row.name ?? row.Name ?? "");
          if (!brandId || !slug || !name) return [];
          const logoRaw = row.logoMediaAssetId ?? row.LogoMediaAssetId;
          return [{
            brandId,
            slug,
            name,
            productCount: Number(row.productCount ?? row.ProductCount ?? 0),
            logoMediaAssetId: logoRaw == null ? null : String(logoRaw),
          }];
        }));
      })
      .catch(() => setBrands([]));
  }, []);

  const closeMegaMenu = useCallback(() => {
    setMegaOpen(false);
    setSelectedCategoryId(null);
  }, []);

  useEffect(() => {
    window.addEventListener("scroll", closeMegaMenu, { passive: true });
    return () => {
      window.removeEventListener("scroll", closeMegaMenu);
      if (megaCloseTimer.current) clearTimeout(megaCloseTimer.current);
    };
  }, [closeMegaMenu]);

  useEffect(() => {
    if (navigationRoots.length === 0) {
      setSelectedCategoryId(null);
      return;
    }
    if (!selectedCategoryId || !navigationRoots.some((item) => item.categoryId === selectedCategoryId)) {
      setSelectedCategoryId(navigationRoots[0]!.categoryId);
    }
  }, [navigationRoots, selectedCategoryId]);

  const selectedCategory = navigationRoots.find((item) => item.categoryId === selectedCategoryId) ?? navigationRoots[0] ?? null;
  const childCategories: NavRow[] = useConfiguredMenu
    ? megaMenuItems
        .filter((item) => item.parentMegaMenuItemId === selectedCategory?.categoryId)
        .map((item) => ({
          categoryId: item.megaMenuItemId,
          parentCategoryId: item.parentMegaMenuItemId,
          name: item.title,
          href: item.destination,
        }))
    : categories
        .filter((item) => item.parentCategoryId === selectedCategory?.categoryId)
        .map((item) => ({ ...item, href: `${lp("/products")}?categoryId=${item.categoryId}` }));

  const categoryIcon = (name: string) => {
    if (name.includes("دیجیتال") || name.includes("موبایل")) return Smartphone;
    if (name.includes("اکسسوری")) return Watch;
    if (name.includes("خانه")) return Coffee;
    if (name.includes("خودرو")) return Truck;
    if (name.includes("ابزار")) return Wrench;
    if (name.includes("مد") || name.includes("پوشاک")) return Shirt;
    return Package;
  };

  const openMegaMenu = () => {
    if (megaCloseTimer.current) clearTimeout(megaCloseTimer.current);
    setMegaOpen(true);
    setSelectedCategoryId((current) => current ?? navigationRoots[0]?.categoryId ?? null);
  };

  const scheduleMegaClose = () => {
    if (megaCloseTimer.current) clearTimeout(megaCloseTimer.current);
    megaCloseTimer.current = setTimeout(closeMegaMenu, 150);
  };

  const toggleMobileCategory = (categoryId: string) => {
    setMobileOpenCategories((current) => ({ ...current, [categoryId]: !current[categoryId] }));
  };

  const navItems = [
    { name: "حراجی", href: "/offers", icon: Tag },
    { name: "پرطرفدار", href: "/trending", icon: TrendingUp },
    { name: "جدیدترین", href: "/new-products", icon: Sparkles },
    { name: "برندها", href: "/brands", icon: Award },
    { name: "فروشندگان", href: "/sellers", icon: Store },
  ];

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

          <form action={lp("/products")} method="get" className="hidden lg:block flex-1 max-w-xl mx-4">
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
            <LocaleSwitcher className="hidden sm:inline-flex me-1" />
            <Link
              href="/products"
              className="lg:hidden w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600"
              aria-label="جستجو"
            >
              <Search className="w-5 h-5" />
            </Link>
            <Link href="/customer-panel/wishlist" className="w-10 h-10 rounded-xl hover:bg-gray-100 flex items-center justify-center text-gray-600" aria-label="علاقه‌مندی">
              <Heart className="w-5 h-5" />
            </Link>
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

        <nav className="hidden lg:flex items-center gap-1 pb-2" aria-label="ناوبری اصلی فروشگاه">
          <div onMouseEnter={openMegaMenu} onMouseLeave={scheduleMegaClose}>
            <button
              type="button"
              className={`flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-sm font-medium transition-all ${
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
                onMouseEnter={openMegaMenu}
                onMouseLeave={scheduleMegaClose}
                className="absolute left-0 right-0 top-full z-50 bg-white shadow-2xl border-t border-gray-200"
              >
                <style>{`
                  .mm-scroll::-webkit-scrollbar { width: 5px; }
                  .mm-scroll::-webkit-scrollbar-track { background: #f1f1f1; border-radius: 10px; }
                  .mm-scroll::-webkit-scrollbar-thumb { background: #2563EB; border-radius: 10px; }
                  .mm-scroll { scrollbar-width: thin; scrollbar-color: #2563EB #f1f1f1; }
                `}</style>
                <div className="max-w-[1800px] mx-auto px-4 sm:px-6 py-5">
                  <div className="grid grid-cols-12 gap-6">
                    <div className="col-span-3 flex flex-col max-h-[460px]">
                      <div className="shrink-0 pb-2.5 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <span className="w-0.5 h-4 bg-[#2563EB] rounded-full" />
                          <span className="font-bold text-xs text-gray-500 tracking-wider">دسته‌بندی‌ها</span>
                        </div>
                        <Link href="/products" onClick={closeMegaMenu} className="text-[10px] text-[#2563EB] hover:underline font-semibold">
                          همه
                        </Link>
                      </div>
                      <div className="flex-1 overflow-y-auto mm-scroll space-y-0.5 min-h-0 pl-0.5">
                        {navigationRoots.map((cat) => {
                          const Icon = categoryIcon(cat.name);
                          const selected = selectedCategory?.categoryId === cat.categoryId;
                          return (
                            <button
                              key={cat.categoryId}
                              type="button"
                              onMouseEnter={() => setSelectedCategoryId(cat.categoryId)}
                              onClick={() => {
                                window.location.href = cat.href;
                                closeMegaMenu();
                              }}
                              className={`w-full flex items-center justify-between px-2.5 py-1.5 rounded-lg text-right transition-all ${
                                selected ? "bg-[#2563EB] text-white shadow-sm" : "text-gray-500 hover:bg-gray-50 hover:text-gray-700"
                              }`}
                            >
                              <span className="flex items-center gap-2.5">
                                <span className={`w-7 h-7 rounded-lg flex items-center justify-center ${selected ? "bg-white/20" : "bg-gray-100 text-gray-500"}`}>
                                  <Icon className="w-4 h-4" />
                                </span>
                                <span className="text-xs">{cat.name}</span>
                              </span>
                              {selected ? <ChevronDown className="w-3 h-3 rotate-90 text-white/50" /> : null}
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    <div className="col-span-6 flex flex-col border-x border-gray-100 px-5 max-h-[460px]">
                      {selectedCategory ? (
                        <>
                          <div className="shrink-0 pb-2.5 flex items-center justify-between pt-0.5">
                            <div className="flex items-center gap-2">
                              {(() => {
                                const Icon = categoryIcon(selectedCategory.name);
                                return <span className="w-5 h-5 rounded-lg bg-[#2563EB]/10 flex items-center justify-center text-[#2563EB]"><Icon className="w-3.5 h-3.5" /></span>;
                              })()}
                              <span className="font-bold text-sm text-gray-800">{selectedCategory.name}</span>
                            </div>
                            <Link href={selectedCategory.href} onClick={closeMegaMenu} className="text-[10px] text-[#2563EB] hover:underline font-semibold">
                              مشاهده همه
                            </Link>
                          </div>
                          <div className="flex-1 overflow-y-auto mm-scroll min-h-0">
                            <div className="grid grid-cols-2 gap-x-4 gap-y-3.5">
                              {childCategories.map((sub) => {
                                const descendants: NavRow[] = useConfiguredMenu
                                  ? megaMenuItems
                                      .filter((item) => item.parentMegaMenuItemId === sub.categoryId)
                                      .map((item) => ({
                                        categoryId: item.megaMenuItemId,
                                        parentCategoryId: item.parentMegaMenuItemId,
                                        name: item.title,
                                        href: item.destination,
                                      }))
                                  : categories
                                      .filter((category) => category.parentCategoryId === sub.categoryId)
                                      .map((item) => ({ ...item, href: `${lp("/products")}?categoryId=${item.categoryId}` }));
                                return (
                                  <div key={sub.categoryId}>
                                    <Link
                                      href={sub.href}
                                      onClick={closeMegaMenu}
                                      className="inline-flex items-center gap-1.5 text-xs font-bold text-gray-800 hover:text-[#2563EB] transition-colors mb-1.5"
                                    >
                                      <span className="w-1 h-1 rounded-full bg-gray-300" />
                                      {sub.name}
                                    </Link>
                                    {descendants.length > 0 ? (
                                      <div className="space-y-1 mr-3.5">
                                        {descendants.slice(0, 4).map((child) => (
                                          <Link
                                            key={child.categoryId}
                                            href={child.href}
                                            onClick={closeMegaMenu}
                                            className="block text-[11px] text-gray-400 hover:text-[#2563EB] transition-colors truncate py-0.5"
                                          >
                                            {child.name}
                                          </Link>
                                        ))}
                                        {descendants.length > 4 ? (
                                          <Link href={sub.href} onClick={closeMegaMenu} className="text-[10px] text-[#2563EB] font-semibold hover:underline">
                                            + {(descendants.length - 4).toLocaleString("fa-IR")} بیشتر
                                          </Link>
                                        ) : null}
                                      </div>
                                    ) : null}
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        </>
                      ) : (
                        <div className="flex items-center justify-center h-full text-gray-400">
                          <p className="text-sm font-medium">دسته‌ای انتخاب نشده</p>
                        </div>
                      )}
                    </div>

                    <div className="col-span-3 space-y-3 max-h-[460px]">
                      <div className="bg-gradient-to-br from-[#2563EB] via-[#1d4ed8] to-[#1e40af] rounded-2xl p-4 text-white text-center shadow-lg shadow-[#2563EB]/20 relative overflow-hidden">
                        <div className="relative z-10">
                          <div className="w-10 h-10 mx-auto mb-2.5 rounded-xl bg-white/15 flex items-center justify-center">
                            <Gift className="w-5 h-5 text-amber-300" />
                          </div>
                          <h4 className="font-bold text-sm">پیشنهادهای فروشگاه</h4>
                          <p className="text-[11px] mt-1 opacity-80">کالاهای دارای پیشنهاد فعال</p>
                          <Link href="/offers" onClick={closeMegaMenu} className="mt-3 inline-flex items-center gap-1 px-4 py-1.5 bg-white text-[#2563EB] rounded-xl text-[11px] font-bold hover:bg-blue-50 transition-all shadow-lg shadow-black/10">
                            مشاهده <ChevronLeft className="w-3 h-3" />
                          </Link>
                        </div>
                      </div>
                      {brands.length > 0 ? (
                        <div className="bg-gray-50 rounded-2xl p-3.5 border border-gray-100">
                          <div className="flex items-center gap-1.5 mb-2.5">
                            <Star className="w-3.5 h-3.5 text-amber-500 fill-amber-500" />
                            <span className="font-bold text-xs text-gray-700">برندهای محبوب</span>
                          </div>
                          <div className="flex flex-wrap gap-1.5">
                            {brands.slice(0, 6).map((brand) => (
                              <Link
                                key={brand.brandId}
                                href={`/brand/${brand.slug}`}
                                onClick={closeMegaMenu}
                                className="px-2.5 py-1 bg-white text-gray-500 rounded-lg text-[10px] font-medium hover:bg-[#2563EB] hover:text-white transition-all border border-gray-100"
                              >
                                {brand.name}
                              </Link>
                            ))}
                          </div>
                        </div>
                      ) : null}
                    </div>
                  </div>
                </div>
              </div>
            ) : null}
          </div>
          <div className="flex items-center gap-0.5 mr-1.5 pr-1.5 border-r border-gray-200">
            {navItems.map((item) => (
              <Link key={item.name} href={item.href} className="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium whitespace-nowrap text-gray-500 hover:bg-gray-50 hover:text-gray-700 transition-all">
                <item.icon className="w-3.5 h-3.5" />
                <span>{item.name}</span>
              </Link>
            ))}
          </div>
        </nav>
      </div>

      <div className={`fixed inset-0 z-[150] transition-all lg:hidden ${mobileOpen ? "visible" : "invisible"}`}>
        <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={() => setMobileOpen(false)} />
        <div className={`absolute right-0 top-0 bottom-0 w-80 bg-white shadow-2xl transform transition-transform duration-300 ${mobileOpen ? "translate-x-0" : "translate-x-full"} flex flex-col`}>
          <div className="flex pr-3 justify-between items-center border-b border-gray-200">
            <div className="relative w-24 h-16 flex items-center">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src="/images/logos/logo.svg" alt="توبا" className="w-24 h-auto" />
            </div>
            <button type="button" onClick={() => setMobileOpen(false)} className="text-gray-500 p-4" aria-label="بستن">
              <X className="w-5 h-5" />
            </button>
          </div>
          <div className="flex-1 overflow-y-auto p-3 space-y-1">
            <form action={lp("/products")} method="get" className="mb-2">
              <div className="relative">
                <input name="q" placeholder="جستجو در کالاها..." className="w-full bg-gray-50 border border-gray-200 rounded-2xl py-2.5 pr-10 pl-3 text-sm" aria-label="جستجوی کالا در موبایل" />
                <Search className="absolute right-3 top-3 w-4 h-4 text-gray-400" />
              </div>
            </form>
            <div className="border-b border-gray-100 pb-2">
              <button type="button" onClick={() => toggleMobileCategory("main")} className="flex justify-between items-center w-full p-3 rounded-xl hover:bg-gray-100 text-gray-800 font-medium">
                <span className="flex items-center gap-2"><Grid3X3 className="w-5 h-5" /> دسته‌بندی‌ها</span>
                <ChevronDown className={`w-4 h-4 transition-transform duration-200 ${mobileOpenCategories.main ? "rotate-180" : ""}`} />
              </button>
              {mobileOpenCategories.main ? (
                <div className="pr-3 mt-1 space-y-3">
                  {navigationRoots.slice(0, 8).map((category) => {
                    const children = categories.filter((child) => child.parentCategoryId === category.categoryId);
                    const Icon = categoryIcon(category.name);
                    return (
                      <div key={category.categoryId} className="border-b border-gray-100 pb-2 last:border-0">
                        <button type="button" onClick={() => toggleMobileCategory(category.categoryId)} className="flex justify-between items-center w-full p-2 rounded-xl hover:bg-gray-100">
                          <span className="flex items-center gap-2 text-sm font-semibold text-gray-800"><Icon className="w-4 h-4 text-gray-400" />{category.name}</span>
                          <ChevronDown className={`w-3 h-3 transition-transform duration-200 ${mobileOpenCategories[category.categoryId] ? "rotate-180" : ""}`} />
                        </button>
                        {mobileOpenCategories[category.categoryId] ? (
                          <div className="pr-3 mt-1 space-y-2">
                            {children.slice(0, 10).map((child) => {
                              const grandchildren = categories.filter((leaf) => leaf.parentCategoryId === child.categoryId);
                              const childKey = `${category.categoryId}:${child.categoryId}`;
                              return (
                                <div key={child.categoryId} className="border-b border-gray-50 pb-1 last:border-0">
                                  <button
                                    type="button"
                                    onClick={() => toggleMobileCategory(childKey)}
                                    className="flex justify-between items-center w-full p-2 rounded-xl hover:bg-gray-100 text-right"
                                  >
                                    <Link
                                      href={`/products?categoryId=${child.categoryId}`}
                                      onClick={(event) => {
                                        event.stopPropagation();
                                        setMobileOpen(false);
                                      }}
                                      className="text-xs font-semibold text-gray-700 hover:text-[#2563EB]"
                                    >
                                      {child.name}
                                    </Link>
                                    {grandchildren.length > 0 ? (
                                      <ChevronDown className={`w-3 h-3 transition-transform duration-200 ${mobileOpenCategories[childKey] ? "rotate-180" : ""}`} />
                                    ) : null}
                                  </button>
                                  {mobileOpenCategories[childKey] && grandchildren.length > 0 ? (
                                    <div className="grid grid-cols-1 gap-1 pr-4 mt-1">
                                      {grandchildren.slice(0, 6).map((leaf) => (
                                        <Link
                                          key={leaf.categoryId}
                                          href={`/products?categoryId=${leaf.categoryId}`}
                                          onClick={() => setMobileOpen(false)}
                                          className="block p-2 text-[11px] text-gray-500 hover:text-[#2563EB] rounded-lg hover:bg-gray-100 transition truncate"
                                        >
                                          {leaf.name}
                                        </Link>
                                      ))}
                                    </div>
                                  ) : null}
                                </div>
                              );
                            })}
                          </div>
                        ) : null}
                      </div>
                    );
                  })}
                </div>
              ) : null}
            </div>
            {navItems.map((item) => (
              <Link key={item.name} href={item.href} onClick={() => setMobileOpen(false)} className="flex items-center gap-3 p-3 rounded-xl hover:bg-gray-100 text-gray-700 transition-colors">
                <item.icon className="w-5 h-5 text-gray-400" />
                <span>{item.name}</span>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
