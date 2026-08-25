"use client";

import Link from "next/link";
import { useState } from "react";
import {
  Bell,
  GitCompare,
  Heart,
  LineChart,
  Minus,
  Plus,
  Share2,
  ShoppingBag,
  Star,
} from "lucide-react";
import { useRouter } from "next/navigation";
import { formatOfferAmount, loadStorefrontDetail, storefrontMediaUrl } from "./storefront-api.ts";
import { addOfferToCart, toCustomerCartMessage } from "./storefront-cart-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import { StorefrontPdpReviews } from "./storefront-pdp-reviews.tsx";

/**
 * PDP سه ستونهٔ Shopeiva. CTA سبد جهش Cart را جعل نمی‌کند.
 */
export function StorefrontShopeivaPdp({ detail }: { detail: StorefrontProductDetailPage }) {
  const router = useRouter();
  const [currentDetail, setCurrentDetail] = useState(detail);
  const [qty, setQty] = useState(1);
  const [tab, setTab] = useState<"intro" | "full" | "specs" | "reviews">("intro");
  const [note, setNote] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const offer = currentDetail.primaryOffer;
  const images = currentDetail.mediaAssetIds.length > 0 ? currentDetail.mediaAssetIds : [null];
  const [active, setActive] = useState(0);

  const tabs = [
    { id: "intro" as const, label: "معرفی اجمالی" },
    { id: "full" as const, label: "معرفی تکمیلی" },
    { id: "specs" as const, label: "مشخصات فنی" },
    { id: "reviews" as const, label: "نظرات", count: currentDetail.reviewCount },
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
              <button type="button" className="flex-1 py-2.5 rounded-xl text-sm border border-gray-200 flex items-center justify-center gap-2">
                <Heart className="w-4 h-4" /> علاقه‌مندی
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
              <dl className="divide-y divide-gray-100">
                {currentDetail.specifications.map((specification) => (
                  <div key={`${specification.label}-${specification.value}`} className="grid grid-cols-1 sm:grid-cols-3 gap-2 py-2">
                    <dt className="font-bold text-gray-600">{specification.label}</dt>
                    <dd className="sm:col-span-2 break-words">{specification.value}</dd>
                  </div>
                ))}
              </dl>
            ) : <p>مشخصاتی برای این کالا ثبت نشده است.</p>
          ) : tab === "reviews" ? (
            <StorefrontPdpReviews detail={currentDetail} />
          ) : tab === "full" ? (
            <p>{currentDetail.fullDescription ?? "معرفی تکمیلی برای این کالا ثبت نشده است."}</p>
          ) : (
            <p>{currentDetail.shortDescription ?? "معرفی اجمالی برای این کالا ثبت نشده است."}</p>
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
