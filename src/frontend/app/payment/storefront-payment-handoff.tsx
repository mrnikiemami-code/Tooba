"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Check, ChevronLeft, CreditCard, ShoppingBag, Truck } from "lucide-react";
import { formatOfferAmount } from "../storefront/storefront-api.ts";
import {
  loadStorefrontCheckout,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
} from "../storefront/storefront-checkout-api.ts";
import { readStoredCheckoutId } from "../storefront/storefront-shipping-api.ts";
import { StorefrontPaymentMethodPicker } from "../storefront/storefront-payment-methods.tsx";
import {
  loadStorefrontPaymentMethods,
  loadStorefrontWalletQuote,
  requiresProviderRedirect,
  startStorefrontPayment,
  toCustomerPaymentMessage,
  WALLET_PROVIDER_CODE,
  type StorefrontPaymentMethodId,
  type StorefrontWalletQuote,
} from "../storefront/storefront-payment-api.ts";

/**
 * پرداخت ویترین — handoff معتبر از Shipping + روش‌های Store-enabled واقعی.
 * فرم ورود شماره کارت / کد امنیتی / تاریخ انقضای قالب Shopeiva اینجا نیست و عمداً اضافه نمی‌شود.
 */
export function StorefrontPaymentHandoff() {
  const params = useSearchParams();
  const router = useRouter();
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [quote, setQuote] = useState<StorefrontWalletQuote | null>(null);
  const [enabledCodes, setEnabledCodes] = useState<string[]>([]);
  const [method, setMethod] = useState<StorefrontPaymentMethodId | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);

  useEffect(() => {
    const checkoutId = params.get("checkoutId") || readStoredCheckoutId();
    if (!checkoutId) {
      setError("سفارش ارسال‌شده پیدا نشد. از مرحلهٔ ارسال ادامه دهید.");
      return;
    }
    let cancelled = false;
    void (async () => {
      try {
        const [checkout, methodsPage, nextQuote] = await Promise.all([
          loadStorefrontCheckout(checkoutId),
          loadStorefrontPaymentMethods(),
          loadStorefrontWalletQuote(checkoutId),
        ]);
        if (cancelled) return;
        setPage(checkout);
        setQuote(nextQuote);
        const codes = methodsPage.methods.map((m) => m.code.toLowerCase());
        setEnabledCodes(codes);
        const walletOk = Boolean(nextQuote?.canPayFullyWithWallet);
        if (walletOk) {
          setMethod("wallet");
        } else if (codes.includes("gateway")) {
          setMethod("gateway");
        } else if (codes.includes("manual") || methodsPage.manualCardToCardEnabled) {
          setMethod("manual");
        } else {
          setMethod(null);
        }
        setError(null);
      } catch (cause: unknown) {
        if (!cancelled) setError(toCustomerCheckoutMessage(cause));
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [params]);

  const paid = page?.paymentState === "Paid" || page?.canInitiatePayment === false;
  const hasAnyMethod = useMemo(() => {
    if (quote?.canPayFullyWithWallet) return true;
    return enabledCodes.includes("gateway") || enabledCodes.includes("manual");
  }, [enabledCodes, quote]);

  async function pay() {
    if (!page?.checkoutId || !method || paid) return;
    if (method === "wallet" && !quote?.canPayFullyWithWallet) {
      setError("پرداخت کامل با کیف پول برای این سفارش ممکن نیست.");
      return;
    }
    if (method === "gateway" && !enabledCodes.includes("gateway")) {
      setError("روش پرداخت درگاه برای این فروشگاه فعال نیست.");
      return;
    }
    if (method === "manual" && !enabledCodes.includes("manual") && !quote?.manualCardToCardEnabled) {
      setError("پرداخت کارت به کارت برای این فروشگاه فعال نیست.");
      return;
    }
    setPaying(true);
    setError(null);
    try {
      const initiated = await startStorefrontPayment(page.checkoutId, {
        providerCode:
          method === "wallet" ? WALLET_PROVIDER_CODE : method === "manual" ? "manual" : "gateway",
      });
      if (requiresProviderRedirect(initiated)) {
        // مسیر داخلی (sandbox): push تا Back مرورگر به /payment برگردد.
        // درگاه خارجی: assign کامل لازم است.
        const redirectUrl = initiated.redirectUrl;
        if (redirectUrl.startsWith("/") && !redirectUrl.startsWith("//")) {
          router.push(redirectUrl);
          return;
        }
        window.location.assign(redirectUrl);
        return;
      }
      const awaiting =
        initiated.providerCode.toLowerCase() === "manual" || method === "manual" ? "&awaitingManual=1" : "";
      window.location.assign(
        `/payment/result?paymentId=${encodeURIComponent(initiated.paymentId)}&checkoutId=${encodeURIComponent(initiated.checkoutId || page.checkoutId)}${awaiting}`,
      );
    } catch (cause: unknown) {
      setError(toCustomerPaymentMessage(cause));
      setPaying(false);
    }
  }

  return (
    <div className="min-h-screen bg-white pb-10" data-testid="payment-handoff-page">
      <div className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 md:pt-6">
        <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-[#E53935] to-[#b71c1c] min-h-[180px]">
          <div className="relative z-10 flex flex-col items-center justify-center text-center p-8">
            <span className="inline-flex items-center gap-1.5 bg-white/15 text-white text-xs font-bold px-3 py-1.5 rounded-full mb-3">
              <CreditCard className="w-3.5 h-3.5" />
              پرداخت
            </span>
            <h1 className="text-2xl font-black text-white">پرداخت سفارش</h1>
            <p className="text-sm text-white/80 mt-1">
              {paid ? "این سفارش پرداخت شده است" : "روش پرداخت فعال فروشگاه را انتخاب کنید"}
            </p>
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

        {error && !page ? (
          <div className="text-center py-10">
            <p className="text-sm text-red-600 mb-4">{error}</p>
            <Link href="/shipping" className="inline-flex px-5 py-2.5 rounded-xl bg-[#E53935] text-white text-sm font-bold">
              بازگشت به ارسال
            </Link>
          </div>
        ) : !page ? (
          <p className="text-center text-sm text-gray-500 py-10">در حال بارگذاری وضعیت سفارش…</p>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 max-w-5xl mx-auto">
            <div className="lg:col-span-5 space-y-4">
              <div
                className="bg-white rounded-2xl border border-gray-200 p-5 shadow-sm space-y-3"
                data-testid="payment-handoff-summary"
              >
                <h2 className="font-black text-gray-900">خلاصهٔ سفارش</h2>
                <p className="text-sm text-gray-600">
                  روش ارسال: <span className="font-bold text-gray-900">{page.shippingMethodLabel}</span>
                </p>
                <p className="text-sm text-gray-600">
                  گیرنده: <span className="font-bold text-gray-900">{page.recipientName}</span> — {page.cityName}
                </p>
                <p className="text-sm text-gray-600">
                  هزینه ارسال:{" "}
                  <span className="font-bold">
                    {page.shippingAmount === 0
                      ? "رایگان / صفر پیکربندی"
                      : formatOfferAmount(page.shippingAmount, page.currency)}
                  </span>
                </p>
                <p className="text-base font-black text-[#E53935]" data-testid="payment-payable-amount">
                  قابل پرداخت: {formatOfferAmount(page.payableAmount, page.currency)}
                </p>
                <p className="text-xs text-gray-500">
                  وضعیت پرداخت:{" "}
                  <span className="font-bold text-gray-800">{paid ? "پرداخت‌شده" : "در انتظار پرداخت"}</span>
                </p>
                <Link href="/shipping" className="inline-flex text-sm text-[#E53935] font-bold">
                  بازگشت به ارسال
                </Link>
              </div>
            </div>

            <div className="lg:col-span-7 space-y-4">
              {paid ? (
                <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-sm text-emerald-800">
                  این سفارش قبلاً پرداخت شده است.
                  <div className="mt-3">
                    <Link
                      href={`/payment/result?checkoutId=${encodeURIComponent(page.checkoutId || "")}`}
                      className="inline-flex px-4 py-2 rounded-xl bg-emerald-600 text-white text-sm font-bold"
                    >
                      مشاهده نتیجه
                    </Link>
                  </div>
                </div>
              ) : !hasAnyMethod ? (
                <div
                  className="rounded-2xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-900"
                  data-testid="payment-methods-unavailable"
                >
                  در حال حاضر هیچ روش پرداخت فعالی برای این فروشگاه در دسترس نیست.
                  <p className="mt-2 text-xs text-amber-800">دکمهٔ پرداخت غیرفعال است؛ گزینهٔ جعلی نمایش داده نمی‌شود.</p>
                </div>
              ) : (
                <>
                  <StorefrontPaymentMethodPicker
                    selected={method ?? "gateway"}
                    onChange={(id) => setMethod(id)}
                    quote={quote}
                    hostEnabledCodes={enabledCodes}
                    accent="#E53935"
                  />
                  <button
                    type="button"
                    disabled={paying || !method}
                    onClick={() => void pay()}
                    className="w-full px-6 py-3.5 rounded-2xl bg-[#E53935] text-white text-sm font-bold disabled:opacity-50 shadow-lg shadow-[#E53935]/25"
                    data-testid="payment-submit"
                  >
                    {paying
                      ? method === "wallet"
                        ? "در حال پرداخت از کیف پول…"
                        : "در حال ثبت پرداخت…"
                      : method === "wallet"
                        ? "پرداخت با کیف پول"
                        : method === "manual"
                          ? "ثبت پرداخت کارت به کارت"
                          : "پرداخت آنلاین"}
                  </button>
                </>
              )}
              {error ? <p className="text-sm text-red-600">{error}</p> : null}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
