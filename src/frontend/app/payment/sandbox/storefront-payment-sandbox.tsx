"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";
import { formatOfferAmount } from "../../storefront/storefront-api.ts";
import {
  completeStorefrontSandboxPayment,
  loadStorefrontSandboxContext,
  toCustomerPaymentMessage,
  type StorefrontSandboxContext,
} from "../../storefront/storefront-payment-api.ts";

/**
 * تحویل sandbox/dev. موفقیت را قبل از Verify اعلام نمی‌کند.
 */
export function StorefrontPaymentSandbox() {
  return (
    <Suspense fallback={<p className="py-16 text-center text-sm text-gray-500">در حال بارگذاری درگاه آزمایشی…</p>}>
      <SandboxBody />
    </Suspense>
  );
}

function SandboxBody() {
  const params = useSearchParams();
  const router = useRouter();
  const paymentId = params.get("paymentId") ?? "";
  const attemptId = params.get("attemptId") ?? "";
  const reference = params.get("ref") ?? "";
  const checkoutId = params.get("checkoutId") ?? "";
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [context, setContext] = useState<StorefrontSandboxContext | null>(null);

  useEffect(() => {
    if (!paymentId) {
      return;
    }
    let cancelled = false;
    void loadStorefrontSandboxContext(paymentId)
      .then((page) => {
        if (!cancelled) {
          setContext(page);
        }
      })
      .catch((cause: unknown) => {
        if (!cancelled) {
          setError(toCustomerPaymentMessage(cause));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [paymentId]);

  async function finish(outcome: "success" | "failure") {
    setBusy(true);
    setError(null);
    try {
      const result = await completeStorefrontSandboxPayment(paymentId, attemptId, reference, outcome);
      const query = new URLSearchParams({
        paymentId: result.paymentId,
        checkoutId: result.checkoutId || checkoutId,
      });
      router.push(`/payment/result?${query.toString()}`);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
      setBusy(false);
    }
  }

  return (
    <div className="py-10 max-w-lg mx-auto space-y-4">
      <div className="rounded-2xl border-2 border-dashed border-amber-400 bg-amber-50 p-5 text-center">
        <p className="text-xs font-black tracking-wide text-amber-800">SANDBOX / TEST</p>
        <p className="text-sm text-amber-900 mt-2">این صفحه بانک واقعی نیست. نتیجه فقط پس از تأیید سرور ثبت می‌شود.</p>
      </div>
      <div className="bg-white rounded-2xl border border-gray-200 p-6 space-y-3 text-sm">
        <h1 className="text-lg font-black text-center">درگاه آزمایشی پرداخت</h1>
        {context ? (
          <dl className="space-y-1 text-gray-700">
            <div className="flex justify-between gap-3"><dt>فروشگاه</dt><dd className="font-bold">{context.storeName}</dd></div>
            <div className="flex justify-between gap-3"><dt>شماره سفارش</dt><dd className="font-bold" dir="ltr">{context.orderNumber}</dd></div>
            <div className="flex justify-between gap-3"><dt>مبلغ</dt><dd className="font-bold">{formatOfferAmount(context.amount, context.currency)}</dd></div>
            <div className="flex justify-between gap-3"><dt>روش</dt><dd className="font-bold">{context.providerLabel}</dd></div>
          </dl>
        ) : null}
        {error ? <p className="text-red-600 text-center">{error}</p> : null}
        <p className="text-center text-xs text-gray-500">در حال انتقال به درگاه</p>
        <button
          type="button"
          disabled={busy || !paymentId || !attemptId || !reference}
          onClick={() => void finish("success")}
          className="w-full py-3 rounded-xl bg-[#2563EB] text-white font-bold disabled:opacity-50"
        >
          پرداخت موفق
        </button>
        <button
          type="button"
          disabled={busy || !paymentId || !attemptId || !reference}
          onClick={() => void finish("failure")}
          className="w-full py-3 rounded-xl border border-gray-300 font-bold disabled:opacity-50"
        >
          پرداخت ناموفق
        </button>
        <p className="text-center">
          <Link
            href={checkoutId ? `/payment?checkoutId=${encodeURIComponent(checkoutId)}` : "/payment"}
            className="text-[#2563EB] text-xs"
          >
            بازگشت به پرداخت
          </Link>
        </p>
      </div>
    </div>
  );
}
