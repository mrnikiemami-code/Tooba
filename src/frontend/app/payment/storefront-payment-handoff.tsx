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
import { StorefrontCartApiError } from "../storefront/storefront-cart-api.ts";
import { readStoredCheckoutId } from "../storefront/storefront-shipping-api.ts";
import { StorefrontPaymentMethodPicker } from "../storefront/storefront-payment-methods.tsx";
import {
  loadStorefrontPaymentMethods,
  loadStorefrontWalletQuote,
  loadStorefrontCartWalletQuote,
  requiresProviderRedirect,
  startStorefrontPayment,
  toCustomerPaymentMessage,
  WALLET_PROVIDER_CODE,
  type StorefrontPaymentInitiation,
  type StorefrontPaymentMethodId,
  type StorefrontWalletQuote,
} from "../storefront/storefront-payment-api.ts";
import {
  loadCartPaymentDraft,
  startFinalClickPayment,
  toCustomerFinalClickMessage,
} from "../storefront/storefront-final-click-api.ts";
import { useCheckoutAuthGate } from "../storefront/use-checkout-auth-gate.ts";

/**
 * پرداخت ویترین — دو حالت صریح و ناسازگار (هرگز هم‌زمان):
 *
 * ۱) «سبد-محور» (`/payment?cartId=...`): خواندن پیش‌نویس خواندنی ORDER 011 و شروع پرداخت فقط با کلیک
 *    نهایی روی مسیر ORDER 028 (`/cart/{cartId}/start-payment`). تا آن کلیک نه commit ارسال، نه رزرو
 *    موجودی و نه ساخت سفارش رخ می‌دهد و مبلغ از پیش‌نویس بک‌اند می‌آید (هیچ نقل‌قول جعلی ساخته نمی‌شود).
 * ۲) «سفارش-محور» (`/payment?checkoutId=...`): بازیابی سفارش پایداری که قبلاً ثبت شده؛ همان جریان
 *    موجود حفظ می‌شود و هیچ commit/رزرو تازه‌ای تحمیل نمی‌کند.
 *
 * فرم ورود شماره کارت / کد امنیتی / تاریخ انقضای قالب Shopeiva اینجا نیست و عمداً اضافه نمی‌شود.
 */
export function StorefrontPaymentHandoff() {
  useCheckoutAuthGate("/payment");
  const params = useSearchParams();
  const router = useRouter();
  const [mode, setMode] = useState<"cart" | "checkout">("checkout");
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [quote, setQuote] = useState<StorefrontWalletQuote | null>(null);
  const [enabledCodes, setEnabledCodes] = useState<string[]>([]);
  const [method, setMethod] = useState<StorefrontPaymentMethodId | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [recoveryCheckoutId, setRecoveryCheckoutId] = useState<string | null>(null);
  const [paying, setPaying] = useState(false);

  useEffect(() => {
    const cartId = params.get("cartId");
    if (cartId) {
      setMode("cart");
      let cancelled = false;
      void (async () => {
        try {
          const [draft, methodsPage, cartQuote] = await Promise.all([
            loadCartPaymentDraft(cartId),
            loadStorefrontPaymentMethods(),
            loadStorefrontCartWalletQuote(cartId),
          ]);
          if (cancelled) return;
          setPage(draft);
          // نقل‌قول کیف‌پول سبد فقط از سرور می‌آید: هم مالکیت خریدار احراز‌شده و هم پوشش کامل مبلغ باید
          // اثبات شده باشد. شکست fetch یا سبد اثبات‌نشده → null (fail-closed) و هیچ مقدار جعلی ساخته نمی‌شود.
          setQuote(cartQuote);
          const codes = methodsPage.methods.map((m) => m.code.toLowerCase());
          const walletOk = Boolean(cartQuote?.canPayFullyWithWallet);
          setEnabledCodes(walletOk && !codes.includes("wallet") ? [...codes, "wallet"] : codes);
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
          if (!cancelled) setError(toCustomerFinalClickMessage(cause));
        }
      })();
      return () => {
        cancelled = true;
      };
    }

    setMode("checkout");
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

  /** پس از شروع موفق پرداخت: درگاه → redirect؛ کیف پول/دستی → صفحهٔ نتیجه. مبلغ از Host است. */
  function routeAfterInitiation(initiated: StorefrontPaymentInitiation): void {
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
    const awaiting = initiated.providerCode.toLowerCase() === "manual" || method === "manual" ? "&awaitingManual=1" : "";
    window.location.assign(
      `/payment/result?paymentId=${encodeURIComponent(initiated.paymentId)}&checkoutId=${encodeURIComponent(initiated.checkoutId || page?.checkoutId || "")}${awaiting}`,
    );
  }

  /** اعتبارسنجی روش انتخاب‌شده با همان کاتالوگ فعال فروشگاه؛ هیچ روشی حدس زده نمی‌شود. */
  function validateMethod(): string | null {
    if (!method) return "روش پرداخت را انتخاب کنید.";
    if (mode === "cart") {
      if (method === "wallet" && !quote?.canPayFullyWithWallet) {
        return "پرداخت با کیف پول برای این سبد در دسترس نیست؛ یکی از روش‌های فعال فروشگاه را انتخاب کنید.";
      }
      if (method === "gateway" && !enabledCodes.includes("gateway")) return "روش پرداخت درگاه برای این فروشگاه فعال نیست.";
      if (method === "manual" && !enabledCodes.includes("manual")) return "پرداخت کارت به کارت برای این فروشگاه فعال نیست.";
      return null;
    }
    if (method === "wallet" && !quote?.canPayFullyWithWallet) {
      return "پرداخت کامل با کیف پول برای این سفارش ممکن نیست.";
    }
    if (method === "gateway" && !enabledCodes.includes("gateway")) {
      return "روش پرداخت درگاه برای این فروشگاه فعال نیست.";
    }
    if (method === "manual" && !enabledCodes.includes("manual") && !quote?.manualCardToCardEnabled) {
      return "پرداخت کارت به کارت برای این فروشگاه فعال نیست.";
    }
    return null;
  }

  async function pay() {
    if (!page || paid || paying) return;
    const invalid = validateMethod();
    if (invalid) {
      setError(invalid);
      return;
    }
    setPaying(true);
    setError(null);
    setRecoveryCheckoutId(null);
    try {
      const initiated =
        mode === "cart"
          ? await startFinalClickPayment({
              cartId: page.cartId,
              expectedCartVersion: page.cartVersion,
              method: method as StorefrontPaymentMethodId,
            })
          : await startStorefrontPayment(page.checkoutId as string, {
              providerCode: method === "wallet" ? WALLET_PROVIDER_CODE : method === "manual" ? "manual" : "gateway",
            });
      routeAfterInitiation(initiated);
    } catch (cause: unknown) {
      setError(mode === "cart" ? toCustomerFinalClickMessage(cause) : toCustomerPaymentMessage(cause));
      // شکست پس از commit: ارجاع سفارش پایدار برای بازیابی همان سفارش (بدون commit دوباره، بدون لغو خودکار).
      setRecoveryCheckoutId(cause instanceof StorefrontCartApiError ? cause.meta.checkoutId ?? null : null);
      setPaying(false);
    }
  }

  return (
    <div className="min-h-screen bg-page pb-10" data-testid="payment-handoff-page" data-storefront-surface-role="page">
      <div className="max-w-[1800px] mx-auto px-4 sm:px-6 pt-4 md:pt-6">
        <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-primary to-primary-strong min-h-[180px]">
          <div className="relative z-10 flex flex-col items-center justify-center text-center p-8">
            <span className="inline-flex items-center gap-1.5 bg-surface/15 text-white text-xs font-bold px-3 py-1.5 rounded-full mb-3">
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
                  s.id === 3 ? "bg-primary text-white" : "bg-red-50 text-red-600"
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
            {mode === "cart" ? (
              <Link href="/cart" className="inline-flex px-5 py-2.5 rounded-xl bg-primary text-white text-sm font-bold">
                بازگشت به سبد خرید
              </Link>
            ) : (
              <Link href="/shipping" className="inline-flex px-5 py-2.5 rounded-xl bg-primary text-white text-sm font-bold">
                بازگشت به ارسال
              </Link>
            )}
          </div>
        ) : !page ? (
          <p className="text-center text-sm text-gray-500 py-10">در حال بارگذاری وضعیت سفارش…</p>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 max-w-5xl mx-auto">
            <div className="lg:col-span-5 space-y-4">
              <div
                className="bg-surface rounded-2xl border border-gray-200 p-5 shadow-sm space-y-3" data-storefront-surface-role="card"
                data-testid={mode === "cart" ? "payment-cart-draft-summary" : "payment-handoff-summary"}
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
                <p className="text-base font-black text-primary" data-testid="payment-payable-amount">
                  قابل پرداخت: {formatOfferAmount(page.payableAmount, page.currency)}
                </p>
                <p className="text-xs text-gray-500">
                  وضعیت پرداخت:{" "}
                  <span className="font-bold text-gray-800">{paid ? "پرداخت‌شده" : "در انتظار پرداخت"}</span>
                </p>
                {mode === "cart" ? (
                  <p className="text-[11px] text-gray-500" data-testid="payment-cart-draft-notice">
                    این سفارش هنوز ثبت نشده است. سفارش و رزرو موجودی فقط با کلیک روی دکمهٔ پرداخت نهایی ساخته می‌شود.
                  </p>
                ) : null}
                <Link href="/shipping" className="inline-flex text-sm text-primary font-bold">
                  {mode === "cart" ? "ویرایش اطلاعات ارسال" : "بازگشت به ارسال"}
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
                    accent="rgb(var(--color-primary))"
                  />
                  <button
                    type="button"
                    disabled={paying || !method}
                    onClick={() => void pay()}
                    className="w-full px-6 py-3.5 rounded-2xl bg-primary text-white text-sm font-bold disabled:opacity-50 shadow-lg shadow-primary/25"
                    data-testid="payment-submit"
                  >
                    {paying
                      ? method === "wallet"
                        ? "در حال پرداخت از کیف پول…"
                        : "در حال ثبت پرداخت…"
                      : mode === "cart"
                        ? "پرداخت نهایی و ثبت سفارش"
                        : method === "wallet"
                          ? "پرداخت با کیف پول"
                          : method === "manual"
                            ? "ثبت پرداخت کارت به کارت"
                            : "پرداخت آنلاین"}
                  </button>
                </>
              )}
              {error ? <p className="text-sm text-red-600" data-testid="payment-error">{error}</p> : null}
              {error && mode === "cart" ? (
                <div className="flex flex-wrap gap-2">
                  <Link href="/cart" className="inline-flex px-4 py-2 rounded-xl border border-gray-200 text-sm font-bold text-gray-700">
                    بازگشت به سبد خرید
                  </Link>
                </div>
              ) : null}
              {recoveryCheckoutId ? (
                <div
                  className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-xs text-amber-900"
                  data-testid="payment-recovery-same-order"
                >
                  این پرداخت شروع نشد، اما سفارش شما ثبت شده است. می‌توانید همان سفارش را بدون ساخت سفارش تازه ادامه دهید.
                  <div className="mt-2">
                    <Link
                      href={`/payment?checkoutId=${encodeURIComponent(recoveryCheckoutId)}`}
                      className="inline-flex px-4 py-2 rounded-xl bg-amber-600 text-white text-xs font-bold"
                    >
                      ادامهٔ پرداخت همین سفارش
                    </Link>
                  </div>
                </div>
              ) : null}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
