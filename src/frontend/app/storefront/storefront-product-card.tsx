"use client";

import Link from "next/link";
import { Heart, ShoppingBag, Star } from "lucide-react";
import { useEffect, useState } from "react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import type { StorefrontProductCard } from "./storefront-model.ts";
import { useStorefrontWishlist } from "./storefront-wishlist-provider.tsx";

/**
 * کارت کالای خانوادهٔ Shopeiva. مبلغ و موجودی از Offer/Inventory است نه از Product.
 */
export function StorefrontProductCardView({ card }: { card: StorefrontProductCard }) {
  const wishlist = useStorefrontWishlist();
  const register = wishlist.register;
  const [note, setNote] = useState<string | null>(null);
  const saved = wishlist.membership.has(card.productId);
  const busy = wishlist.pending.has(card.productId);

  useEffect(() => register(card.productId), [card.productId, register]);

  return (
    <article className="group relative flex flex-col rounded-2xl overflow-hidden bg-white border border-gray-100 hover:shadow-xl hover:-translate-y-1 transition-all duration-300">
      <Link href={`/products/${card.slug}`} className="flex flex-1 flex-col">
        <div className="relative aspect-[4/5] bg-gray-50 overflow-hidden">
        {/* تصویر نمایشی Host خارج از بهینه‌ساز Next است و حقیقت Catalog نیست. */}
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={storefrontMediaUrl(card.mediaAssetId)}
          alt=""
          className="absolute inset-0 w-full h-full object-contain p-3 group-hover:scale-105 transition duration-700"
        />
        {card.promotionLabel ? (
          <span className="absolute top-2 right-2 z-10 bg-[#2563EB] text-white text-[10px] font-bold px-2.5 py-0.5 rounded-lg">
            {card.promotionLabel}
          </span>
        ) : null}
        </div>
        <div className="flex-1 flex flex-col p-3 gap-1.5 min-h-[140px]">
        <h3 className="text-xs sm:text-sm font-bold text-gray-800 line-clamp-2 leading-snug min-h-[36px] group-hover:text-[#2563EB]">
          {card.title}
        </h3>
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
        <p className="text-[10px] text-gray-500">{card.sellerDisplayName}</p>
        {card.reviewCount > 0 && card.averageRating !== null ? (
          <span className="inline-flex items-center gap-1 text-[10px] text-gray-500">
            <Star className="size-3.5 fill-amber-400 text-amber-400" />
            <strong className="text-gray-700">{card.averageRating.toLocaleString("fa-IR", { maximumFractionDigits: 1 })}</strong>
            ({card.reviewCount.toLocaleString("fa-IR")})
          </span>
        ) : null}
        <span
          className={`mt-auto inline-flex items-center justify-center gap-1 h-8 rounded-lg text-[11px] font-bold ${
            card.inStock ? "bg-[#2563EB] text-white" : "bg-gray-100 text-gray-400"
          }`}
        >
          <ShoppingBag className="w-3 h-3" />
          {card.inStock ? "مشاهده و خرید" : "ناموجود"}
        </span>
        </div>
      </Link>
      <button
        type="button"
        disabled={busy}
        aria-pressed={saved}
        aria-label={saved ? `حذف ${card.title} از علاقه‌مندی` : `افزودن ${card.title} به علاقه‌مندی`}
        className={`absolute top-2 left-2 z-10 w-7 h-7 flex items-center justify-center rounded-full bg-white/90 shadow-md disabled:opacity-60 ${saved ? "text-rose-600" : "text-gray-600"}`}
        onClick={() => void wishlist.toggle(card.productId).then(setNote)}
      >
        <Heart className={`w-3.5 h-3.5 ${saved ? "fill-current" : ""}`} />
      </button>
      {note ? <span role="status" className="px-3 pb-2 text-[10px] text-red-600">{note}</span> : null}
    </article>
  );
}
