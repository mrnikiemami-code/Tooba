"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Check, ChevronLeft, CreditCard, ShoppingBag, Truck } from "lucide-react";
import { formatOfferAmount } from "../storefront/storefront-api.ts";
import { loadStorefrontCheckout, toCustomerCheckoutMessage, type StorefrontCheckoutPage } from "../storefront/storefront-checkout-api.ts";
import { readStoredCheckoutId } from "../storefront/storefront-shipping-api.ts";

/**
 * قالب پرداخت T003 — فقط handoff از Shipping؛ روش پرداخت در این Task پیاده نمی‌شود.
 */
export function StorefrontPaymentHandoff() {
  const params = useSearchParams();
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const checkoutId = params.get("checkoutId") || readStoredCheckoutId();
    if (!checkoutId) {
      setError("سفارش ارسال‌شده پیدا نشد. از مرحلهٔ ارسال ادامه دهید.");
      return;
    }
    void loadStorefrontCheckout(checkoutId)
      .then((result) => {
        setPage(result);
        setError(null);
      })
      .catch((cause: unknown) => setError(toCustomerCheckoutMessage(cause)));
  }, [params]);

  return (
    <div className="min-h-screen bg-white pb-10" data-testid="payment-handoff-page">
      <div className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 md:pt-6">
        <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-[#E53935] to-[#b71c1c] min-h-[180px]">
          <div className="relative z-10 flex flex-col items-center justify-center text-center p-8">
            <span className="inline-flex items-center gap-1.5 bg-white/15 text-white text-xs font-bold px-3 py-1.5 rounded-full mb-3">
              <CreditCard className="w-3.5 h-3.5" />
              پرداخت (قالب — T003)
            </span>
            <h1 className="text-2xl font-black text-white">پرداخت سفارش</h1>
            <p className="text-sm text-white/80 mt-1">انتخاب روش پرداخت در وظیفهٔ بعدی متصل می‌شود</p>
          </div>
        </div>
        <div className="flex items-center justify-center gap-2 -mt-5 relative z-20 mb-8">
          {[
            { id: 1, label: "سبد خرید", icon: ShoppingBag },
            { id: 2, label: "ارسال", icon: Truck },
            { id: 3, label: "پرداخت", icon: CreditCard },
          ].map((s, i) => (
            <div key={s.id} className="flex items-center gap-2">
              <div
                className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-sm font-bold ${
                  s.id === 3 ? "bg-[#E53935] text-white" : "bg-red-50 text-red-600"
                }`}
              >
                <s.icon className="w-4 h-4" />
                {s.id < 3 ? <Check className="w-3.5 h-3.5" /> : null}
                {s.label}
              </div>
              {i < 2 ? <ChevronLeft className="w-4 h-4 text-red-400" /> : null}
            </div>
          ))}
        </div>

        {error ? (
          <div className="text-center py-10">
            <p className="text-sm text-red-600 mb-4">{error}</p>
            <Link href="/shipping" className="inline-flex px-5 py-2.5 rounded-xl bg-[#E53935] text-white text-sm font-bold">
              بازگشت به ارسال
            </Link>
          </div>
        ) : !page ? (
          <p className="text-center text-sm text-gray-500 py-10">در حال بارگذاری وضعیت سفارش…</p>
        ) : (
          <div className="max-w-xl mx-auto bg-white rounded-2xl border border-gray-200 p-5 shadow-sm space-y-3" data-testid="payment-handoff-summary">
            <h2 className="font-black text-gray-900">خلاصهٔ سفارش ثبت‌شده</h2>
            <p className="text-sm text-gray-600">
              روش ارسال: <span className="font-bold text-gray-900">{page.shippingMethodLabel}</span>
            </p>
            <p className="text-sm text-gray-600">
              گیرنده: <span className="font-bold text-gray-900">{page.recipientName}</span> — {page.cityName}
            </p>
            <p className="text-sm text-gray-600">
              هزینه ارسال:{" "}
              <span className="font-bold">
                {page.shippingAmount === 0 ? "رایگان / صفر پیکربندی" : formatOfferAmount(page.shippingAmount, page.currency)}
              </span>
            </p>
            <p className="text-base font-black text-[#E53935]">
              قابل پرداخت: {formatOfferAmount(page.payableAmount, page.currency)}
            </p>
            <p className="text-xs text-gray-400 leading-6">
              این صفحه فقط handoff معتبر از Shipping است. فرم کارت/CVV قالب Shopeiva عمداً اینجا پیاده نشده و در TB-P10-T003 متصل می‌شود.
            </p>
            <Link href="/shipping" className="inline-flex text-sm text-[#E53935] font-bold">
              بازگشت به ارسال
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
