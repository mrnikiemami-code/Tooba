"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, useEffect, useMemo, useState } from "react";
import { formatOfferAmount } from "../../storefront/storefront-api.ts";
import {
  clearCartSession,
  persistPaymentResultProofFromAccess,
  resolveCommittedCheckoutAccess,
  writePaymentResultProof,
} from "../../storefront/storefront-cart-api.ts";
import { loadStorefrontCheckout, type StorefrontCheckoutPage } from "../../storefront/storefront-checkout-api.ts";
import {
  loadStorefrontPayment,
  resetStorefrontPaymentIdempotency,
  retryStorefrontManualPayment,
  retryStorefrontUnpaidPayment,
  shouldPollStorefrontPayment,
  submitStorefrontManualEvidence,
  toCustomerPaymentMessage,
  uploadStorefrontManualProof,
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
  const [tracking, setTracking] = useState("");
  const [proofId, setProofId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const resolvedPaymentId = paymentId ?? "";
    if (!resolvedPaymentId) {
      setError("شناسهٔ پرداخت نیست.");
      return;
    }
    let cancelled = false;
    let timer: number | null = null;
    let hardStop: number | null = null;
    let inFlight = false;

    function stopPolling() {
      if (timer != null) {
        window.clearInterval(timer);
        timer = null;
      }
      if (hardStop != null) {
        window.clearTimeout(hardStop);
        hardStop = null;
      }
    }

    async function refresh(): Promise<boolean> {
      if (inFlight || cancelled) {
        return false;
      }
      inFlight = true;
      try {
        const nextPayment = await loadStorefrontPayment(resolvedPaymentId, checkoutId);
        if (cancelled) {
          return false;
        }
        setPayment(nextPayment);
        setError(null);
        const orderId = checkoutId || nextPayment.checkoutId;
        if (orderId) {
          const nextCheckout = await loadStorefrontCheckout(orderId, resolvedPaymentId);
          if (!cancelled) {
            setCheckout(nextCheckout);
          }
        }
        if (!shouldPollStorefrontPayment(nextPayment)) {
          stopPolling();
          return false;
        }
        return true;
      } catch (cause: unknown) {
        if (!cancelled) {
          setError(toCustomerPaymentMessage(cause));
          stopPolling();
        }
        return false;
      } finally {
        inFlight = false;
      }
    }

    void refresh().then((shouldContinue) => {
      if (cancelled || !shouldContinue) {
        return;
      }
      timer = window.setInterval(() => {
        void refresh().then((keepGoing) => {
          if (!keepGoing) {
            stopPolling();
          }
        });
      }, 1500);
      hardStop = window.setTimeout(() => stopPolling(), 20000);
    });

    return () => {
      cancelled = true;
      stopPolling();
    };
  }, [paymentId, checkoutId]);

  const paid = checkout?.paymentState === "Paid" || payment?.status === "Succeeded";
  const failed = payment?.status === "Failed" || payment?.status === "Cancelled";
  const unpaidExpired = payment?.status === "Expired";
  const manual = (payment?.providerCode ?? "").toLowerCase() === "manual";
  const awaitingSubmit = Boolean(payment?.canSubmitManualEvidence);
  const awaitingAdmin = manual && payment?.status === "Pending" && Boolean(payment?.evidenceSubmittedAt);
  const rejected = manual && failed;
  const orderNumber = payment?.orderNumber || checkout?.sellerOrders?.[0]?.orderNumber || "";
  const proofMode = (payment?.manualProofRequirement ?? "Optional").toLowerCase();
  const showUpload = proofMode === "optional" || proofMode === "required";

  useEffect(() => {
    if (!paid && !awaitingAdmin) {
      return;
    }
    if (paymentId && payment?.checkoutId) {
      const access = resolveCommittedCheckoutAccess(payment.checkoutId, paymentId);
      persistPaymentResultProofFromAccess(
        { paymentId, checkoutId: payment.checkoutId },
        access,
      );
      if (access.cartId) {
        writePaymentResultProof({
          paymentId,
          checkoutId: payment.checkoutId,
          cartId: access.cartId,
          guestSecret: access.guestSecret ?? "",
        });
      }
    }
    clearCartSession();
  }, [paid, awaitingAdmin, paymentId, payment?.checkoutId]);

  const orderHref = useMemo(() => {
    const id = checkout?.checkoutId || payment?.checkoutId;
    if (!id) {
      return null;
    }
    return `/customer-panel/orders/${id}`;
  }, [checkout?.checkoutId, payment?.checkoutId]);

  const statusLabel = paid
    ? "پرداخت موفق"
    : unpaidExpired
      ? "مهلت پرداخت این سفارش به پایان رسیده است."
      : failed && !manual
      ? "پرداخت ناموفق"
      : awaitingSubmit
        ? "منتظر ثبت اطلاعات پرداخت"
        : awaitingAdmin
          ? "در انتظار تایید"
          : rejected
            ? "رد شده"
            : "در حال انتقال به درگاه";

  async function onSubmitManual() {
    if (!paymentId) {
      return;
    }
    const trimmed = tracking.trim();
    if (!trimmed) {
      setError("شماره پیگیری پرداخت الزامی است.");
      return;
    }
    if (proofMode === "required" && !proofId) {
      setError("بارگذاری مدرک پرداخت الزامی است.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const next = await submitStorefrontManualEvidence(paymentId, trimmed, proofId);
      setPayment(next);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function onRetryUnpaid() {
    if (!paymentId) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const next = await retryStorefrontUnpaidPayment(paymentId);
      setPayment(next);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function onRetryManual() {
    if (!paymentId) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const next = await retryStorefrontManualPayment(paymentId);
      setPayment(next);
      setTracking("");
      setProofId(null);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function onUpload(file: File | undefined) {
    if (!file || !paymentId) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const id = await uploadStorefrontManualProof(paymentId, file);
      setProofId(id);
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="py-10 max-w-lg mx-auto space-y-4">
      <div className="bg-white rounded-2xl border border-gray-200 p-6 text-center space-y-3">
        {error ? <p className="text-sm text-red-600">{error}</p> : null}
        <h1 className="text-xl font-black">{statusLabel}</h1>
        {orderNumber ? (
          <p className="text-sm text-gray-700">
            شماره سفارش: <span dir="ltr" className="font-bold">{orderNumber}</span>
          </p>
        ) : null}
        {payment ? (
          <p className="text-sm font-bold">مبلغ: {formatOfferAmount(payment.amount, payment.currency)}</p>
        ) : null}
        {awaitingAdmin && payment?.customerTransferReference ? (
          <p className="text-sm text-gray-600">
            شماره پیگیری پرداخت: <span dir="ltr">{payment.customerTransferReference}</span>
            {payment.proofMediaAssetId ? " · مدرک ثبت شد" : ""}
          </p>
        ) : null}

        {awaitingSubmit ? (
          <form
            className="text-right space-y-3"
            onSubmit={(event) => {
              event.preventDefault();
              void onSubmitManual();
            }}
          >
            {payment?.manualPaymentInstructions ? (
              <p className="text-sm text-gray-600 whitespace-pre-line">{payment.manualPaymentInstructions}</p>
            ) : null}
            <label className="block text-sm font-bold">
              شماره پیگیری پرداخت
              <input
                value={tracking}
                onChange={(event) => setTracking(event.target.value)}
                maxLength={64}
                className="mt-1 w-full rounded-xl border border-gray-200 px-3 py-2 font-medium"
                dir="ltr"
              />
            </label>
            {showUpload ? (
              <label className="block text-sm font-bold">
                مدرک پرداخت {proofMode === "required" ? "(الزامی)" : "(اختیاری)"}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/webp,application/pdf"
                  className="mt-1 block w-full text-xs"
                  onChange={(event) => void onUpload(event.target.files?.[0])}
                />
                {proofId ? <span className="text-xs text-emerald-700">فایل ثبت شد</span> : null}
              </label>
            ) : null}
            <button
              type="submit"
              disabled={busy}
              className="w-full py-3 rounded-xl bg-[#2563EB] text-white font-bold disabled:opacity-50"
            >
              ثبت اطلاعات پرداخت
            </button>
          </form>
        ) : null}

        {unpaidExpired ? (
          <div className="space-y-3" data-testid="payment-unpaid-expired">
            {payment?.canRetryUnpaid ? (
              <button
                type="button"
                disabled={busy}
                onClick={() => void onRetryUnpaid()}
                className="w-full py-3 rounded-xl bg-[#2563EB] text-white font-bold disabled:opacity-50"
                data-testid="payment-unpaid-retry"
              >
                تلاش مجدد پرداخت
              </button>
            ) : null}
            {orderHref ? (
              <Link href={orderHref} className="inline-flex px-5 py-2.5 rounded-xl border border-blue-200 text-[#2563EB] text-sm font-bold">
                مشاهده سفارش
              </Link>
            ) : null}
          </div>
        ) : null}

        {rejected ? (
          <button
            type="button"
            disabled={busy}
            onClick={() => void onRetryManual()}
            className="w-full py-3 rounded-xl border border-gray-300 font-bold disabled:opacity-50"
          >
            تلاش مجدد
          </button>
        ) : null}

        {failed && !manual && checkoutId ? (
          <Link
            href={`/payment?checkoutId=${checkoutId}`}
            onClick={() => resetStorefrontPaymentIdempotency(checkoutId, "gateway")}
            className="inline-flex px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold"
          >
            تلاش مجدد برای پرداخت
          </Link>
        ) : null}

        {(paid || awaitingAdmin) && orderHref ? (
          <div className="flex flex-wrap justify-center gap-2">
            <Link href={orderHref} className="inline-flex px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
              مشاهده سفارش
            </Link>
            <Link href="/products" className="inline-flex px-5 py-2.5 rounded-xl border border-blue-200 text-[#2563EB] text-sm font-bold">
              ادامه خرید
            </Link>
          </div>
        ) : null}
      </div>
    </div>
  );
}
