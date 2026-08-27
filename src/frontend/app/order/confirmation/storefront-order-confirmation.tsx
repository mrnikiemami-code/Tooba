"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { CheckCircle, Copy, Home, ShieldCheck, ShoppingBag } from "lucide-react";
import { formatOfferAmount } from "../../storefront/storefront-api.ts";
import {
  loadStorefrontCheckout,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
} from "../../storefront/storefront-checkout-api.ts";
import { StorefrontPaymentMethodPicker } from "../../storefront/storefront-payment-methods.tsx";
import {
  loadStorefrontWalletQuote,
  requiresProviderRedirect,
  startStorefrontPayment,
  toCustomerPaymentMessage,
  WALLET_PROVIDER_CODE,
  type StorefrontPaymentMethodId,
  type StorefrontWalletQuote,
} from "../../storefront/storefront-payment-api.ts";

/**
 * تأیید سفارش زنده با پوستهٔ کارت موفقیت Shopeiva.
 * Paid فقط از Host؛ وضعیت جعلی نمایش داده نمی‌شود.
 * کیف پول فقط وقتی Host canPayFullyWithWallet بدهد.
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
  const [quote, setQuote] = useState<StorefrontWalletQuote | null>(null);
  const [method, setMethod] = useState<StorefrontPaymentMethodId>("gateway");
  const [error, setError] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!checkoutId) {
      setError("شناسهٔ سفارش نیست.");
      return;
    }
    void loadStorefrontCheckout(checkoutId)
      .then(async (checkout) => {
        setPage(checkout);
        if (checkout.paymentState !== "Paid") {
          const nextQuote = await loadStorefrontWalletQuote(checkoutId);
          setQuote(nextQuote);
          if (nextQuote?.canPayFullyWithWallet) {
            setMethod("wallet");
          }
        }
      })
      .catch((cause: unknown) => setError(toCustomerCheckoutMessage(cause)));
  }, [checkoutId]);

  if (!page) {
    return (
      <div className="py-16 text-center" data-testid="order-confirmation">
        <p className="text-sm text-red-600">{error ?? "سفارش پیدا نشد."}</p>
        <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
          بازگشت به سبد
        </Link>
      </div>
    );
  }

  const current = page;
  const reference =
    current.sellerOrders.map((order) => order.orderNumber).join("، ") || current.checkoutId || "—";
  const paid = current.paymentState === "Paid";

  async function pay() {
    if (!current.checkoutId) {
      return;
    }
    if (method === "wallet" && !quote?.canPayFullyWithWallet) {
      setError("پرداخت کامل با کیف پول برای این سفارش ممکن نیست.");
      return;
    }
    setPaying(true);
    setError(null);
    try {
      const initiated = await startStorefrontPayment(current.checkoutId, {
        providerCode: method === "wallet" ? WALLET_PROVIDER_CODE : undefined,
      });
      if (requiresProviderRedirect(initiated)) {
        window.location.assign(initiated.redirectUrl);
        return;
      }
      window.location.assign(
        `/payment/result?paymentId=${encodeURIComponent(initiated.paymentId)}&checkoutId=${encodeURIComponent(initiated.checkoutId || current.checkoutId)}`,
      );
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
      setPaying(false);
    }
  }

  async function copyReference() {
    try {
      await navigator.clipboard.writeText(reference);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      setCopied(false);
    }
  }

  return (
    <div className="py-8 md:py-12 flex items-center justify-center" data-testid="order-confirmation">
      <div className="max-w-md w-full mx-auto text-center space-y-4 px-3">
        <div className="bg-white border border-gray-200 rounded-3xl p-6 md:p-10 shadow-2xl">
          <div
            className={`w-16 h-16 md:w-20 md:h-20 mx-auto mb-4 rounded-full flex items-center justify-center ${
              paid ? "bg-emerald-50" : "bg-amber-50"
            }`}
          >
            {paid ? (
              <CheckCircle className="w-8 h-8 md:w-10 md:h-10 text-emerald-500" />
            ) : (
              <ShoppingBag className="w-8 h-8 md:w-10 md:h-10 text-amber-500" />
            )}
          </div>
          <h1 className="text-xl md:text-2xl font-black text-gray-900 mb-1">
            {paid ? "پرداخت با موفقیت انجام شد" : "سفارش ثبت شد"}
          </h1>
          <p className="text-[10px] md:text-xs text-gray-500 mb-6">
            {paid ? "سفارش شما ثبت و پرداخت شد" : "پرداخت هنوز انجام نشده است — این صفحه Paid جعلی نیست"}
          </p>

          <div className="bg-gray-50 rounded-2xl p-4 md:p-6 mb-6 text-right">
            <div className="flex items-center justify-between mb-3">
              <span className="text-[10px] md:text-xs text-gray-500">کد سفارش</span>
              <button
                type="button"
                onClick={() => void copyReference()}
                className="text-[#2563EB] text-[10px] md:text-xs font-bold flex items-center gap-1"
              >
                <Copy className="w-3 h-3" /> {copied ? "کپی شد" : "کپی"}
              </button>
            </div>
            <p className="text-base md:text-lg font-black text-gray-900 tracking-wide" dir="ltr">
              {reference}
            </p>
            <div className="flex items-center justify-between mt-4 pt-4 border-t border-gray-200">
              <span className="text-[10px] md:text-xs text-gray-500">مبلغ قابل پرداخت</span>
              <span className={`text-sm md:text-base font-black ${paid ? "text-emerald-500" : "text-[#2563EB]"}`}>
                {formatOfferAmount(page.payableAmount, page.currency)}
              </span>
            </div>
            <div className="flex items-center justify-between mt-2">
              <span className="text-[10px] md:text-xs text-gray-500">وضعیت پرداخت</span>
              <span className="text-[10px] md:text-xs text-gray-700">{paid ? "Paid" : "در انتظار پرداخت"}</span>
            </div>
          </div>

          {!paid ? (
            <div className="space-y-4 text-right mb-4">
              <StorefrontPaymentMethodPicker selected={method} onChange={setMethod} quote={quote} />
              <button
                type="button"
                disabled={paying}
                onClick={() => void pay()}
                className="w-full px-6 py-3 rounded-2xl bg-[#2563EB] text-white text-sm font-bold disabled:opacity-50 shadow-lg shadow-[#2563EB]/25"
                data-testid="confirmation-pay"
              >
                {paying
                  ? method === "wallet"
                    ? "در حال پرداخت از کیف پول…"
                    : "در حال انتقال…"
                  : method === "wallet"
                    ? "پرداخت با کیف پول"
                    : "پرداخت سفارش"}
              </button>
            </div>
          ) : null}
          {error ? <p className="text-sm text-red-600 mt-3">{error}</p> : null}

          <div className="mt-4 flex flex-wrap justify-center gap-2">
            <Link
              href={`/customer-panel/orders/${current.checkoutId}`}
              className="px-5 py-2.5 rounded-2xl border border-blue-200 text-[#2563EB] text-sm font-bold"
            >
              مشاهده در سفارش‌های من
            </Link>
            <Link
              href="/"
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-2xl bg-[#2563EB] text-white text-sm font-bold"
            >
              <Home className="w-4 h-4" /> صفحه اصلی
            </Link>
          </div>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 p-5 text-sm space-y-2 text-right shadow-sm">
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
                  {line.title} × {line.quantity.toLocaleString("fa-IR")} —{" "}
                  {formatOfferAmount(line.linePayable, line.currency)}
                </p>
              ))}
            </div>
          ))}
          <p className="text-[11px] text-gray-400 flex items-center gap-1 pt-2">
            <ShieldCheck className="w-3 h-3" />
            مالیات و جمع از تصویر سفارش Host است. وضعیت ارسال جعلی نیست.
          </p>
        </div>
      </div>
    </div>
  );
}
