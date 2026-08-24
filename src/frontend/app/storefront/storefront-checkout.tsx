"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { CreditCard, MapPin, ShieldCheck, Truck } from "lucide-react";
import { formatOfferAmount } from "./storefront-api.ts";
import { readCartSession } from "./storefront-cart-api.ts";
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
  }, []);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const session = readCartSession();
    if (!session.cartId || !page) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const submitted = await submitStorefrontCheckout(session.cartId, page.cartVersion, shipping);
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
        <section className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm space-y-3">
          <h2 className="text-base font-black flex items-center gap-2">
            <MapPin className="w-4 h-4 text-[#2563EB]" />
            اطلاعات گیرنده
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label="نام و نام خانوادگی" value={shipping.recipientName} onChange={(value) => setShipping({ ...shipping, recipientName: value })} />
            <Field label="موبایل" value={shipping.contactMobile} ltr onChange={(value) => setShipping({ ...shipping, contactMobile: value })} />
            <Field label="استان" value={shipping.provinceName} onChange={(value) => setShipping({ ...shipping, provinceName: value })} />
            <Field label="شهر" value={shipping.cityName} onChange={(value) => setShipping({ ...shipping, cityName: value })} />
            <label className="md:col-span-2 text-xs font-bold text-gray-600">
              نشانی
              <textarea
                required
                value={shipping.postalAddress}
                onChange={(event) => setShipping({ ...shipping, postalAddress: event.target.value })}
                className="mt-1 w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm min-h-24"
              />
            </label>
            <Field label="کد پستی" value={shipping.postalCode} ltr onChange={(value) => setShipping({ ...shipping, postalCode: value })} />
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
