"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { formatOfferAmount } from "../../storefront/storefront-api.ts";
import {
  loadStorefrontCheckout,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
} from "../../storefront/storefront-checkout-api.ts";
import { startStorefrontPayment, toCustomerPaymentMessage } from "../../storefront/storefront-payment-api.ts";

/**
 * تأیید سفارش زنده. Paid نمایش داده نمی‌شود.
 */
export function StorefrontOrderConfirmation() {
  return (
    <Suspense fallback={<p className="py-16 text-center text-sm text-gray-500">در حال بارگذاری سفارش…</p>}>
      <ConfirmationBody />
    </Suspense>
  );
}

function ConfirmationBody() {
  const params = useSearchParams();
  const checkoutId = params.get("checkoutId");
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);

  useEffect(() => {
    if (!checkoutId) {
      setError("شناسهٔ سفارش نیست.");
      return;
    }
    void loadStorefrontCheckout(checkoutId)
      .then(setPage)
      .catch((cause: unknown) => setError(toCustomerCheckoutMessage(cause)));
  }, [checkoutId]);

  if (!page) {
    return (
      <div className="py-16 text-center">
        <p className="text-sm text-red-600">{error ?? "سفارش پیدا نشد."}</p>
        <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
          بازگشت به سبد
        </Link>
      </div>
    );
  }

  const current = page;
  const reference = current.sellerOrders.map((order) => order.orderNumber).join("، ");
  const paid = current.paymentState === "Paid";

  async function pay() {
    if (!current.checkoutId) {
      return;
    }
    setPaying(true);
    setError(null);
    try {
      const initiated = await startStorefrontPayment(current.checkoutId);
      window.location.assign(initiated.redirectUrl);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
      setPaying(false);
    }
  }

  return (
    <div className="py-8 md:py-12 max-w-3xl mx-auto space-y-4">
      <div className="bg-white rounded-2xl border border-gray-200 p-6 text-center shadow-sm">
        <h1 className="text-xl font-black mb-2">{paid ? "پرداخت تأیید شد" : "سفارش ثبت شد"}</h1>
        <p className="text-sm text-gray-500 mb-4">
          {paid ? "وضعیت از تصویر سفارش Host است." : "پرداخت هنوز انجام نشده است."}
        </p>
        <p className="text-sm font-bold">شمارهٔ مرجع: {reference || current.checkoutId}</p>
        <p className={`text-xs mt-3 rounded-xl p-3 ${paid ? "text-emerald-800 bg-emerald-50" : "text-amber-700 bg-amber-50"}`}>
          {paid ? "سفارش Paid است." : "وضعیت فعلی: در انتظار پرداخت. این صفحه پرداخت موفق نیست."}
        </p>
        {!paid ? (
          <button
            type="button"
            disabled={paying}
            onClick={() => void pay()}
            className="mt-4 px-6 py-3 rounded-xl bg-[#2563EB] text-white text-sm font-bold disabled:opacity-50"
          >
            {paying ? "در حال انتقال…" : "پرداخت سفارش"}
          </button>
        ) : null}
        {error ? <p className="text-sm text-red-600 mt-3">{error}</p> : null}
      </div>
      <div className="bg-white rounded-2xl border border-gray-200 p-5 text-sm space-y-2">
        <p>
          گیرنده: {page.recipientName} — {page.contactMobile}
        </p>
        <p>
          ارسال: {page.provinceName}، {page.cityName}، {page.postalAddress}
        </p>
        <p>روش ارسال: {page.shippingMethodLabel}</p>
        {page.sellerOrders.map((order) => (
          <div key={order.sellerOrderId} className="border-t border-gray-100 pt-2">
            <p className="font-bold">
              {order.sellerDisplayName} · {order.orderNumber} · {order.status}
            </p>
            {order.lines.map((line) => (
              <p key={line.offerId} className="text-gray-600">
                {line.title} × {line.quantity.toLocaleString("fa-IR")} — {formatOfferAmount(line.linePayable, line.currency)}
              </p>
            ))}
          </div>
        ))}
        <p className="font-black text-[#2563EB] pt-2">قابل پرداخت: {formatOfferAmount(page.payableAmount, page.currency)}</p>
        <p className="text-[11px] text-gray-400 flex items-center gap-1">
          <ShieldCheck className="w-3 h-3" />
          مالیات و جمع از تصویر سفارش Host است.
        </p>
      </div>
    </div>
  );
}
