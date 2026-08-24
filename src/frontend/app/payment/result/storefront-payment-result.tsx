"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { formatOfferAmount } from "../../storefront/storefront-api.ts";
import { loadStorefrontCheckout, type StorefrontCheckoutPage } from "../../storefront/storefront-checkout-api.ts";
import {
  loadStorefrontPayment,
  toCustomerPaymentMessage,
  type StorefrontPaymentPage,
} from "../../storefront/storefront-payment-api.ts";

/**
 * نتیجه را از Host می‌خواند. تا Paid سفارش، موفقیت نمایش داده نمی‌شود.
 */
export function StorefrontPaymentResult() {
  return (
    <Suspense fallback={<p className="py-16 text-center text-sm text-gray-500">در حال خواندن نتیجهٔ پرداخت…</p>}>
      <ResultBody />
    </Suspense>
  );
}

function ResultBody() {
  const params = useSearchParams();
  const paymentId = params.get("paymentId");
  const checkoutId = params.get("checkoutId");
  const [payment, setPayment] = useState<StorefrontPaymentPage | null>(null);
  const [checkout, setCheckout] = useState<StorefrontCheckoutPage | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const resolvedPaymentId = paymentId ?? "";
    if (!resolvedPaymentId) {
      setError("شناسهٔ پرداخت نیست.");
      return;
    }
    let cancelled = false;
    async function refresh() {
      try {
        const nextPayment = await loadStorefrontPayment(resolvedPaymentId);
        if (cancelled) {
          return;
        }
        setPayment(nextPayment);
        const orderId = checkoutId || nextPayment.checkoutId;
        if (orderId) {
          const nextCheckout = await loadStorefrontCheckout(orderId);
          if (!cancelled) {
            setCheckout(nextCheckout);
          }
        }
      } catch (cause: unknown) {
        if (!cancelled) {
          setError(toCustomerPaymentMessage(cause));
        }
      }
    }
    void refresh();
    const timer = window.setInterval(() => void refresh(), 1500);
    const stop = window.setTimeout(() => window.clearInterval(timer), 20000);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
      window.clearTimeout(stop);
    };
  }, [paymentId, checkoutId]);

  const paid = checkout?.paymentState === "Paid";
  const failed = payment?.status === "Failed" || payment?.status === "Cancelled";
  const pending = !paid && !failed;

  return (
    <div className="py-10 max-w-lg mx-auto space-y-4">
      <div className="bg-white rounded-2xl border border-gray-200 p-6 text-center space-y-3">
        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        {paid ? (
          <>
            <h1 className="text-xl font-black">پرداخت تأیید شد</h1>
            <p className="text-sm text-gray-600">وضعیت از تصویر سفارش Host خوانده شده است.</p>
          </>
        ) : null}
        {failed ? (
          <>
            <h1 className="text-xl font-black">پرداخت تأیید نشد</h1>
            <p className="text-sm text-gray-600">در صورت کسر وجه، وضعیت را دوباره بررسی کنید.</p>
          </>
        ) : null}
        {pending && !error ? (
          <>
            <h1 className="text-xl font-black">در انتظار تأیید پرداخت</h1>
            <p className="text-sm text-gray-600">این صفحه پرداخت موفق نیست تا سفارش Paid شود.</p>
          </>
        ) : null}
        {payment ? (
          <p className="text-sm font-bold">
            مبلغ: {formatOfferAmount(payment.amount, payment.currency)} · وضعیت پرداخت: {payment.status}
          </p>
        ) : null}
        {failed && checkoutId ? (
          <Link
            href={`/order/confirmation?checkoutId=${checkoutId}`}
            className="inline-flex px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold"
          >
            تلاش دوباره
          </Link>
        ) : null}
        {paid ? (
          <Link href="/" className="inline-flex px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
            ادامه خرید
          </Link>
        ) : null}
      </div>
    </div>
  );
}
