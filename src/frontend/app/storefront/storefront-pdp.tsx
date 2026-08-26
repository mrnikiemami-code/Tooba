"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  Award,
  Bell,
  GitCompare,
  Headphones,
  Heart,
  LineChart,
  Minus,
  Package,
  Plus,
  RotateCcw,
  Share2,
  Shield,
  ShoppingBag,
  Star,
  Truck,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { formatOfferAmount, loadStorefrontDetail, loadStorefrontQuestions, storefrontMediaUrl } from "./storefront-api.ts";
import { addOfferToCart, toCustomerCartMessage } from "./storefront-cart-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import { StorefrontPdpReviews } from "./storefront-pdp-reviews.tsx";
import { StorefrontPdpQa } from "./storefront-pdp-qa.tsx";
import { StorefrontPdpBulk } from "./storefront-pdp-bulk.tsx";
import { useStorefrontWishlist } from "./storefront-wishlist-provider.tsx";

/**
 * PDP سه ستونهٔ Shopeiva. CTA سبد جهش Cart را جعل نمی‌کند.
 */
export function StorefrontShopeivaPdp({ detail }: { detail: StorefrontProductDetailPage }) {
  const router = useRouter();
  const [currentDetail, setCurrentDetail] = useState(detail);
  const [qty, setQty] = useState(1);
  const [tab, setTab] = useState<"intro" | "full" | "specs" | "reviews" | "qa" | "bulk">("intro");
  const [qaCount, setQaCount] = useState(0);
  const [note, setNote] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const wishlist = useStorefrontWishlist();
  const registerWishlistProduct = wishlist.register;
  const wishlistSaved = wishlist.membership.has(currentDetail.productId);
  const wishlistBusy = wishlist.pending.has(currentDetail.productId);
  const offer = currentDetail.primaryOffer;
  const images = currentDetail.mediaAssetIds.length > 0 ? currentDetail.mediaAssetIds : [null];
  const [active, setActive] = useState(0);

  useEffect(() => registerWishlistProduct(currentDetail.productId), [currentDetail.productId, registerWishlistProduct]);
  useEffect(() => {
    void loadStorefrontQuestions(currentDetail.slug).then((page) => setQaCount(page?.totalCount ?? 0));
  }, [currentDetail.slug]);

  const tabs = [
    { id: "intro" as const, label: "معرفی اجمالی" },
    { id: "full" as const, label: "معرفی تکمیلی" },
    { id: "specs" as const, label: "مشخصات فنی" },
    { id: "reviews" as const, label: "نظرات", count: currentDetail.reviewCount },
    { id: "qa" as const, label: "پرسش و پاسخ", count: qaCount },
    { id: "bulk" as const, label: "خرید عمده", count: 0 },
  ];

  return (
    <div className="py-4 space-y-6">
      <nav className="text-xs text-gray-500 flex gap-2">
        <Link href="/" className="hover:text-[#2563EB]">
          خانه
        </Link>
        <span>/</span>
        <Link href="/products" className="hover:text-[#2563EB]">
          {currentDetail.categoryName}
        </Link>
        <span>/</span>
        <span className="text-gray-800">{currentDetail.title}</span>
      </nav>

      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm">
        <div className="grid grid-cols-1 lg:grid-cols-12">
          <div className="lg:col-span-5 border-b lg:border-b-0 lg:border-l border-gray-200 p-4">
            <div className="relative aspect-square bg-gray-50 rounded-2xl overflow-hidden">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={storefrontMediaUrl(images[active])} alt={currentDetail.title} className="w-full h-full object-contain p-6" />
            </div>
            <div className="flex gap-2 mt-3 overflow-x-auto">
              {images.map((id, index) => (
                <button
                  key={`${id}-${index}`}
                  type="button"
                  onClick={() => setActive(index)}
                  className={`w-16 h-16 rounded-xl border overflow-hidden shrink-0 ${index === active ? "border-[#2563EB]" : "border-gray-200"}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={storefrontMediaUrl(id)} alt="" className="w-full h-full object-contain p-1" />
                </button>
              ))}
            </div>
          </div>

          <div className="lg:col-span-4 border-b lg:border-b-0 lg:border-l border-gray-200 p-4 lg:p-5 space-y-4">
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <Link href="/products" className="text-[#2563EB] font-medium hover:underline">
                {currentDetail.categoryName}
              </Link>
              <span>/</span>
              <span className="bg-gray-100 px-2 py-0.5 rounded-lg">{currentDetail.brandName ?? "برند ثبت‌نشده"}</span>
            </div>
            <h1 className="text-xl lg:text-2xl font-extrabold text-gray-900 leading-9">{currentDetail.title}</h1>
            <p className="text-sm text-gray-500 leading-6">{currentDetail.shortDescription ?? "معرفی اجمالی برای این کالا ثبت نشده است."}</p>
            {currentDetail.reviewCount > 0 && currentDetail.averageRating !== null ? (
              <button type="button" onClick={() => setTab("reviews")} className="flex items-center gap-3 text-xs text-gray-500">
                <span className="flex items-center gap-1.5 rounded-xl bg-gray-100 px-3 py-1.5">
                  <Star className="size-4 fill-amber-400 text-amber-400" />
                  <strong className="text-gray-800">{currentDetail.averageRating.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}</strong>
                  از ۵
                </span>
                <span>({currentDetail.reviewCount.toLocaleString("fa-IR")} دیدگاه)</span>
                <span className="text-[#2563EB]">مشاهده نظرات</span>
              </button>
            ) : null}
            {currentDetail.variants.some((variant) => variant.options.length > 0) ? (
              <div className="space-y-2 min-w-0">
                <p className="text-sm font-bold text-gray-700">انتخاب گزینه</p>
                <div className="flex flex-wrap gap-2 max-w-full">
                  {currentDetail.variants.filter((variant) => variant.options.length > 0).map((variant) => (
                    <button
                      key={variant.variantId}
                      type="button"
                      disabled={busy}
                      aria-pressed={variant.variantId === currentDetail.selectedVariantId}
                      className={`max-w-full rounded-xl border px-3 py-2 text-xs break-words ${
                        variant.variantId === currentDetail.selectedVariantId
                          ? "border-[#2563EB] bg-blue-50 text-[#2563EB]"
                          : "border-gray-200 bg-white text-gray-700"
                      }`}
                      onClick={() => {
                        void (async () => {
                          setBusy(true);
                          setNote(null);
                          const selected = await loadStorefrontDetail(currentDetail.slug, variant.variantId);
                          if (selected) {
                            setCurrentDetail(selected);
                            setQty(1);
                          } else {
                            setNote("دریافت اطلاعات این گزینه ممکن نشد.");
                          }
                          setBusy(false);
                        })();
                      }}
                    >
                      {variant.options.map((option) => `${option.label}: ${option.value}`).join(" · ")}
                    </button>
                  ))}
                </div>
              </div>
            ) : null}
          </div>

          <div className="lg:col-span-3 p-4 lg:p-5 space-y-3">
            <div className="grid grid-cols-2 gap-2">
              <div className="p-2 bg-gray-50 rounded-xl">
                <p className="text-[9px] text-gray-500">دسته</p>
                <p className="text-xs font-medium truncate">{currentDetail.categoryName}</p>
              </div>
              <div className="p-2 bg-gray-50 rounded-xl">
                <p className="text-[9px] text-gray-500">برند</p>
                <p className="text-xs font-medium truncate">{currentDetail.brandName ?? "-"}</p>
              </div>
              <div className="p-2 bg-gray-50 rounded-xl">
                <p className="text-[9px] text-gray-500">فروشنده</p>
                <p className="text-xs font-medium truncate">{offer.sellerDisplayName}</p>
              </div>
              <div className="p-2 bg-gray-50 rounded-xl">
                <p className="text-[9px] text-gray-500">موجودی Offer</p>
                <p className={`text-xs font-medium ${offer.availableUnits > 0 ? "text-emerald-600" : "text-red-500"}`}>
                  {offer.availableUnits > 0 ? `${offer.availableUnits.toLocaleString("fa-IR")} عدد` : "ناموجود"}
                </p>
              </div>
            </div>
            <div className="flex items-center justify-between p-3 bg-gray-50 rounded-xl text-xs">
              <span className="flex items-center gap-2">
                <span className={`w-2.5 h-2.5 rounded-full ${offer.availableUnits > 0 ? "bg-emerald-500" : "bg-red-500"}`} />
                {offer.availableUnits > 0 ? "موجود در انبار فروشنده" : "ناموجود"}
              </span>
            </div>
            <p className="text-xs text-gray-600">فروشنده: {offer.sellerDisplayName}</p>
            <p className="text-[11px] text-gray-400">مالیات: {offer.taxCategoryLabel} · بازار {offer.market}</p>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={wishlistBusy}
                aria-pressed={wishlistSaved}
                onClick={() => void wishlist.toggle(currentDetail.productId).then(setNote)}
                className={`flex-1 py-2.5 rounded-xl text-sm border flex items-center justify-center gap-2 disabled:opacity-60 ${wishlistSaved ? "border-rose-200 text-rose-600 bg-rose-50" : "border-gray-200"}`}
              >
                <Heart className={`w-4 h-4 ${wishlistSaved ? "fill-current" : ""}`} /> {wishlistSaved ? "حذف از علاقه‌مندی" : "علاقه‌مندی"}
              </button>
              <button type="button" className="px-4 py-2.5 rounded-xl border border-gray-200">
                <LineChart className="w-4 h-4" />
              </button>
              <button type="button" className="px-4 py-2.5 rounded-xl border border-gray-200">
                <GitCompare className="w-4 h-4" />
              </button>
              <button type="button" className="px-4 py-2.5 rounded-xl border border-gray-200">
                <Share2 className="w-4 h-4" />
              </button>
            </div>
            <div className="flex items-center justify-between border border-gray-200 rounded-xl overflow-hidden">
              <button type="button" className="px-3 py-2" onClick={() => setQty((value) => Math.max(1, value - 1))} aria-label="کاهش">
                <Minus className="w-4 h-4" />
              </button>
              <span className="text-sm font-bold">{qty.toLocaleString("fa-IR")}</span>
              <button
                type="button"
                className="px-3 py-2 disabled:opacity-40"
                disabled={qty >= offer.availableUnits}
                onClick={() => setQty((value) => Math.min(offer.availableUnits, value + 1))}
                aria-label="افزایش"
              >
                <Plus className="w-4 h-4" />
              </button>
            </div>
            {currentDetail.promotionLabel ?? offer.promotionLabel ? (
              <p className="text-xs font-bold text-[#2563EB]">{currentDetail.promotionLabel ?? offer.promotionLabel}</p>
            ) : null}
            <p className="text-2xl font-black text-[#2563EB]">
              {formatOfferAmount(
                currentDetail.promotionalAmountExclusiveOfTax ?? offer.promotionalAmountExclusiveOfTax ?? offer.amountExclusiveOfTax,
                offer.currency,
              )}
            </p>
            {offer.availableUnits <= 0 ? (
              <button type="button" className="w-full py-3 rounded-xl font-bold text-sm bg-amber-50 text-amber-600 border border-amber-200 flex items-center justify-center gap-2">
                <Bell className="w-4 h-4" /> موجود شد خبرم کن
              </button>
            ) : (
              <button
                type="button"
                disabled={!currentDetail.cartMutationEnabled || busy}
                onClick={() => {
                  void (async () => {
                    setBusy(true);
                    setNote(null);
                    try {
                      await addOfferToCart(offer.offerId, qty);
                      setNote("به سبد زنده اضافه شد.");
                      router.push("/cart");
                    } catch (cause) {
                      setNote(toCustomerCartMessage(cause));
                    } finally {
                      setBusy(false);
                    }
                  })();
                }}
                className="w-full py-3 rounded-xl font-bold text-sm bg-[#2563EB] text-white hover:bg-[#1d4ed8] disabled:opacity-60 flex items-center justify-center gap-2"
              >
                <ShoppingBag className="w-4 h-4" /> افزودن به سبد خرید
              </button>
            )}
            {note ? <p className="text-xs text-gray-500" role="status" aria-live="polite">{note}</p> : null}
            {currentDetail.otherSellers.length > 0 ? (
              <div className="pt-3 border-t border-dashed border-gray-200 space-y-2">
                <strong className="text-xs">فروشندگان دیگر همین کالا</strong>
                {currentDetail.otherSellers.map((seller) => (
                  <p key={seller.offerId} className="text-xs text-gray-600">
                    {seller.sellerDisplayName} · {formatOfferAmount(seller.amountExclusiveOfTax, seller.currency)} ·{" "}
                    {seller.inStock ? "موجود" : "ناموجود"}
                  </p>
                ))}
              </div>
            ) : null}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden">
        <div className="flex border-b border-gray-200 overflow-x-auto">
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              className={`px-4 lg:px-6 py-3 text-sm font-medium border-b-2 whitespace-nowrap ${
                tab === item.id ? "border-[#2563EB] text-[#2563EB]" : "border-transparent text-gray-500"
              }`}
            >
              {item.label}
              {(item.count ?? 0) > 0 ? <span className="mr-1 rounded-full bg-gray-100 px-2 py-0.5 text-[10px]">{item.count?.toLocaleString("fa-IR")}</span> : null}
            </button>
          ))}
        </div>
        <div className="p-5 lg:p-6 text-sm leading-8 text-gray-700">
          {tab === "specs" ? (
            currentDetail.specifications.length > 0 ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4" data-testid="pdp-specs">
                {currentDetail.specifications.map((specification) => (
                  <div key={`${specification.label}-${specification.value}`} className="flex items-start gap-3 p-3 bg-gray-50 rounded-xl border border-gray-200">
                    <Package className="w-5 h-5 text-gray-400 shrink-0 mt-0.5" />
                    <div>
                      <p className="text-xs text-gray-500">{specification.label}</p>
                      <p className="text-sm font-medium text-gray-900 break-words">{specification.value}</p>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p>مشخصاتی برای این کالا ثبت نشده است.</p>
            )
          ) : tab === "reviews" ? (
            <StorefrontPdpReviews detail={currentDetail} />
          ) : tab === "qa" ? (
            <StorefrontPdpQa detail={currentDetail} />
          ) : tab === "bulk" ? (
            <StorefrontPdpBulk detail={currentDetail} />
          ) : tab === "full" ? (
            <div className="space-y-6" data-testid="pdp-full">
              <h2 className="text-2xl font-extrabold text-gray-900 relative pb-3 before:absolute before:bottom-0 before:right-0 before:h-1 before:w-24 before:bg-[#2563EB] before:rounded">
                معرفی تکمیلی
              </h2>
              <p className="text-justify leading-8 text-gray-700 whitespace-pre-wrap">
                {currentDetail.fullDescription ?? "معرفی تکمیلی برای این کالا ثبت نشده است."}
              </p>
            </div>
          ) : (
            <div className="space-y-6" data-testid="pdp-intro">
              <h2 className="text-2xl font-extrabold text-gray-900 relative pb-3 before:absolute before:bottom-0 before:right-0 before:h-1 before:w-24 before:bg-[#2563EB] before:rounded">
                معرفی محصول
              </h2>
              <p className="text-justify leading-8 text-gray-700 whitespace-pre-wrap">
                {currentDetail.shortDescription ?? "معرفی اجمالی برای این کالا ثبت نشده است."}
              </p>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 pt-4 border-t border-gray-200">
                {[
                  { icon: Truck, title: "ارسال از فروشنده", desc: "بر اساس موجودی Offer" },
                  { icon: Shield, title: "عرضهٔ زنده", desc: "قیمت از Pricing" },
                  { icon: RotateCcw, title: "وضعیت موجودی", desc: "از Inventory" },
                  { icon: Headphones, title: "پشتیبانی فروشگاه", desc: "مسیر رسمی مشتری" },
                  { icon: Award, title: "فروشندهٔ ثبت‌شده", desc: "هویت Party" },
                  { icon: Package, title: "کالای Catalog", desc: "بدون قیمت روی Product" },
                ].map((item) => (
                  <div key={item.title} className="flex items-center gap-2 p-3 bg-gray-50 rounded-xl">
                    <div className="w-8 h-8 rounded-lg bg-[#2563EB]/10 flex items-center justify-center shrink-0">
                      <item.icon className="w-4 h-4 text-[#2563EB]" />
                    </div>
                    <div>
                      <p className="text-xs font-bold text-gray-700">{item.title}</p>
                      <p className="text-[9px] text-gray-500">{item.desc}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
      {currentDetail.relatedProducts.length > 0 ? (
        <section className="space-y-3" aria-labelledby="related-products-title">
          <div className="flex items-center justify-between">
            <h2 id="related-products-title" className="text-lg font-extrabold text-gray-900">محصولات مرتبط</h2>
            <Link href="/products" className="text-xs font-bold text-[#2563EB]">مشاهده همه</Link>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
            {currentDetail.relatedProducts.map((card) => <StorefrontProductCardView key={card.slug} card={card} />)}
          </div>
        </section>
      ) : null}
    </div>
  );
}
