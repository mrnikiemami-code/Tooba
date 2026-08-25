"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Check, ChevronDown, CreditCard, Home, MapPin, Plus, ShieldCheck, Truck } from "lucide-react";
import { formatOfferAmount } from "./storefront-api.ts";
import { readCartSession } from "./storefront-cart-api.ts";
import {
  listCheckoutSavedAddresses,
  shippingFromCustomerAddress,
  type CustomerAddress,
} from "../customer-panel/customer-address-api.ts";
import {
  previewStorefrontCheckout,
  submitStorefrontCheckout,
  toCustomerCheckoutMessage,
  type StorefrontCheckoutPage,
  type StorefrontCheckoutShipping,
} from "./storefront-checkout-api.ts";

const emptyShipping: StorefrontCheckoutShipping = {
  recipientName: "",
  contactMobile: "",
  provinceName: "",
  cityName: "",
  postalAddress: "",
  postalCode: "",
};

/**
 * تسویه با پوستهٔ Shopeiva و حقیقت Host. پرداخت موفق جعل نمی‌شود.
 */
export function StorefrontShopeivaCheckout() {
  const router = useRouter();
  const [page, setPage] = useState<StorefrontCheckoutPage | null>(null);
  const [shipping, setShipping] = useState<StorefrontCheckoutShipping>(emptyShipping);
  const [error, setError] = useState<string | null>(null);
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
    try {
      const submitted = await submitStorefrontCheckout(session.cartId, page.cartVersion, shipping, savedAddressId);
      if (!submitted.checkoutId) {
        throw new Error("شناسهٔ سفارش برنگشت.");
      }
      router.push(`/order/confirmation?checkoutId=${submitted.checkoutId}`);
    } catch (cause) {
      setError(toCustomerCheckoutMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (!page) {
    return (
      <div className="py-16 text-center">
        {error ? <p className="text-sm text-red-600">{error}</p> : <p className="text-sm text-gray-500">در حال آماده‌سازی تسویه…</p>}
        <Link href="/cart" className="inline-flex mt-6 px-5 py-2.5 rounded-xl bg-[#2563EB] text-white text-sm font-bold">
          بازگشت به سبد
        </Link>
      </div>
    );
  }

  return (
    <form onSubmit={(event) => void onSubmit(event)} className="py-6 md:py-10 grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div className="lg:col-span-8 space-y-4">
        <div className="flex items-center gap-2 text-xs font-bold text-[#2563EB]">
          <span className="px-3 py-1 rounded-full bg-blue-50">۱. ارسال</span>
          <span className="text-gray-300">/</span>
          <span className="px-3 py-1 rounded-full bg-gray-50 text-gray-500">۲. پرداخت بعدی</span>
        </div>
        {error ? <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl p-3">{error}</p> : null}
        {savedAddresses ? (
          <section className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm">
            <h2 className="text-sm md:text-base font-black flex items-center gap-2 mb-3">
              <Home className="w-4 h-4 text-[#2563EB]" />
              انتخاب آدرس
            </h2>
            <div className="flex gap-3 mb-4">
              <button
                type="button"
                onClick={startNewAddress}
                className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                  !useSavedAddress ? "border-[#2563EB] bg-[#2563EB]/5 text-[#2563EB]" : "border-gray-200 text-gray-500 hover:border-gray-300"
                }`}
              >
                <Plus className="w-4 h-4" /> آدرس جدید
              </button>
              <button
                type="button"
                onClick={() => setShowSaved((open) => !open)}
                className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-2xl text-xs md:text-sm font-bold border-2 transition-all ${
                  useSavedAddress ? "border-[#2563EB] bg-[#2563EB]/5 text-[#2563EB]" : "border-gray-200 text-gray-500 hover:border-gray-300"
                }`}
              >
                <Home className="w-4 h-4" /> آدرس‌های من
                <ChevronDown className={`w-3 h-3 transition-transform ${showSaved ? "rotate-180" : ""}`} />
              </button>
            </div>
            {showSaved ? (
              <div className="space-y-2">
                {savedAddresses.length === 0 ? (
                  <p className="text-xs text-gray-500 text-center py-3">نشانی ذخیره‌شده‌ای نیست.</p>
                ) : (
                  savedAddresses.map((saved) => (
                    <button
                      key={saved.addressId}
                      type="button"
                      onClick={() => selectSaved(saved)}
                      className={`w-full text-right p-3 md:p-4 rounded-xl border-2 transition-all ${
                        savedAddressId === saved.addressId
                          ? "border-[#2563EB] bg-[#2563EB]/5"
                          : "border-gray-100 hover:border-gray-200 bg-gray-50"
                      }`}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                          <p className="text-xs md:text-sm font-bold">{saved.recipientName}</p>
                          <p className="text-[10px] md:text-xs text-gray-500">
                            {saved.provinceName}، {saved.cityName}، {saved.postalAddress}
                          </p>
                          <p className="text-[10px] text-gray-400 mt-0.5">
                            {saved.contactMobile} | کد پستی: {saved.postalCode}
                            {saved.isDefault ? " · پیش‌فرض" : ""}
                          </p>
                        </div>
                        {savedAddressId === saved.addressId ? <Check className="w-4 h-4 text-[#2563EB] shrink-0 mt-1" /> : null}
                      </div>
                    </button>
                  ))
                )}
              </div>
            ) : null}
          </section>
        ) : null}
        <section className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm space-y-3">
          <h2 className="text-base font-black flex items-center gap-2">
            <MapPin className="w-4 h-4 text-[#2563EB]" />
            اطلاعات گیرنده
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label="نام و نام خانوادگی" value={shipping.recipientName} onChange={(value) => changeShipping("recipientName", value)} />
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
        </section>
        <section className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm">
          <h2 className="text-base font-black flex items-center gap-2 mb-3">
            <Truck className="w-4 h-4 text-[#2563EB]" />
            روش ارسال
          </h2>
          <div className="rounded-xl border-2 border-[#2563EB] bg-blue-50/50 p-3 text-sm">
            <p className="font-bold">{page.shippingMethodLabel}</p>
            <p className="text-xs text-gray-500 mt-1">نرخ چندحامل جعلی نیست. موتور ارسال کامل در فاز بعد است.</p>
          </div>
        </section>
        <section className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm">
          <h2 className="text-base font-black flex items-center gap-2 mb-3">
            <CreditCard className="w-4 h-4 text-[#2563EB]" />
            پرداخت
          </h2>
          <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
            سفارش پس از ثبت در وضعیت انتظار پرداخت می‌ماند. درگاه واقعی در این مرحله وصل نیست و پرداخت موفق نمایش داده نمی‌شود.
          </div>
        </section>
      </div>
      <aside className="lg:col-span-4">
        <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-md">
          <h2 className="text-base font-black mb-4">خلاصه سفارش</h2>
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
            <div className="flex justify-between font-black text-[#2563EB] border-t border-gray-200 pt-2">
              <span>قابل پرداخت</span>
              <span>{formatOfferAmount(page.payableAmount, page.currency)}</span>
            </div>
          </div>
          <button type="submit" disabled={busy} className="mt-4 w-full py-3 rounded-2xl font-black text-sm bg-[#2563EB] text-white disabled:opacity-60">
            {busy ? "در حال ثبت…" : "ثبت سفارش"}
          </button>
          <p className="text-[11px] text-gray-400 mt-2 flex items-center gap-1">
            <ShieldCheck className="w-3 h-3" />
            مبلغ از تسویهٔ Host است نه جمع صفحه.
          </p>
        </div>
      </aside>
    </form>
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
