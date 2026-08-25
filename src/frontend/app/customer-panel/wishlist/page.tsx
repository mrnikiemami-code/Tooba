"use client";

import Link from "next/link";
import { Heart } from "lucide-react";
import { useEffect, useState } from "react";
import { StorefrontProductCardView } from "../../storefront/storefront-product-card";
import {
  loadWishlist,
  type StorefrontWishlistPage,
  WISHLIST_CHANGED_EVENT,
  wishlistEmptyMessage,
  wishlistErrorMessage,
} from "../../storefront/storefront-wishlist-api";

/** صفحهٔ واقعی علاقه‌مندی Shopeiva با کارت‌های ترکیب‌شدهٔ جاری Host. */
export default function CustomerWishlistPage() {
  const [page, setPage] = useState<StorefrontWishlistPage | null | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const refresh = () => {
      void loadWishlist().then((result) => {
        setPage(result);
        setError(null);
      }).catch((cause) => {
        setPage(null);
        setError(wishlistErrorMessage(cause));
      });
    };
    refresh();
    window.addEventListener(WISHLIST_CHANGED_EVENT, refresh);
    return () => window.removeEventListener(WISHLIST_CHANGED_EVENT, refresh);
  }, []);

  if (page === undefined) return <div className="rounded-2xl border bg-white p-8 text-center text-gray-500">در حال دریافت علاقه‌مندی‌ها...</div>;
  if (!page) return <div role="alert" className="rounded-2xl border border-red-100 bg-white p-8 text-center text-red-600">{error}</div>;
  const empty = wishlistEmptyMessage(page.items.length);

  return (
    <div className="space-y-5">
      <header className="flex items-center gap-3 rounded-2xl border border-gray-100 bg-white p-5">
        <span className="flex size-11 items-center justify-center rounded-xl bg-rose-50 text-rose-600"><Heart className="size-5" /></span>
        <div>
          <h1 className="text-xl font-black">علاقه‌مندی‌های من</h1>
          <p className="mt-1 text-xs text-gray-500">{page.totalCount.toLocaleString("fa-IR")} محصول ذخیره‌شده</p>
        </div>
      </header>
      {empty ? (
        <section className="rounded-2xl border border-gray-100 bg-white px-5 py-14 text-center">
          <Heart className="mx-auto size-12 text-gray-200" />
          <p className="mt-4 font-bold text-gray-700">{empty}</p>
          <Link href="/products" className="mt-5 inline-flex rounded-xl bg-[#2563EB] px-5 py-2.5 text-sm font-bold text-white">مشاهده محصولات</Link>
        </section>
      ) : (
        <section className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-4" aria-label="محصولات علاقه‌مندی">
          {page.items.map((item) => <StorefrontProductCardView key={item.productId} card={item.card} />)}
        </section>
      )}
    </div>
  );
}
