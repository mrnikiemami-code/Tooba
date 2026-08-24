"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { completeStorefrontSandboxPayment, toCustomerPaymentMessage } from "../../storefront/storefront-payment-api.ts";

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
        <p className="text-xs font-black tracking-wide text-amber-800">SANDBOX / DEV PROVIDER</p>
        <p className="text-sm text-amber-900 mt-2">این صفحه بانک واقعی نیست. نتیجه فقط پس از تأیید سرور ثبت می‌شود.</p>
      </div>
      <div className="bg-white rounded-2xl border border-gray-200 p-6 space-y-3 text-sm">
        <h1 className="text-lg font-black text-center">تحویل آزمایشی پرداخت</h1>
        {error ? <p className="text-red-600 text-center">{error}</p> : null}
        <button
          type="button"
          disabled={busy || !paymentId || !attemptId || !reference}
          onClick={() => void finish("success")}
          className="w-full py-3 rounded-xl bg-[#2563EB] text-white font-bold disabled:opacity-50"
        >
          شبیه‌سازی پرداخت موفق
        </button>
        <button
          type="button"
          disabled={busy || !paymentId || !attemptId || !reference}
          onClick={() => void finish("failure")}
          className="w-full py-3 rounded-xl border border-gray-300 font-bold disabled:opacity-50"
        >
          شبیه‌سازی پرداخت ناموفق
        </button>
        <p className="text-center">
          <Link href={checkoutId ? `/order/confirmation?checkoutId=${checkoutId}` : "/cart"} className="text-[#2563EB] text-xs">
            بازگشت
          </Link>
        </p>
      </div>
    </div>
  );
}
