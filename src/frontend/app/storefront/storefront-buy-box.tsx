"use client";

import { useState } from "react";
import { useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { formatOfferAmount } from "./storefront-api.ts";
import { StorefrontCartApiError, addOfferToCart } from "./storefront-cart-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";

/**
 * جعبه خرید PDP. جهش Cart را جعل نمی‌کند و تا API سبد فقط آمادهٔ اتصال است.
 */
export function StorefrontBuyBox({ detail }: { detail: StorefrontProductDetailPage }) {
  const localizePath = useLocalizedPath();
  const [note, setNote] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const offer = detail.primaryOffer;
  return (
    <div className="sf-buy">
      <h1>{detail.title}</h1>
      <p className="sf-meta">
        {detail.brandName ? `${detail.brandName} · ` : ""}
        {detail.categoryName}
      </p>
      <p className="sf-price">{formatOfferAmount(offer.amountExclusiveOfTax, offer.currency)}</p>
      <p className="sf-meta">
        فروشنده: {offer.sellerDisplayName}
        {offer.sellerSku ? ` · کد فروشنده ${offer.sellerSku}` : ""}
      </p>
      <p className="sf-meta">
        موجودی قابل فروش: {offer.availableUnits.toLocaleString("fa-IR")} · مالیات: {offer.taxCategoryLabel}
      </p>
      <button
        className="sf-cta"
        type="button"
        disabled={!detail.cartMutationEnabled || offer.availableUnits <= 0 || busy}
        onClick={() => {
          void (async () => {
            setBusy(true);
            setNote(null);
            try {
              await addOfferToCart(offer.offerId, 1);
              setNote(`محصول ${detail.title} به سبد خرید اضافه شد`);
              window.location.assign(localizePath("/cart"));
            } catch (cause) {
              setNote(cause instanceof StorefrontCartApiError ? cause.detail ?? cause.message : "افزودن به سبد شکست خورد.");
            } finally {
              setBusy(false);
            }
          })();
        }}
      >
        افزودن به سبد
      </button>
      {note ? <p className="sf-meta">{note}</p> : null}
      {detail.otherSellers.length > 0 ? (
        <div className="sf-sellers">
          <strong>فروشندگان دیگر</strong>
          {detail.otherSellers.map((seller) => (
            <p key={seller.offerId} className="sf-meta">
              {seller.sellerDisplayName} · {formatOfferAmount(seller.amountExclusiveOfTax, seller.currency)} ·{" "}
              {seller.inStock ? "موجود" : "ناموجود"}
            </p>
          ))}
        </div>
      ) : null}
    </div>
  );
}
