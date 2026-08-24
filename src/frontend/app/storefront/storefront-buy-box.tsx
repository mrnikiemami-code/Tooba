"use client";

import { useState } from "react";
import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontProductDetailPage } from "./storefront-model.ts";

/**
 * جعبه خرید PDP. جهش Cart را جعل نمی‌کند و تا API سبد فقط آمادهٔ اتصال است.
 */
export function StorefrontBuyBox({ detail }: { detail: StorefrontProductDetailPage }) {
  const [note, setNote] = useState<string | null>(null);
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
        disabled={!detail.cartMutationEnabled || offer.availableUnits <= 0}
        onClick={() => setNote("سبد خرید هنوز به API سبد Tooba وصل نشده است.")}
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
