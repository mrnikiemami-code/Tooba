"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useMemo, useState } from "react";
import {
  Award,
  Bell,
  Check,
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
  Store,
  Truck,
} from "lucide-react";
import { toast } from "react-toastify";
import { formatQuantityDisplay, parseQuantityInput } from "../../lib/quantity-display.ts";
import { formatOfferAmount, loadStorefrontDetail, loadStorefrontQuestions, storefrontMediaUrl } from "./storefront-api.ts";
import { addOfferToCart, toCustomerCartMessage } from "./storefront-cart-api.ts";
import type {
  StorefrontAlternateOffer,
  StorefrontOfferCandidate,
  StorefrontProductDetailPage,
  StorefrontVariantOption,
} from "./storefront-model.ts";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import { StorefrontPdpReviews } from "./storefront-pdp-reviews.tsx";
import { StorefrontPdpQa } from "./storefront-pdp-qa.tsx";
import { StorefrontPdpBulk } from "./storefront-pdp-bulk.tsx";
import { useStorefrontWishlist } from "./storefront-wishlist-provider.tsx";

/**
 * PDP سه ستونهٔ Shopeiva. CTA سبد جهش Cart را جعل نمی‌کند.
 */
export function StorefrontShopeivaPdp({ detail }: { detail: StorefrontProductDetailPage }) {
  const [currentDetail, setCurrentDetail] = useState(detail);
  const [qtyText, setQtyText] = useState("1");
  const [selectedOfferId, setSelectedOfferId] = useState(detail.primaryOffer.offerId);
  const [tab, setTab] = useState<"intro" | "full" | "specs" | "reviews" | "qa" | "bulk">("intro");
  const [qaCount, setQaCount] = useState(0);
  const [note, setNote] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const wishlist = useStorefrontWishlist();
  const registerWishlistProduct = wishlist.register;
  const wishlistSaved = wishlist.membership.has(currentDetail.productId);
  const wishlistBusy = wishlist.pending.has(currentDetail.productId);
  const sellerChoices = useMemo(() => listSellerChoices(currentDetail), [currentDetail]);
  const offer = resolveSelectedOffer(currentDetail, selectedOfferId);
  const displayAmount =
    offer.offerId === currentDetail.primaryOffer.offerId
      ? (currentDetail.promotionalAmountExclusiveOfTax ?? offer.promotionalAmountExclusiveOfTax ?? offer.amountExclusiveOfTax)
      : offer.amountExclusiveOfTax;
  const images = currentDetail.mediaAssetIds.length > 0 ? currentDetail.mediaAssetIds : [null];
  const [active, setActive] = useState(0);
  const optionVariants = currentDetail.variants.filter((variant) => variant.options.length > 0);

  useEffect(() => {
    setCurrentDetail(detail);
    setSelectedOfferId(detail.primaryOffer.offerId);
  }, [detail]);
  useEffect(() => setSelectedOfferId(currentDetail.primaryOffer.offerId), [currentDetail.selectedVariantId, currentDetail.primaryOffer.offerId]);
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
    <div className="py-4 space-y-6 bg-section-surface" data-testid="storefront-pdp" data-storefront-surface-role="section">
      <nav className="text-xs text-gray-500 flex gap-2">
        <Link href="/" className="hover:text-primary">
          خانه
        </Link>
        <span>/</span>
        <Link href="/products" className="hover:text-primary">
          {currentDetail.categoryName}
        </Link>
        <span>/</span>
        <span className="text-gray-800">{currentDetail.title}</span>
      </nav>

      <div className="rounded-2xl" data-storefront-surface-role="inherit" data-testid="pdp-primary-card">
        <div className="grid grid-cols-1 lg:grid-cols-12">
          <div className="lg:col-span-5 border-b lg:border-b-0 lg:border-l border-gray-200 p-4">
            <div className="relative aspect-square bg-background rounded-2xl overflow-hidden" data-storefront-surface-role="media" data-testid="pdp-gallery">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={storefrontMediaUrl(images[active])} alt={currentDetail.title} className="w-full h-full object-contain p-6" />
            </div>
            <div className="flex gap-2 mt-3 overflow-x-auto">
              {images.map((id, index) => (
                <button
                  key={`${id}-${index}`}
                  type="button"
                  onClick={() => setActive(index)}
                  className={`w-16 h-16 rounded-xl border overflow-hidden shrink-0 ${index === active ? "border-primary" : "border-gray-200"}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img src={storefrontMediaUrl(id)} alt="" className="w-full h-full object-contain p-1" />
                </button>
              ))}
            </div>
          </div>

          <div className="lg:col-span-4 border-b lg:border-b-0 lg:border-l border-gray-200 p-4 lg:p-5 space-y-4" data-storefront-surface-role="inherit" data-testid="pdp-product-info">
            <div className="flex items-center gap-2 text-xs text-gray-500">
              <Link href="/products" className="text-primary font-medium hover:underline">
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
                <span className="text-primary">مشاهده نظرات</span>
              </button>
            ) : null}
            {optionVariants.length > 0 ? (
              <div className="space-y-2 min-w-0">
                <p className="text-sm font-bold text-gray-700">انتخاب گزینه</p>
                <div className="flex flex-wrap gap-2 max-w-full">
                  {optionVariants.map((variant) => {
                    const selected = variant.variantId === currentDetail.selectedVariantId;
                    const shoppable = Boolean(variant.primaryOffer);
                    return (
                      <button
                        key={variant.variantId}
                        type="button"
                        disabled={busy || !shoppable}
                        aria-pressed={selected}
                        aria-disabled={!shoppable}
                        title={shoppable ? undefined : "این گزینه فعلاً توسط فروشنده‌ای عرضه نشده است."}
                        className={`max-w-full rounded-xl border px-3 py-2 text-xs text-right ${
                          selected
                            ? "border-primary bg-blue-50 text-primary"
                            : shoppable
                              ? "border-gray-200 bg-surface text-gray-700 hover:border-gray-300"
                              : "border-dashed border-gray-200 bg-gray-50 text-gray-400"
                        }`}
                        onClick={() => {
                          if (!shoppable || selected) {
                            return;
                          }
                          void (async () => {
                            setBusy(true);
                            setNote(null);
                            const next = await loadStorefrontDetail(currentDetail.slug, variant.variantId);
                            if (next) {
                              setCurrentDetail(next);
                              setSelectedOfferId(next.primaryOffer.offerId);
                              setQtyText("1");
                            } else {
                              setNote("دریافت اطلاعات این گزینه ممکن نشد.");
                            }
                            setBusy(false);
                          })();
                        }}
                      >
                        <VariantOptionLabel options={variant.options} />
                        {!shoppable ? <span className="mt-1 block text-[10px]">ناموجود</span> : null}
                      </button>
                    );
                  })}
                </div>
              </div>
            ) : null}
          </div>

          <div className="lg:col-span-3 p-4 lg:p-5 space-y-3" data-storefront-surface-role="inherit" data-testid="pdp-buy-column">
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
              <button
                type="button"
                className="px-3 py-2"
                onClick={() => {
                  const current = parseQuantityInput(qtyText) ?? 1;
                  setQtyText(formatQuantityDisplay(Math.max(0.01, current - 1), 6));
                }}
                aria-label="کاهش"
              >
                <Minus className="w-4 h-4" />
              </button>
              <input
                className="w-20 text-center text-sm font-bold tabular-nums bg-transparent"
                value={qtyText}
                inputMode="decimal"
                dir="ltr"
                aria-label="تعداد"
                data-testid="pdp-qty-input"
                onChange={(event) => setQtyText(event.target.value)}
                onBlur={() => {
                  const parsed = parseQuantityInput(qtyText);
                  setQtyText(parsed == null ? "1" : formatQuantityDisplay(parsed, 6));
                }}
              />
              <button
                type="button"
                className="px-3 py-2 disabled:opacity-40"
                disabled={(parseQuantityInput(qtyText) ?? 1) >= offer.availableUnits}
                onClick={() => {
                  const current = parseQuantityInput(qtyText) ?? 1;
                  setQtyText(formatQuantityDisplay(Math.min(offer.availableUnits, current + 1), 6));
                }}
                aria-label="افزایش"
              >
                <Plus className="w-4 h-4" />
              </button>
            </div>
            {currentDetail.promotionLabel ?? offer.promotionLabel ? (
              <p className="text-xs font-bold text-primary">{currentDetail.promotionLabel ?? offer.promotionLabel}</p>
            ) : null}
            <p className="text-2xl font-black text-primary">
              {formatOfferAmount(displayAmount, offer.currency)}
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
                      const qty = parseQuantityInput(qtyText);
                      if (qty == null) {
                        setNote("تعداد نامعتبر است.");
                        return;
                      }
                      await addOfferToCart(offer.offerId, qty);
                      const added = `محصول ${currentDetail.title} به سبد خرید اضافه شد`;
                      setNote(added);
                      toast.success(added, { autoClose: 2800 });
                    } catch (cause) {
                      const message = toCustomerCartMessage(cause);
                      setNote(message);
                      toast.error(message);
                    } finally {
                      setBusy(false);
                    }
                  })();
                }}
                className="w-full py-3 rounded-xl font-bold text-sm bg-primary text-white hover:bg-primary-strong disabled:opacity-60 flex items-center justify-center gap-2"
              >
                <ShoppingBag className="w-4 h-4" /> افزودن به سبد خرید
              </button>
            )}
            {note ? <p className="text-xs text-gray-500" role="status" aria-live="polite">{note}</p> : null}
            <div className="pt-3 border-t border-dashed border-gray-200 space-y-2" data-testid="pdp-other-sellers">
              <div className="flex items-center justify-between gap-2">
                <strong className="text-xs">فروشندگان این کالا</strong>
                <span className="text-[10px] text-gray-400">
                  {sellerChoices.length.toLocaleString("fa-IR")} فروشنده
                </span>
              </div>
              <div className="space-y-2" role="radiogroup" aria-label="انتخاب فروشنده">
                {sellerChoices.map((seller) => {
                  const selected = seller.offerId === offer.offerId;
                  return (
                    <button
                      key={seller.offerId}
                      type="button"
                      role="radio"
                      aria-checked={selected}
                      disabled={busy}
                      onClick={() => {
                        setSelectedOfferId(seller.offerId);
                        setQtyText("1");
                        setNote(null);
                      }}
                      className={`w-full rounded-xl border p-2.5 text-right transition-colors ${
                        selected
                          ? "border-primary bg-blue-50 ring-1 ring-primary/15"
                          : "border-gray-200 bg-surface hover:border-gray-300"
                      }`}
                    >
                      <div className="flex items-start gap-2">
                        <span
                          className={`mt-0.5 flex size-4 shrink-0 items-center justify-center rounded-full border ${
                            selected ? "border-primary bg-primary text-white" : "border-gray-300 bg-surface"
                          }`}
                        >
                          {selected ? <Check className="size-2.5" strokeWidth={3} /> : null}
                        </span>
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-1.5">
                            <Store className="size-3.5 shrink-0 text-gray-400" />
                            <p className="truncate text-xs font-bold text-gray-800">{seller.sellerDisplayName}</p>
                          </div>
                          <p className={`mt-1 text-[11px] ${seller.inStock ? "text-emerald-600" : "text-red-500"}`}>
                            {seller.inStock
                              ? `موجود · ${seller.availableUnits.toLocaleString("fa-IR")} عدد`
                              : "ناموجود"}
                          </p>
                        </div>
                        <div className="shrink-0 text-left">
                          <p className="text-xs font-black tabular-nums text-primary" dir="ltr">
                            {formatOfferAmount(seller.amountExclusiveOfTax, seller.currency)}
                          </p>
                          <p className="mt-0.5 text-[10px] text-gray-500">{selected ? "انتخاب‌شده" : "انتخاب"}</p>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-section-alternate rounded-2xl" data-testid="pdp-tabs-card" data-storefront-surface-role="alternate">
        <div
          className="sticky top-0 z-20 flex border-b border-gray-200 overflow-x-auto bg-transparent rounded-t-2xl"
          data-testid="pdp-sticky-tabs"
          data-storefront-surface-role="inherit"
        >
          {tabs.map((item) => (
            <button
              key={item.id}
              type="button"
              data-testid={`pdp-tab-${item.id}`}
              onClick={() => setTab(item.id)}
              className={`px-4 lg:px-6 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
                tab === item.id ? "border-primary text-primary" : "border-transparent text-gray-500 hover:text-gray-700"
              }`}
            >
              {item.label}
              {(item.count ?? 0) > 0 ? (
                <span
                  className={`mr-1 rounded-full px-2 py-0.5 text-[10px] ${
                    tab === item.id ? "bg-primary text-white" : "bg-gray-200 text-gray-600"
                  }`}
                >
                  {item.count?.toLocaleString("fa-IR")}
                </span>
              ) : null}
            </button>
          ))}
        </div>
        <div className="p-5 lg:p-6 text-sm leading-8 text-gray-700 overflow-hidden rounded-b-2xl" data-storefront-surface-role="inherit">
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
              <h2 className="text-2xl font-extrabold text-gray-900 relative pb-3 before:absolute before:bottom-0 before:right-0 before:h-1 before:w-24 before:bg-primary before:rounded">
                معرفی تکمیلی
              </h2>
              <p className="text-justify leading-8 text-gray-700 whitespace-pre-wrap">
                {currentDetail.fullDescription ?? "معرفی تکمیلی برای این کالا ثبت نشده است."}
              </p>
            </div>
          ) : (
            <div className="space-y-6" data-testid="pdp-intro">
              <h2 className="text-2xl font-extrabold text-gray-900 relative pb-3 before:absolute before:bottom-0 before:right-0 before:h-1 before:w-24 before:bg-primary before:rounded">
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
                  <div key={item.title} className="flex items-center gap-2 p-3 rounded-xl bg-surface" data-storefront-surface-role="card">
                    <div className="w-8 h-8 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
                      <item.icon className="w-4 h-4 text-primary" />
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
        <section className="space-y-3 py-8 px-1 bg-section-alternate rounded-2xl" aria-labelledby="related-products-title" data-testid="pdp-related" data-storefront-surface-role="alternate">
          <div className="flex items-center justify-between">
            <h2 id="related-products-title" className="text-lg font-extrabold text-gray-900">محصولات مرتبط</h2>
            <Link href="/products" className="text-xs font-bold text-primary">مشاهده همه</Link>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
            {currentDetail.relatedProducts.map((card) => <StorefrontProductCardView key={card.slug} card={card} />)}
          </div>
        </section>
      ) : null}
    </div>
  );
}

function VariantOptionLabel({ options }: { options: StorefrontVariantOption[] }) {
  return (
    <span className="flex flex-wrap items-center justify-end gap-x-1.5 gap-y-0.5" dir="rtl">
      {options.map((option, index) => (
        <span key={`${option.label}-${option.value}`} className="inline-flex max-w-full items-center gap-1">
          {index > 0 ? <span className="text-gray-300">·</span> : null}
          <span className="text-gray-500">{cleanOptionText(option.label)}:</span>
          <span dir="auto" className="break-words font-medium">
            {cleanOptionText(option.value)}
          </span>
        </span>
      ))}
    </span>
  );
}

function cleanOptionText(value: string): string {
  const cleaned = value.replace(/\s*\([^)]*\)\s*/g, " ").replace(/\s+/g, " ").trim();
  return cleaned || value;
}

type SellerChoice = StorefrontAlternateOffer & { isPrimary: boolean };

function listSellerChoices(detail: StorefrontProductDetailPage): SellerChoice[] {
  const primary: SellerChoice = {
    offerId: detail.primaryOffer.offerId,
    sellerDisplayName: detail.primaryOffer.sellerDisplayName,
    amountExclusiveOfTax: detail.primaryOffer.amountExclusiveOfTax,
    currency: detail.primaryOffer.currency,
    availableUnits: detail.primaryOffer.availableUnits,
    inStock: detail.primaryOffer.availableUnits > 0,
    isPrimary: true,
  };
  const others = detail.otherSellers
    .filter((seller) => seller.offerId !== primary.offerId)
    .map((seller) => ({ ...seller, isPrimary: false }));
  return [primary, ...others].sort((left, right) => {
    if (left.inStock !== right.inStock) {
      return left.inStock ? -1 : 1;
    }
    return left.amountExclusiveOfTax - right.amountExclusiveOfTax;
  });
}

function resolveSelectedOffer(detail: StorefrontProductDetailPage, selectedOfferId: string): StorefrontOfferCandidate {
  if (selectedOfferId === detail.primaryOffer.offerId) {
    return detail.primaryOffer;
  }
  const alternate = detail.otherSellers.find((seller) => seller.offerId === selectedOfferId);
  if (!alternate) {
    return detail.primaryOffer;
  }
  return {
    ...detail.primaryOffer,
    offerId: alternate.offerId,
    sellerDisplayName: alternate.sellerDisplayName,
    amountExclusiveOfTax: alternate.amountExclusiveOfTax,
    currency: alternate.currency,
    availableUnits: alternate.availableUnits,
    promotionalAmountExclusiveOfTax: null,
    promotionLabel: null,
  };
}
