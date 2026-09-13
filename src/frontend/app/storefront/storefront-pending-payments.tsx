"use client";

import { Clock, CreditCard } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale, useLocalizedPath } from "../../lib/i18n/locale-context.tsx";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import {
  formatCountdown,
  remainingSecondsFromServer,
  shouldRefreshOnceAtZero,
  toCustomerPendingPaymentMessage,
  type StorefrontPendingPaymentItem,
} from "./storefront-pending-payment-api.ts";
import { retryStorefrontUnpaidPayment } from "./storefront-payment-api.ts";

function copy(locale: "fa" | "en") {
  const fa = locale === "fa";
  return {
    title: fa ? "در انتظار پرداخت" : "Awaiting payment",
    hold: fa
      ? "موجودی این سفارش تا پایان این زمان برای شما نگه داشته می‌شود."
      : "Inventory for this order is held until this time ends.",
    retryHold: fa ? "مهلت رزرو مجدد" : "Retry hold",
    expired: fa ? "مهلت رزرو موجودی پایان یافته است." : "The inventory hold for this order has ended.",
    pay: fa ? "پرداخت" : "Pay",
    retry: fa ? "بررسی موجودی و پرداخت مجدد" : "Check availability and pay again",
    review: fa ? "در انتظار بررسی پرداخت" : "Awaiting payment review",
    failed: fa
      ? "پرداخت ناموفق بود؛ تا پایان مهلت رزرو می‌توانید دوباره تلاش کنید."
      : "Payment failed; you can try again before the hold ends.",
    retryLimit: fa
      ? "تعداد دفعات مجاز رزرو مجدد موجودی برای این سفارش به پایان رسیده است."
      : "No more inventory reservation retries remain for this order.",
    unavailable: fa ? "این سفارش در حال حاضر قابل تأمین نیست." : "This order cannot be supplied right now.",
    cycle: (n: number) => (fa ? `رزرو ${n}` : `Hold ${n}`),
  };
}

function ReservationCountdown({
  item,
  onExpire,
}: {
  item: StorefrontPendingPaymentItem;
  onExpire: () => void;
}) {
  const receivedAt = useRef(Date.now());
  const refreshed = useRef(false);
  const previous = useRef(item.secondsRemaining);
  const [seconds, setSeconds] = useState(() =>
    remainingSecondsFromServer(item.holdEndsAt, item.serverTime, receivedAt.current),
  );

  useEffect(() => {
    receivedAt.current = Date.now();
    refreshed.current = false;
    previous.current = item.secondsRemaining;
    setSeconds(remainingSecondsFromServer(item.holdEndsAt, item.serverTime, receivedAt.current));
  }, [item.checkoutId, item.holdEndsAt, item.serverTime, item.secondsRemaining]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      const next = remainingSecondsFromServer(item.holdEndsAt, item.serverTime, receivedAt.current);
      const prev = previous.current;
      previous.current = next;
      setSeconds(next);
      if (shouldRefreshOnceAtZero(prev, next, refreshed.current)) {
        refreshed.current = true;
        onExpire();
      }
    }, 1000);
    return () => window.clearInterval(timer);
  }, [item.holdEndsAt, item.serverTime, onExpire]);

  return (
    <p className="text-lg md:text-2xl font-black tabular-nums text-[#2563EB] tracking-wide" dir="ltr" data-testid="pending-payment-countdown">
      {formatCountdown(seconds)}
    </p>
  );
}

export function StorefrontPendingPayments({
  items,
  onRefresh,
}: {
  items: StorefrontPendingPaymentItem[];
  onRefresh: () => void;
}) {
  const locale = useLocale();
  const labels = copy(locale);
  const router = useRouter();
  const localizePath = useLocalizedPath();
  const [busyId, setBusyId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Record<string, string>>({});

  if (items.length === 0) {
    return null;
  }

  async function onAction(item: StorefrontPendingPaymentItem) {
    setMessages((current) => {
      const next = { ...current };
      delete next[item.checkoutId];
      return next;
    });
    if (item.primaryAction === "pay") {
      router.push(localizePath(`/payment?checkoutId=${encodeURIComponent(item.checkoutId)}`));
      return;
    }
    if (item.primaryAction !== "retryAfterExpiry" || !item.paymentId) {
      return;
    }
    setBusyId(item.checkoutId);
    try {
      await retryStorefrontUnpaidPayment(item.paymentId, item.checkoutId);
      router.push(localizePath(`/payment?checkoutId=${encodeURIComponent(item.checkoutId)}`));
    } catch (cause) {
      setMessages((current) => ({
        ...current,
        [item.checkoutId]: toCustomerPendingPaymentMessage(cause, locale),
      }));
      onRefresh();
    } finally {
      setBusyId(null);
    }
  }

  return (
    <section className="pt-8 md:pt-10" data-testid="pending-payment-section" dir={locale === "fa" ? "rtl" : "ltr"}>
      <div className="flex items-center gap-2 mb-4">
        <Clock className="w-5 h-5 text-[#2563EB]" />
        <h2 className="text-base md:text-xl font-black text-gray-900">{labels.title}</h2>
      </div>
      <div className="space-y-3">
        {items.map((item) => {
          const localMessage = messages[item.checkoutId];
          const heldPay = item.reservationPresentation === "held" && item.primaryAction === "pay";
          const statusCopy = heldPay
            ? (item.paymentPresentation === "failedRetryable" ? labels.failed : null)
            : localMessage
              ?? (item.supplyStatus && /unavail/i.test(item.supplyStatus) ? labels.unavailable : null)
              ?? (item.paymentPresentation === "awaitingReview" ? labels.review : null)
              ?? (item.paymentPresentation === "retryLimit" || item.hasReachedRetryLimit ? labels.retryLimit : null)
              ?? (item.paymentPresentation === "failedRetryable" ? labels.failed : null)
              ?? (item.reservationPresentation === "ended" ? labels.expired : null);
          return (
            <article
              key={item.checkoutId}
              className="bg-white rounded-2xl border border-gray-200 p-3 md:p-4 shadow-sm"
              data-testid="pending-payment-card"
            >
              <div className="flex flex-col md:flex-row md:items-center gap-3 md:gap-4">
                <div className="flex -space-x-2 rtl:space-x-reverse shrink-0">
                  {item.items.slice(0, 3).map((line, index) => (
                    <div
                      key={`${item.checkoutId}-${index}`}
                      className="w-12 h-12 md:w-14 md:h-14 rounded-xl bg-gray-50 border border-gray-100 overflow-hidden"
                    >
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img src={storefrontMediaUrl(line.mediaAssetId)} alt="" className="w-full h-full object-contain p-1" />
                    </div>
                  ))}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm md:text-base font-black text-gray-900 truncate">{item.orderReference}</p>
                  <p className="text-[11px] md:text-xs text-gray-500 mt-1 line-clamp-1">
                    {item.items.map((line) => line.title).join(" · ")}
                  </p>
                  <p className="text-sm font-black text-[#2563EB] mt-1">
                    {formatOfferAmount(item.payableAmount, item.currency)}
                  </p>
                  {statusCopy ? <p className="text-xs text-gray-600 mt-2 leading-6">{statusCopy}</p> : null}
                </div>
                <div className="shrink-0 text-center md:text-end min-w-[7.5rem]">
                  {item.reservationPresentation === "held" ? (
                    <>
                      <ReservationCountdown item={item} onExpire={onRefresh} />
                      <p className="text-[10px] md:text-xs text-gray-500 mt-1 max-w-[14rem] leading-5">
                        {item.cycleNumber && item.cycleNumber > 1 ? labels.retryHold : labels.hold}
                      </p>
                    </>
                  ) : null}
                  {item.primaryAction === "pay" ? (
                    <button
                      type="button"
                      data-testid="pending-payment-pay"
                      className="mt-2 inline-flex items-center justify-center gap-1 px-4 py-2 rounded-xl bg-[#2563EB] text-white text-xs md:text-sm font-black hover:bg-[#1d4ed8] whitespace-nowrap"
                      onClick={() => void onAction(item)}
                    >
                      <CreditCard className="w-3.5 h-3.5" />
                      {labels.pay}
                    </button>
                  ) : null}
                  {item.primaryAction === "retryAfterExpiry" ? (
                    <button
                      type="button"
                      data-testid="pending-payment-retry"
                      disabled={busyId === item.checkoutId}
                      className="mt-2 inline-flex items-center justify-center px-4 py-2 rounded-xl bg-[#2563EB] text-white text-xs md:text-sm font-black hover:bg-[#1d4ed8] disabled:opacity-60 whitespace-nowrap"
                      onClick={() => void onAction(item)}
                    >
                      {labels.retry}
                    </button>
                  ) : null}
                </div>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}
