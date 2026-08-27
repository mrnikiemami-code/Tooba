"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useEffect, useState, type ReactNode } from "react";
import {
  ArrowLeft,
  ChevronLeft,
  CreditCard,
  HeadphonesIcon,
  Minus,
  Percent,
  Plus,
  RotateCcw,
  Shield,
  ShieldCheck,
  ShoppingBag,
  Tag,
  Trash2,
  Truck,
} from "lucide-react";
import { formatOfferAmount, storefrontMediaUrl } from "./storefront-api.ts";
import {
  changeCartLineQuantity,
  loadStorefrontCart,
  removeCartLine,
  toCustomerCartMessage,
  type StorefrontCartPage,
} from "./storefront-cart-api.ts";

/**
 * سبد خرید با پوستهٔ Shopeiva روی حقیقت Cart Host.
 * کوپن/حمل جعلی و OfferId مشتری‌نما نمایش داده نمی‌شود.
 */
export function StorefrontShopeivaCart() {
  const [cart, setCart] = useState<StorefrontCartPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void loadStorefrontCart()
      .then((page) => {
        setCart(page);
        setError(null);
      })
      .catch((cause: unknown) => {
        setError(toCustomerCartMessage(cause));
        setCart(null);
      })
      .finally(() => setLoading(false));
  }, []);

  async function mutate(action: () => Promise<StorefrontCartPage>) {
    setBusy(true);
    setError(null);
    try {
      setCart(await action());
    } catch (cause) {
      setError(toCustomerCartMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return (
      <div className="py-16 text-center text-sm text-gray-500" data-testid="cart-loading">
        در حال بارگذاری سبد…
      </div>
    );
  }

  const itemCount = cart?.itemCount ?? 0;
  const subtotal = cart?.subtotalExclusiveOfTax ?? 0;
  const currency = cart?.currency ?? "IRR";
  const discountPercent = 0;

  return (
    <div className="pb-10" data-testid="cart-page">
      <nav className="text-xs text-gray-500 pt-4 mb-2 flex flex-wrap gap-2" aria-label="مسیر صفحه" data-testid="cart-breadcrumb">
        <Link href="/" className="hover:text-[#2563EB]">
          خانه
        </Link>
        <span>/</span>
        <span className="text-gray-800 font-bold">سبد خرید</span>
      </nav>

      <CartHero itemCount={itemCount} subtotalLabel={formatOfferAmount(subtotal, currency)} discountPercent={discountPercent} />

      {!cart || cart.lines.length === 0 ? (
        <CartEmpty error={error} />
      ) : (
        <section className="pt-8 md:pt-10">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 md:gap-6">
            <div className="lg:col-span-2 space-y-3" data-testid="cart-items">
              {error ? (
                <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-xl p-3" role="alert">
                  {error}
                </p>
              ) : null}
              {cart.lines.map((line) => (
                <div
                  key={line.lineId}
                  className="bg-white rounded-2xl border border-gray-200 p-3 md:p-4 shadow-sm hover:shadow-md transition-all"
                  data-testid="cart-line"
                >
                  <div className="flex gap-3 md:gap-4">
                    <div className="w-20 h-20 md:w-24 md:h-24 rounded-xl bg-gray-50 overflow-hidden shrink-0 border border-gray-100">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img src={storefrontMediaUrl(line.mediaAssetId)} alt="" className="w-full h-full object-contain p-2" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                          <Link
                            href={line.productSlug ? `/products/${line.productSlug}` : "/products"}
                            className="text-sm md:text-base font-bold text-gray-900 hover:text-[#2563EB] line-clamp-1"
                          >
                            {line.title}
                          </Link>
                          <p className="text-[11px] text-gray-500 mt-1">فروشنده: {line.sellerDisplayName}</p>
                        </div>
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => void mutate(() => removeCartLine(line.lineId))}
                          className="p-1.5 rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-500 shrink-0"
                          title="حذف کالا"
                          aria-label="حذف کالا"
                          data-testid="cart-line-remove"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                      <div className="flex flex-wrap items-end justify-between gap-2 mt-2">
                        <div className="flex items-center border border-gray-200 rounded-xl overflow-hidden" data-testid="cart-line-qty">
                          <button
                            type="button"
                            disabled={busy || line.quantity <= 1}
                            onClick={() => void mutate(() => changeCartLineQuantity(line.lineId, line.quantity - 1))}
                            className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100 disabled:opacity-30"
                            aria-label="کاهش"
                          >
                            <Minus className="w-3.5 h-3.5" />
                          </button>
                          <span className="w-10 h-8 flex items-center justify-center text-sm font-bold border-x border-gray-200">
                            {line.quantity.toLocaleString("fa-IR")}
                          </span>
                          <button
                            type="button"
                            disabled={busy}
                            onClick={() => void mutate(() => changeCartLineQuantity(line.lineId, line.quantity + 1))}
                            className="w-8 h-8 flex items-center justify-center text-gray-500 hover:bg-gray-100"
                            aria-label="افزایش"
                          >
                            <Plus className="w-3.5 h-3.5" />
                          </button>
                        </div>
                        <div className="text-left">
                          {line.unitAmountExclusiveOfTax != null ? (
                            <p className="text-xs text-gray-500">واحد: {formatOfferAmount(line.unitAmountExclusiveOfTax, line.currency)}</p>
                          ) : null}
                          <p className="text-sm font-black text-[#2563EB]">
                            {line.lineAmountExclusiveOfTax != null
                              ? formatOfferAmount(line.lineAmountExclusiveOfTax, line.currency)
                              : "—"}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2 mt-2 pt-2 border-t border-gray-100">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => void mutate(() => removeCartLine(line.lineId))}
                          className="flex items-center gap-1 text-[10px] md:text-xs text-gray-400 hover:text-red-500"
                        >
                          <Trash2 className="w-3 h-3" /> حذف
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="cart-shipping-honest">
                <h4 className="text-xs md:text-sm font-bold text-gray-900 flex items-center gap-2 mb-2">
                  <Truck className="w-4 h-4 text-[#2563EB]" />
                  روش ارسال
                </h4>
                <p className="text-xs text-gray-500 leading-6">
                  انتخاب حامل و هزینهٔ ارسال در مرحلهٔ تسویه از حقیقت Host انجام می‌شود. در سبد، نرخ چندحامل جعلی نمایش داده
                  نمی‌شود.
                </p>
              </div>
            </div>

            <div className="lg:col-span-1">
              <div className="lg:sticky lg:top-24 space-y-4" data-testid="cart-summary">
                <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-md">
                  <h2 className="text-base md:text-lg font-black text-gray-900 flex items-center gap-2 mb-4">
                    <CreditCard className="w-4 h-4 text-[#2563EB]" />
                    خلاصه سفارش
                  </h2>
                  <div className="space-y-2.5 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-500">تعداد کالاها</span>
                      <span className="font-bold">{cart.itemCount.toLocaleString("fa-IR")}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">مبلغ کالا (بدون مالیات)</span>
                      <span className="font-bold">{formatOfferAmount(cart.subtotalExclusiveOfTax, cart.currency)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">هزینه ارسال</span>
                      <span className="font-bold text-gray-500">در تسویه</span>
                    </div>
                    <div className="border-t border-gray-200 pt-2.5 flex justify-between">
                      <span className="font-black">قابل پرداخت (برآورد)</span>
                      <span className="font-black text-[#2563EB]">
                        {formatOfferAmount(cart.subtotalExclusiveOfTax, cart.currency)}
                      </span>
                    </div>
                    <p className="text-[11px] text-gray-400 leading-6">
                      مبلغ از نقل‌قول Pricing روی Offer است. مالیات و هزینهٔ ارسال قطعی در Checkout محاسبه می‌شود.
                    </p>
                  </div>
                  <Link
                    href="/checkout"
                    className="mt-4 w-full py-3 rounded-2xl font-black text-sm flex items-center justify-center gap-2 bg-[#2563EB] text-white hover:bg-[#1d4ed8] shadow-lg shadow-[#2563EB]/25"
                    data-testid="cart-checkout-cta"
                  >
                    ادامه خرید
                    <ChevronLeft className="w-4 h-4" />
                  </Link>
                  <div className="flex items-center justify-center gap-3 mt-3 text-[10px] text-gray-400">
                    <span className="flex items-center gap-1">
                      <Shield className="w-3 h-3" /> پرداخت امن
                    </span>
                    <span className="flex items-center gap-1">
                      <Truck className="w-3 h-3" /> ارسال از Host
                    </span>
                  </div>
                </div>

                <div className="bg-white rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm" data-testid="cart-coupon">
                  <h4 className="text-xs md:text-sm font-bold text-gray-900 flex items-center gap-2 mb-3">
                    <Tag className="w-4 h-4 text-[#2563EB]" />
                    کد تخفیف
                  </h4>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      disabled
                      placeholder="فعلاً در دسترس نیست"
                      className="flex-1 px-3.5 py-2.5 rounded-xl text-xs bg-gray-50 border border-gray-200 text-gray-400 outline-none"
                    />
                    <button
                      type="button"
                      disabled
                      className="px-4 py-2.5 rounded-xl bg-gray-200 text-gray-500 text-xs font-bold cursor-not-allowed shrink-0"
                    >
                      اعمال
                    </button>
                  </div>
                  <p className="text-[10px] md:text-xs mt-2 text-amber-700 leading-5">
                    موتور کوپن امن در Backend وصل نیست؛ پذیرش جعلی کد تخفیف انجام نمی‌شود.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </section>
      )}

      <CartBenefits />
    </div>
  );
}

function CartHero({
  itemCount,
  subtotalLabel,
  discountPercent,
}: {
  itemCount: number;
  subtotalLabel: string;
  discountPercent: number;
}) {
  return (
    <section className="w-full" data-testid="cart-hero">
      <div className="relative overflow-hidden rounded-2xl md:rounded-3xl bg-gradient-to-l from-[#2563EB] to-[#1e3a8a] min-h-[160px] md:min-h-[180px]">
        <div className="absolute inset-0 opacity-[0.08]">
          <div className="absolute -top-20 -right-20 w-56 h-56 rounded-full bg-white" />
          <div className="absolute -bottom-16 -left-16 w-40 h-40 rounded-full bg-white" />
          <div className="absolute top-10 left-1/4 w-20 h-20 rounded-full bg-white" />
        </div>
        <div className="relative z-10 flex flex-col items-center justify-center text-center p-6 md:p-10">
          <span className="inline-flex items-center gap-1.5 bg-white/15 backdrop-blur-sm text-white text-[10px] md:text-xs font-bold px-3 py-1.5 rounded-full mb-3">
            <ShoppingBag className="w-3.5 h-3.5" />
            {itemCount.toLocaleString("fa-IR")} کالا در سبد خرید
          </span>
          <h1 className="text-xl md:text-3xl font-black text-white leading-snug mb-1">سبد خرید شما</h1>
          <p className="text-sm md:text-base text-white/80 leading-relaxed">مرور کنید، ویرایش کنید و سفارش دهید</p>
        </div>
      </div>
      <div className="grid grid-cols-3 gap-1.5 md:gap-4 -mt-8 md:-mt-10 relative z-20">
        <MetricCard
          icon={<ShoppingBag className="w-3 h-3 md:w-5 md:h-5 text-[#2563EB]" />}
          value={itemCount.toLocaleString("fa-IR")}
          label="تعداد کالا"
          tone="blue"
        />
        <MetricCard
          icon={<CreditCard className="w-3 h-3 md:w-5 md:h-5 text-emerald-500" />}
          value={subtotalLabel}
          label="جمع کل"
          tone="emerald"
        />
        <MetricCard
          icon={<Percent className="w-3 h-3 md:w-5 md:h-5 text-amber-500" />}
          value={`٪${discountPercent.toLocaleString("fa-IR")}`}
          label="تخفیف"
          tone="amber"
        />
      </div>
    </section>
  );
}

function MetricCard({
  icon,
  value,
  label,
  tone,
}: {
  icon: ReactNode;
  value: string;
  label: string;
  tone: "blue" | "emerald" | "amber";
}) {
  const bg =
    tone === "blue" ? "bg-[#2563EB]/10" : tone === "emerald" ? "bg-emerald-50" : "bg-amber-50";
  return (
    <div className="flex items-center gap-1 md:gap-3 p-1.5 md:p-5 rounded-2xl bg-white border border-gray-200 shadow-lg">
      <div className={`w-6 h-6 md:w-12 md:h-12 rounded-lg md:rounded-xl ${bg} flex items-center justify-center shrink-0`}>
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-[9px] md:text-lg font-black text-gray-900 truncate">{value}</p>
        <p className="text-[7px] md:text-xs text-gray-500 truncate leading-tight">{label}</p>
      </div>
    </div>
  );
}

function CartEmpty({ error }: { error: string | null }) {
  return (
    <div className="py-16 md:py-24 text-center" data-testid="cart-empty">
      <div className="w-28 h-28 md:w-36 md:h-36 rounded-full bg-gray-100 flex items-center justify-center mx-auto mb-6">
        <ShoppingBag className="w-14 h-14 md:w-20 md:h-20 text-gray-300" />
      </div>
      <h2 className="text-xl md:text-2xl font-black text-gray-900 mb-2">سبد خرید شما خالی است</h2>
      <p className="text-sm md:text-base text-gray-500 mb-8 max-w-md mx-auto">
        هنوز کالایی به سبد اضافه نشده است. برای شروع خرید به فروشگاه بروید.
      </p>
      {error ? <p className="text-sm text-red-600 mb-6">{error}</p> : null}
      <Link
        href="/products"
        className="inline-flex items-center gap-2 px-6 py-3 bg-[#2563EB] text-white rounded-xl font-black text-sm hover:bg-[#1d4ed8] shadow-lg shadow-[#2563EB]/25"
      >
        <ArrowLeft className="w-4 h-4" />
        بازگشت به فروشگاه
      </Link>
    </div>
  );
}

function CartBenefits() {
  const benefits = [
    { icon: Truck, title: "ارسال از Host", desc: "هزینه و روش در تسویه مشخص می‌شود" },
    { icon: RotateCcw, title: "بازگشت کالا", desc: "طبق سیاست فروشنده و سفارش" },
    { icon: ShieldCheck, title: "ضمانت اصالت", desc: "Offer زنده از کاتالوگ Tooba" },
    { icon: HeadphonesIcon, title: "پشتیبانی", desc: "پیگیری از پنل مشتری" },
  ];
  return (
    <section className="pt-8 md:pt-10" data-testid="cart-benefits">
      <div className="w-full h-px bg-gradient-to-r from-transparent via-gray-200 to-transparent mb-8" />
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 md:gap-4">
        {benefits.map((item) => (
          <div
            key={item.title}
            className="flex items-center gap-3 p-4 md:p-5 rounded-2xl bg-white border border-gray-200 shadow-sm"
          >
            <div className="w-10 h-10 md:w-12 md:h-12 rounded-xl bg-[#2563EB]/5 flex items-center justify-center shrink-0">
              <item.icon className="w-4 h-4 md:w-5 md:h-5 text-[#2563EB]" />
            </div>
            <div>
              <p className="text-xs md:text-sm font-bold text-gray-900">{item.title}</p>
              <p className="text-[9px] md:text-xs text-gray-500">{item.desc}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
