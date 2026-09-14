"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  Check,
  ChevronDown,
  ChevronLeft,
  CreditCard,
  Home,
  MapPin,
  Plus,
  ShieldCheck,
  ShoppingBag,
  Truck,
} from "lucide-react";
import { formatOfferAmount } from "./storefront-api.ts";
import { readCartSession } from "./storefront-cart-api.ts";
import {
  listCheckoutSavedAddresses,
  recipientDisplayName,
  shippingFromCustomerAddress,
  type CustomerAddress,
} from "../customer-panel/customer-address-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";
import { StorefrontCheckoutLimitNotice } from "./storefront-checkout-limit-notice.tsx";
import {
  previewStorefrontCheckout,
  submitStorefrontCheckout,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
  type StorefrontCheckoutShipping,
} from "./storefront-checkout-api.ts";

const emptyShipping: StorefrontCheckoutShipping = {
  firstName: "",
  lastName: "",
  recipientName: "",
  contactMobile: "",
  provinceName: "",
  cityName: "",
  postalAddress: "",
  postalCode: "",
};

const steps = [
  { id: 1, label: "سبد خرید", icon: ShoppingBag, href: "/cart" },
  { id: 2, label: "ارسال", icon: Truck, href: null },
  { id: 3, label: "پرداخت", icon: CreditCard, href: null },
] as const;

/**
 * تسویه با پوستهٔ Shipping/Payment Shopeiva و حقیقت Host.
 * AddressBook → snapshot غیرقابل‌تغییر؛ پرداخت موفق جعل نمی‌شود.
 */
export function StorefrontShopeivaCheckout() {
  const router = useRouter();
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [shipping, setShipping] = useState<StorefrontCheckoutShipping>(emptyShipping);
  const [error, setError] = useState<string | null>(null);
  const [limitCode, setLimitCode] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [savedAddresses, setSavedAddresses] = useState<CustomerAddress[] | null>(null);
  const [useSavedAddress, setUseSavedAddress] = useState(false);
  const [showSaved, setShowSaved] = useState(false);
  const [savedAddressId, setSavedAddressId] = useState<string | null>(null);

  useEffect(() => {
    const session = readCartSession();
    if (!session.cartId) {
      setError("سبد خرید پیدا نشد.");
      return;
    }
    void previewStorefrontCheckout(session.cartId)
      .then((result) => {
        setPage(result);
        setError(null);
      })
      .catch((cause: unknown) => setError(toCustomerCheckoutMessage(cause)));
    void listCheckoutSavedAddresses()
      .then((result) => setSavedAddresses(result.addresses))
      .catch(() => setSavedAddresses(null));
  }, []);

  function changeShipping<K extends keyof StorefrontCheckoutShipping>(key: K, value: StorefrontCheckoutShipping[K]) {
    setSavedAddressId(null);
    setUseSavedAddress(false);
    setShipping((current) => ({ ...current, [key]: value }));
  }

  function selectSaved(address: CustomerAddress) {
    setShipping(shippingFromCustomerAddress(address));
    setSavedAddressId(address.addressId);
    setUseSavedAddress(true);
    setShowSaved(false);
  }

  function startNewAddress() {
    setUseSavedAddress(false);
    setSavedAddressId(null);
    setShowSaved(false);
    setShipping(emptyShipping);
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const session = readCartSession();
    if (!session.cartId || !page) {
      return;
    }
    setBusy(true);
    setError(null);
    setLimitCode(null);
    try {
      const submitted = await submitStorefrontCheckout(session.cartId, page.cartVersion, shipping, savedAddressId);
      if (!submitted.checkoutId) {
        throw new Error("شناسهٔ سفارش برنگشت.");
      }
      router.push(`/order/confirmation?checkoutId=${submitted.checkoutId}`);
    } catch (cause) {
      setError(toCustomerCheckoutMessage(cause));
      setLimitCode(cause instanceof StorefrontCartApiError ? cause.errorCode : null);
    } finally {
      setBusy(false);
    }
  }

  if (!page) {
    return (
      <div className="py-10" data-testid="checkout-page">
        <CheckoutHero
          activeStep={2}
          itemCount={0}
          subtotalLabel="—"
          badge="اطلاعات ارسال و تحویل"
          title="اطلاعات ارسال"
          subtitle="آدرس و روش ارسال خود را انتخاب کنید"
        />
        <div className="py-16 text-center">
          {error ? <p className="text-sm text-red-600">{error}</p> : <p className="text-sm text-gray-500">در حال آماده‌سازی تسویه…</p>}
          <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-primary text-white text-sm font-bold">
            بازگشت به سبد
          </Link>
        </div>
      </div>
    );
  }

  const itemCount = page.sellerOrders.reduce((sum, order) => sum + order.lines.reduce((s, line) => s + line.quantity, 0), 0);

  return (
    <form onSubmit={(event) => void onSubmit(event)} className="pb-10 bg-section-surface" data-testid="checkout-page" data-storefront-surface-role="section">
      <nav className="text-xs text-gray-500 pt-4 mb-2 flex flex-wrap gap-2" aria-label="مسیر صفحه" data-testid="checkout-breadcrumb">
        <Link href="/cart" className="hover:text-primary">
          سبد خرید
        </Link>
        <span>/</span>
        <span className="text-gray-800 font-bold">اطلاعات ارسال</span>
      </nav>

      <CheckoutHero
        activeStep={2}
        itemCount={itemCount}
        subtotalLabel={formatOfferAmount(page.payableAmount, page.currency)}
        badge="اطلاعات ارسال و تحویل"
        title="اطلاعات ارسال"
        subtitle="آدرس و روش ارسال خود را انتخاب کنید"
      />

      <section className="pt-6 md:pt-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 md:gap-6">
          <div className="lg:col-span-2 space-y-4">
            {error ? (
              <div className="text-sm bg-red-50 border border-red-100 rounded-xl p-3" role="alert">
                <StorefrontCheckoutLimitNotice errorCode={limitCode} message={error} />
              </div>
            ) : null}

            {savedAddresses ? (
              <section className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="checkout-address-picker" data-storefront-surface-role="card">
                <h2 className="text-sm md:text-base font-black flex items-center gap-2 mb-3">
                  <Home className="w-4 h-4 text-primary" />
                  انتخاب آدرس
                </h2>
                <div className="flex gap-3 mb-4">
                  <button
                    type="button"
                    onClick={startNewAddress}
                    className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                      !useSavedAddress
                        ? "border-primary bg-primary/5 text-primary"
                        : "border-gray-200 text-gray-500 hover:border-gray-300"
                    }`}
                  >
                    <Plus className="w-4 h-4" /> آدرس جدید
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowSaved((open) => !open)}
                    className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                      useSavedAddress
                        ? "border-primary bg-primary/5 text-primary"
                        : "border-gray-200 text-gray-500 hover:border-gray-300"
                    }`}
                  >
                    <Home className="w-4 h-4" /> آدرس‌های من
                    <ChevronDown className={`w-3 h-3 transition-transform ${showSaved ? "rotate-180" : ""}`} />
                  </button>
                </div>
                {showSaved ? (
                  <div className="space-y-2" data-testid="checkout-saved-addresses">
                    {savedAddresses.length === 0 ? (
                      <p className="text-xs text-gray-500 text-center py-3">نشانی ذخیره‌شده‌ای نیست. مهمان می‌تواند آدرس جدید وارد کند.</p>
                    ) : (
                      savedAddresses.map((saved) => (
                        <button
                          key={saved.addressId}
                          type="button"
                          onClick={() => selectSaved(saved)}
                          className={`w-full text-right p-3 md:p-4 rounded-xl border-2 transition-all ${
                            savedAddressId === saved.addressId
                              ? "border-primary bg-primary/5"
                              : "border-gray-100 hover:border-gray-200 bg-gray-50"
                          }`}
                        >
                          <div className="flex items-start justify-between gap-2">
                            <div className="min-w-0">
                              <p className="text-xs md:text-sm font-bold">{recipientDisplayName(saved)}</p>
                              <p className="text-[10px] md:text-xs text-gray-500">
                                {saved.provinceName}، {saved.cityName}، {saved.postalAddress}
                              </p>
                              <p className="text-[10px] text-gray-400 mt-0.5">
                                {saved.contactMobile} | کد پستی: {saved.postalCode}
                                {saved.isDefault ? " · پیش‌فرض" : ""}
                              </p>
                            </div>
                            {savedAddressId === saved.addressId ? (
                              <Check className="w-4 h-4 text-primary shrink-0 mt-1" />
                            ) : null}
                          </div>
                        </button>
                      ))
                    )}
                  </div>
                ) : null}
              </section>
            ) : null}

              <section className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm space-y-3" data-testid="checkout-recipient" data-storefront-surface-role="input">
              <h2 className="text-base font-black flex items-center gap-2">
                <MapPin className="w-4 h-4 text-primary" />
                اطلاعات گیرنده
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <Field label="نام" value={shipping.firstName} onChange={(value) => changeShipping("firstName", value)} />
                <Field label="نام خانوادگی" value={shipping.lastName} onChange={(value) => changeShipping("lastName", value)} />
                <Field label="موبایل" value={shipping.contactMobile} ltr onChange={(value) => changeShipping("contactMobile", value)} />
                <Field label="استان" value={shipping.provinceName} onChange={(value) => changeShipping("provinceName", value)} />
                <Field label="شهر" value={shipping.cityName} onChange={(value) => changeShipping("cityName", value)} />
                <label className="md:col-span-2 text-xs font-bold text-gray-600">
                  نشانی
                  <textarea
                    required
                    value={shipping.postalAddress}
                    onChange={(event) => changeShipping("postalAddress", event.target.value)}
                    className="mt-1 w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm min-h-24"
                  />
                </label>
                <Field label="کد پستی" value={shipping.postalCode} ltr onChange={(value) => changeShipping("postalCode", value)} />
              </div>
              <p className="text-[11px] text-gray-400 leading-5">
                مسیر مهمان/آدرس جدید بدون اجبار ورود باز است. اگر آدرس ذخیره‌شده انتخاب شود، Checkout مالکیت AddressBook را
                اعتبارسنجی می‌کند و snapshot غیرقابل‌تغییر می‌سازد.
              </p>
            </section>

            <section className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="checkout-shipping-method" data-storefront-surface-role="input">
              <h2 className="text-base font-black flex items-center gap-2 mb-3">
                <Truck className="w-4 h-4 text-primary" />
                روش ارسال
              </h2>
              <div className="rounded-xl border-2 border-primary bg-primary/5 p-3 text-sm">
                <p className="font-bold">{page.shippingMethodLabel}</p>
                <p className="text-xs text-gray-500 mt-1">برچسب از Host است. نرخ چندحامل جعلی اضافه نشده است.</p>
              </div>
            </section>

            <section className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="checkout-payment" data-storefront-surface-role="input">
              <h2 className="text-base font-black flex items-center gap-2 mb-3">
                <CreditCard className="w-4 h-4 text-primary" />
                پرداخت
              </h2>
              <div className="rounded-xl border-2 border-primary bg-surface p-4 space-y-3">
                <div className="flex items-start gap-3">
                  <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
                    <ShieldCheck className="w-5 h-5 text-primary" />
                  </div>
                  <div>
                    <p className="text-sm font-bold text-gray-900">پرداخت پس از ثبت سفارش</p>
                    <p className="text-xs text-gray-500 mt-1 leading-6">
                      پس از ثبت، در صفحهٔ تأیید می‌توانید درگاه بانکی یا — در صورت پوشش کامل موجودی —
                      کیف پول را انتخاب کنید. کارت بانکی جعلی یا شارژ کیف پول در این صفحه نیست.
                    </p>
                  </div>
                </div>
                <div
                  className="rounded-xl border border-dashed border-gray-200 bg-gray-50 px-3 py-2"
                  data-testid="checkout-payment-mixed-deferred"
                >
                  <p className="text-[10px] md:text-xs text-gray-500">
                    پرداخت ترکیبی کیف پول + درگاه فعلاً DEFERRED است و اینجا ادعا نمی‌شود.
                  </p>
                </div>
              </div>
            </section>
          </div>

          <aside className="lg:col-span-1">
            <div className="lg:sticky lg:top-24" data-testid="checkout-summary">
              <div className="bg-surface-elevated rounded-2xl border border-gray-200 p-4 md:p-5 shadow-md" data-storefront-surface-role="elevated">
                <h2 className="text-base font-black mb-4 flex items-center gap-2">
                  <CreditCard className="w-4 h-4 text-primary" />
                  خلاصه سفارش
                </h2>
                <div className="space-y-2 text-sm">
                  {page.sellerOrders.flatMap((order) =>
                    order.lines.map((line) => (
                      <div key={`${order.sellerOrderId}-${line.offerId}`} className="flex justify-between gap-2">
                        <span className="text-gray-600 line-clamp-1">
                          {line.title} × {line.quantity.toLocaleString("fa-IR")}
                        </span>
                        <span className="font-bold">{formatOfferAmount(line.linePayable, line.currency)}</span>
                      </div>
                    )),
                  )}
                  <div className="flex justify-between border-t border-gray-200 pt-2">
                    <span className="text-gray-500">جمع کالا</span>
                    <span>{formatOfferAmount(page.subtotalExclusiveOfTax, page.currency)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">تخفیف</span>
                    <span>{formatOfferAmount(page.discountAmount, page.currency)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">مالیات</span>
                    <span>{formatOfferAmount(page.taxAmount, page.currency)}</span>
                  </div>
                  <div className="flex justify-between font-black text-primary border-t border-gray-200 pt-2">
                    <span>قابل پرداخت</span>
                    <span>{formatOfferAmount(page.payableAmount, page.currency)}</span>
                  </div>
                </div>
                <button
                  type="submit"
                  disabled={busy}
                  className="mt-4 w-full py-3 rounded-2xl font-black text-sm bg-primary text-white disabled:opacity-60 flex items-center justify-center gap-2 shadow-lg shadow-primary/25"
                  data-testid="checkout-submit"
                >
                  {busy ? "در حال ثبت…" : "ثبت سفارش و ادامه پرداخت"}
                  {!busy ? <ChevronLeft className="w-4 h-4" /> : null}
                </button>
                <p className="text-[11px] text-gray-400 mt-2 flex items-center gap-1">
                  <ShieldCheck className="w-3 h-3" />
                  مبلغ از تسویهٔ Host است نه جمع صفحه.
                </p>
              </div>
            </div>
          </aside>
        </div>
      </section>
    </form>
  );
}

function CheckoutHero({
  activeStep,
  itemCount,
  subtotalLabel,
  badge,
  title,
  subtitle,
}: {
  activeStep: number;
  itemCount: number;
  subtotalLabel: string;
  badge: string;
  title: string;
  subtitle: string;
}) {
  return (
    <section className="w-full bg-section-surface" data-testid="checkout-hero" data-storefront-surface-role="section">
      <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-primary to-primary-strong min-h-[200px] md:min-h-[220px]">
        <div className="absolute inset-0 opacity-[0.08]">
          <div className="absolute -top-20 -right-20 w-56 h-56 rounded-full bg-surface" />
          <div className="absolute -bottom-16 -left-16 w-40 h-40 rounded-full bg-surface" />
          <div className="absolute top-10 left-1/4 w-20 h-20 rounded-full bg-surface" />
        </div>
        <div className="relative z-10 flex flex-col items-center justify-center text-center p-6 pt-8 md:p-10 md:pt-12">
          <span className="inline-flex items-center gap-1.5 bg-surface/15 backdrop-blur-sm text-white text-[10px] md:text-xs font-bold px-3 py-1.5 rounded-full mb-4">
            <Truck className="w-3.5 h-3.5" />
            {badge}
          </span>
          <h1 className="text-lg md:text-3xl font-black text-white leading-snug mb-1">{title}</h1>
          <p className="text-[10px] md:text-sm text-white/80">{subtitle}</p>
        </div>
      </div>

      <div className="flex items-center justify-center gap-2 md:gap-3 -mt-6 md:-mt-8 relative z-20 mb-6 md:mb-8 flex-wrap" data-testid="checkout-stepper">
        {steps.map((step, index) => {
          const active = step.id === activeStep;
          const done = step.id < activeStep;
          const content = (
            <span
              className={`flex items-center gap-1.5 md:gap-2 px-3 md:px-5 py-2 md:py-3 rounded-xl text-[11px] md:text-sm font-bold transition-all ${
                active
                  ? "bg-primary text-white shadow-lg shadow-primary/30"
                  : done
                    ? "bg-blue-50 text-primary"
                    : "bg-gray-100 text-gray-400"
              }`}
            >
              <step.icon className="w-3.5 h-3.5 md:w-4 md:h-4" />
              {done ? <Check className="w-3.5 h-3.5" /> : null}
              {step.label}
            </span>
          );
          return (
            <div key={step.id} className="flex items-center gap-2 md:gap-3">
              {step.href ? (
                <Link href={step.href} className="hover:opacity-90">
                  {content}
                </Link>
              ) : (
                content
              )}
              {index < steps.length - 1 ? (
                <ChevronLeft className={`w-3.5 h-3.5 md:w-4 md:h-4 ${done ? "text-primary" : "text-gray-300"}`} />
              ) : null}
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-3 gap-1.5 md:gap-4 mb-2">
        <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-surface border border-gray-200 shadow-lg" data-storefront-surface-role="card">
          <div className="w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
            <ShoppingBag className="w-3 h-3 md:w-5 md:h-5 text-primary" />
          </div>
          <div className="min-w-0">
            <p className="text-[10px] md:text-2xl font-black text-gray-900">{itemCount.toLocaleString("fa-IR")}</p>
            <p className="text-[7px] md:text-xs text-gray-500 truncate">تعداد کالا</p>
          </div>
        </div>
        <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-surface border border-gray-200 shadow-lg" data-storefront-surface-role="card">
          <div className="w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl bg-emerald-50 flex items-center justify-center shrink-0">
            <CreditCard className="w-3 h-3 md:w-5 md:h-5 text-emerald-500" />
          </div>
          <div className="min-w-0">
            <p className="text-[9px] md:text-lg font-black text-gray-900 truncate">{subtotalLabel}</p>
            <p className="text-[7px] md:text-xs text-gray-500 truncate">قابل پرداخت</p>
          </div>
        </div>
        <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-surface border border-gray-200 shadow-lg" data-storefront-surface-role="card">
          <div className="w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl bg-amber-50 flex items-center justify-center shrink-0">
            <ShieldCheck className="w-3 h-3 md:w-5 md:h-5 text-amber-500" />
          </div>
          <div className="min-w-0">
            <p className="text-[10px] md:text-2xl font-black text-amber-500">امن</p>
            <p className="text-[7px] md:text-xs text-gray-500 truncate">پرداخت Host</p>
          </div>
        </div>
      </div>
    </section>
  );
}

function Field({
  label,
  value,
  onChange,
  ltr,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  ltr?: boolean;
}) {
  return (
    <label className="text-xs font-bold text-gray-600">
      {label}
      <input
        required
        value={value}
        dir={ltr ? "ltr" : undefined}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm"
      />
    </label>
  );
}
