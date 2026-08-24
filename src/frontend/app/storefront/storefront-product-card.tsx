import Link from "next/link";
import { Heart, ShoppingBag, Star } from "lucide-react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import type { StorefrontProductCard } from "./storefront-model.ts";

/**
 * کارت کالای خانوادهٔ Shopeiva. مبلغ و موجودی از Offer/Inventory است نه از Product.
 */
export function StorefrontProductCardView({ card }: { card: StorefrontProductCard }) {
  return (
    <Link
      href={`/products/${card.slug}`}
      className="group relative flex flex-col rounded-2xl overflow-hidden bg-white border border-gray-100 hover:shadow-xl hover:-translate-y-1 transition-all duration-300"
    >
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
        <span className="absolute top-2 left-2 z-10 w-7 h-7 flex items-center justify-center rounded-full bg-white/90 shadow-md text-gray-600">
          <Heart className="w-3.5 h-3.5" />
        </span>
      </div>
      <div className="flex-1 flex flex-col p-3 gap-1.5 min-h-[140px]">
        <h3 className="text-xs sm:text-sm font-bold text-gray-800 line-clamp-2 leading-snug min-h-[36px] group-hover:text-[#2563EB]">
          {card.title}
        </h3>
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className="text-sm sm:text-base font-black text-[#2563EB] whitespace-nowrap">
            {formatOfferAmount(card.offerAmountExclusiveOfTax, card.currency)}
          </span>
        </div>
        <div className="flex items-center gap-1">
          {[1, 2, 3, 4, 5].map((index) => (
            <Star key={index} className={`w-2.5 h-2.5 ${index <= 4 ? "fill-amber-400 text-amber-400" : "text-gray-300"}`} />
          ))}
        </div>
        <p className="text-[10px] text-gray-500">{card.sellerDisplayName}</p>
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
  );
}
