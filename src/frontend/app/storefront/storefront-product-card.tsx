"use client";

import Link from "next/link";
import { Eye, Heart, Share2, ShoppingBag, Sparkles, Star, Zap } from "lucide-react";
import { useEffect, useState } from "react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import type { StorefrontProductCard } from "./storefront-model.ts";
import { useStorefrontWishlist } from "./storefront-wishlist-provider.tsx";

export const STOREFRONT_ACCENT = "#2563EB";

function discountPercent(card: StorefrontProductCard): number | null {
  if (card.promotionalAmountExclusiveOfTax == null || card.offerAmountExclusiveOfTax <= 0) return null;
  const pct = Math.round((1 - card.promotionalAmountExclusiveOfTax / card.offerAmountExclusiveOfTax) * 100);
  return pct > 0 ? pct : null;
}

/**
 * کارت کالای خانوادهٔ Shopeiva. مبلغ و موجودی از Offer/Inventory است نه از Product.
 */
export function StorefrontProductCardView({
  card,
  showNew = false,
  showHoverActions = true,
}: {
  card: StorefrontProductCard;
  showNew?: boolean;
  showHoverActions?: boolean;
}) {
  const wishlist = useStorefrontWishlist();
  const register = wishlist.register;
  const [note, setNote] = useState<string | null>(null);
  const saved = wishlist.membership.has(card.productId);
  const busy = wishlist.pending.has(card.productId);
  const discount = discountPercent(card);
  const productHref = `/products/${card.slug}`;

  useEffect(() => register(card.productId), [card.productId, register]);

  const handleShare = (event: React.MouseEvent) => {
    event.preventDefault();
    event.stopPropagation();
    const url = `${window.location.origin}${productHref}`;
    if (navigator.share) {
      void navigator.share({ title: card.title, url });
      return;
    }
    void navigator.clipboard.writeText(url);
  };

  return (
    <article className="group relative flex flex-col rounded-2xl overflow-hidden bg-white border border-gray-100 hover:shadow-xl hover:-translate-y-1 transition-all duration-300">
      <Link href={productHref} className="flex flex-1 flex-col">
        <div className="relative aspect-[4/5] bg-gray-50 overflow-hidden">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={storefrontMediaUrl(card.mediaAssetId)}
            alt=""
            className="absolute inset-0 w-full h-full object-contain p-3 group-hover:scale-105 transition duration-700"
          />
          <div className="absolute top-2 right-2 z-10 flex flex-col gap-1 items-start">
            {discount !== null ? (
              <span className="bg-[#2563EB] text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg shadow-lg shadow-[#2563EB]/30 flex items-center gap-1">
                <Zap className="w-3 h-3" />
                {discount.toLocaleString("fa-IR")}%
              </span>
            ) : null}
            {showNew ? (
              <span className="relative overflow-hidden bg-gradient-to-l from-emerald-400 via-emerald-500 to-emerald-600 text-white text-[10px] font-bold px-3 py-0.5 rounded-lg shadow-lg shadow-emerald-500/30 flex items-center gap-1.5">
                <Sparkles className="w-3 h-3" />
                جدید
              </span>
            ) : null}
            {!showNew && card.promotionLabel ? (
              <span className="bg-[#2563EB] text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg">
                {card.promotionLabel}
              </span>
            ) : null}
          </div>
          {showHoverActions ? (
            <div className="absolute top-2 left-2 z-10 flex flex-col gap-1.5">
              <button
                type="button"
                disabled={busy}
                aria-pressed={saved}
                aria-label={saved ? `حذف ${card.title} از علاقه‌مندی` : `افزودن ${card.title} به علاقه‌مندی`}
                className={`w-7 h-7 flex items-center justify-center rounded-full bg-white/90 backdrop-blur-sm shadow-md hover:scale-110 transition-transform disabled:opacity-60 ${saved ? "text-rose-600" : "text-gray-600"}`}
                onClick={(event) => {
                  event.preventDefault();
                  event.stopPropagation();
                  void wishlist.toggle(card.productId).then(setNote);
                }}
              >
                <Heart className={`w-3.5 h-3.5 ${saved ? "fill-current" : ""}`} />
              </button>
              <button
                type="button"
                aria-label={`اشتراک‌گذاری ${card.title}`}
                className="w-7 h-7 flex items-center justify-center rounded-full bg-white/90 backdrop-blur-sm shadow-md hover:scale-110 transition-transform text-gray-600"
                onClick={handleShare}
              >
                <Share2 className="w-3.5 h-3.5" />
              </button>
              <Link
                href={productHref}
                aria-label={`مشاهده ${card.title}`}
                className="w-7 h-7 flex items-center justify-center rounded-full bg-white/90 backdrop-blur-sm shadow-md hover:scale-110 transition-transform text-gray-600"
                onClick={(event) => event.stopPropagation()}
              >
                <Eye className="w-3.5 h-3.5" />
              </Link>
            </div>
          ) : null}
        </div>
        <div className="flex-1 flex flex-col p-3 gap-1.5 min-h-[140px]">
          <h3 className="text-xs sm:text-sm font-bold text-gray-800 line-clamp-2 leading-snug min-h-[36px] group-hover:text-[#2563EB]">
            {card.title}
          </h3>
          {card.reviewCount > 0 && card.averageRating !== null ? (
            <div className="flex items-center gap-1">
              {[1, 2, 3, 4, 5].map((star) => (
                <Star
                  key={star}
                  className={`w-3 h-3 ${star <= Math.round(card.averageRating!) ? "fill-amber-400 text-amber-400" : "text-gray-300"}`}
                />
              ))}
              <span className="text-[10px] text-gray-400">({card.reviewCount.toLocaleString("fa-IR")})</span>
            </div>
          ) : null}
          <div className="flex items-center gap-1.5 flex-wrap">
            <span className="text-sm sm:text-base font-black text-[#2563EB] whitespace-nowrap">
              {formatOfferAmount(card.promotionalAmountExclusiveOfTax ?? card.offerAmountExclusiveOfTax, card.currency)}
            </span>
            {card.promotionalAmountExclusiveOfTax !== null ? (
              <span className="text-[10px] text-gray-400 line-through whitespace-nowrap">
                {formatOfferAmount(card.offerAmountExclusiveOfTax, card.currency)}
              </span>
            ) : null}
          </div>
          <span
            className={`mt-auto inline-flex items-center justify-center gap-1 h-8 rounded-lg text-[11px] font-bold ${
              card.inStock ? "bg-[#2563EB] text-white hover:bg-[#1d4ed8]" : "bg-gray-100 text-gray-400"
            }`}
          >
            <ShoppingBag className="w-3 h-3" />
            {card.inStock ? "افزودن به سبد" : "ناموجود"}
          </span>
        </div>
      </Link>
      {note ? <span role="status" className="px-3 pb-2 text-[10px] text-red-600">{note}</span> : null}
    </article>
  );
}
