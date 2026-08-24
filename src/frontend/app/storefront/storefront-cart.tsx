"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ArrowLeft, CreditCard, Minus, Plus, ShoppingBag, Trash2 } from "lucide-react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import {
  StorefrontCartApiError,
  changeCartLineQuantity,
  loadStorefrontCart,
  removeCartLine,
  toCustomerCartMessage,
  type StorefrontCartPage,
} from "./storefront-cart-api.ts";

/**
 * صفحهٔ سبد با الگوی Shopeiva روی حقیقت Host. تخفیف/مالیات جعلی نشان داده نمی‌شود.
 */
export function StorefrontShopeivaCart() {
  const [cart, setCart] = useState<StorefrontCartPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void loadStorefrontCart()
      .then((page) => {
        setCart(page);
        setError(null);
      })
      .catch((cause: unknown) => {
        setError(toCustomerCartMessage(cause));
      });
  }, []);

  async function mutate(action: () => Promise<StorefrontCartPage>) {
    setBusy(true);
    setError(null);
    try {
      setCart(await action());
    } catch (cause) {
      setError(toCustomerCartMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (!cart || cart.lines.length === 0) {
    return (
      <div className="py-16 md:py-24 text-center">
        <div className="w-28 h-28 md:w-36 md:h-36 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-6">
          <ShoppingBag className="w-14 h-14 md:w-20 md:h-20 text-gray-300" />
        </div>
        <h1 className="text-xl md:text-2xl font-black text-gray-900 mb-2">سبد خرید شما خالی است</h1>
        <p className="text-sm md:text-base text-gray-500 mb-8 max-w-md mx-auto">
          هنوز Offerی به سبد Tooba اضافه نشده است. برای شروع خرید به فروشگاه بروید.
        </p>
        {error ? <p className="text-sm text-red-600 mb-6">{error}</p> : null}
        <Link
          href="/products"
          className="inline-flex items-center gap-2 px-6 py-3 bg-[#2563EB] text-white rounded-xl font-black text-sm hover:bg-[#1d4ed8] shadow-lg shadow-[#2563EB]/25"
        >
          <ArrowLeft className="w-4 h-4" />
          بازگشت به فروشگاه
        </Link>
      </div>
    );
  }

  return (
    <div className="py-6 md:py-10 grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div className="lg:col-span-8 space-y-3">
        <h1 className="text-xl font-black text-gray-900 mb-2">سبد خرید</h1>
        {error ? <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl p-3">{error}</p> : null}
        {cart.lines.map((line) => (
          <div key={line.lineId} className="bg-white rounded-2xl border border-gray-200 p-3 md:p-4 shadow-sm">
            <div className="flex gap-3 md:gap-4">
              <div className="w-20 h-20 md:w-24 md:h-24 rounded-xl bg-gray-50 overflow-hidden shrink-0 border border-gray-100">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={storefrontMediaUrl(line.mediaAssetId)} alt="" className="w-full h-full object-contain p-2" />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <Link
                      href={line.productSlug ? `/products/${line.productSlug}` : "/products"}
                      className="text-sm md:text-base font-bold text-gray-900 hover:text-[#2563EB] line-clamp-1"
                    >
                      {line.title}
                    </Link>
                    <p className="text-[11px] text-gray-500 mt-1">فروشنده: {line.sellerDisplayName}</p>
                    <p className="text-[10px] text-gray-400 mt-0.5">Offer {line.offerId.slice(0, 8)}</p>
                  </div>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void mutate(() => removeCartLine(line.lineId))}
                    className="p-1.5 rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500"
                    title="حذف کالا"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
                <div className="flex flex-wrap items-end justify-between gap-2 mt-2">
                  <div className="flex items-center border border-gray-200 rounded-xl overflow-hidden">
                    <button
                      type="button"
                      disabled={busy || line.quantity <= 1}
                      onClick={() => void mutate(() => changeCartLineQuantity(line.lineId, line.quantity - 1))}
                      className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100 disabled:opacity-30"
                      aria-label="کاهش"
                    >
                      <Minus className="w-3.5 h-3.5" />
                    </button>
                    <span className="w-10 h-8 flex items-center justify-center text-sm font-bold border-x border-gray-200">
                      {line.quantity.toLocaleString("fa-IR")}
                    </span>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void mutate(() => changeCartLineQuantity(line.lineId, line.quantity + 1))}
                      className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100"
                      aria-label="افزایش"
                    >
                      <Plus className="w-3.5 h-3.5" />
                    </button>
                  </div>
                  <div className="text-left">
                    {line.unitAmountExclusiveOfTax != null ? (
                      <p className="text-xs text-gray-500">
                        واحد: {formatOfferAmount(line.unitAmountExclusiveOfTax, line.currency)}
                      </p>
                    ) : null}
                    <p className="text-sm font-black text-[#2563EB]">
                      {line.lineAmountExclusiveOfTax != null
                        ? formatOfferAmount(line.lineAmountExclusiveOfTax, line.currency)
                        : "—"}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
      <aside className="lg:col-span-4 space-y-4">
        <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-md">
          <h2 className="text-base md:text-lg font-black text-gray-900 flex items-center gap-2 mb-4">
            <CreditCard className="w-4 h-4 text-[#2563EB]" />
            خلاصه سفارش
          </h2>
          <div className="space-y-2.5 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">تعداد کالاها</span>
              <span className="font-bold">{cart.itemCount.toLocaleString("fa-IR")}</span>
            </div>
            <div className="flex justify-between border-t border-gray-200 pt-2.5">
              <span className="font-black">قابل پرداخت (برآورد بدون مالیات)</span>
              <span className="font-black text-[#2563EB]">
                {formatOfferAmount(cart.subtotalExclusiveOfTax, cart.currency)}
              </span>
            </div>
            <p className="text-[11px] text-gray-400 leading-6">
              مبلغ از نقل‌قول Pricing روی Offer است. مالیات قطعی در Checkout محاسبه می‌شود. تخفیف جعلی نمایش داده نشده
              است.
            </p>
          </div>
          <Link
            href="/checkout"
            className="mt-4 w-full py-3 rounded-2xl font-black text-sm flex items-center justify-center gap-2 bg-[#2563EB] text-white hover:bg-[#1d4ed8]"
          >
            ادامه به تسویه
          </Link>
        </div>
      </aside>
    </div>
  );
}
