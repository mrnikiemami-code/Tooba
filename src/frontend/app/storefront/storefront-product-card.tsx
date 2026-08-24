import Link from "next/link";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import type { StorefrontProductCard } from "./storefront-model.ts";

/**
 * کارت کالای فروشگاهی با ترکیب دیداری Shopeiva. مبلغ از Offer است.
 */
export function StorefrontProductCardView({ card }: { card: StorefrontProductCard }) {
  return (
    <Link className="sf-card" href={`/products/${card.slug}`}>
      {/* تصویر نمایشی Host خارج از بهینه‌ساز Next است و حقیقت Catalog نیست. */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={storefrontMediaUrl(card.mediaAssetId)} alt="" />
      <div className="sf-card-body">
        <div className="sf-card-title">{card.title}</div>
        <div className="sf-price">{formatOfferAmount(card.offerAmountExclusiveOfTax, card.currency)}</div>
        <div className="sf-meta">
          {card.sellerDisplayName} · {card.inStock ? "موجود" : "ناموجود"}
        </div>
      </div>
    </Link>
  );
}
