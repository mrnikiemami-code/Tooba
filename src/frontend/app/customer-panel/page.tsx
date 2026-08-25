"use client";

import Link from "next/link";
import { Eye, Heart, Package, Sparkles, Star, WalletCards } from "lucide-react";
import { useEffect, useState } from "react";
import {
  type CustomerDashboardPage,
  formatCustomerMoney,
  formatCustomerOrderStatus,
  loadCustomerDashboard,
} from "./customer-api";

/**
 * پیشخوان Shopeiva با شمارنده و سفارش‌های واقعی؛ نمودار بازدید/فروش جعلی حذف شده است.
 */
export default function CustomerDashboard() {
  const [page, setPage] = useState<CustomerDashboardPage | null | undefined>(undefined);

  useEffect(() => {
    void loadCustomerDashboard().then(setPage);
  }, []);

  if (page === undefined) {
    return <CustomerPanelLoading />;
  }
  if (!page) {
    return <CustomerPanelError />;
  }

  return (
    <div className="space-y-5">
      <section className="rounded-2xl border border-red-100 bg-gradient-to-l from-red-50 to-white p-5 md:p-7">
        <div className="flex items-start gap-3">
          <Sparkles className="w-6 h-6 text-amber-500 shrink-0 mt-1" />
          <div>
            <h1 className="text-xl md:text-2xl font-black">
              خوش آمدی، <span className="text-[#2563EB]">{page.displayName}</span>
            </h1>
            <p className="text-sm text-gray-600 mt-2 leading-7">
              سفارش‌ها و اطلاعات خرید خود را در ساختار پنل مشتری مدیریت کنید.
            </p>
          </div>
        </div>
        <span className="inline-flex mt-4 items-center gap-2 bg-white border border-gray-200 rounded-xl px-4 py-2 text-xs font-bold shadow-sm">
          <Star className="w-4 h-4 text-amber-500" />
          کاربر ویژه
        </span>
      </section>

      <section className="grid grid-cols-2 xl:grid-cols-4 gap-3">
        <Metric icon={Package} value={page.totalOrders} label="کل سفارش‌ها" tone="blue" />
        <Metric icon={WalletCards} value={page.paidOrders} label="پرداخت‌شده" tone="green" />
        <Metric icon={Eye} value={page.pendingOrders} label="در انتظار" tone="amber" />
        <Metric icon={Heart} value={0} label="علاقه‌مندی" tone="pink" suffix={page.wishlistAvailable ? "" : "غیرفعال"} />
      </section>

      <section className="bg-white rounded-2xl border border-gray-100 p-4 md:p-6 shadow-sm">
        <div className="flex items-center justify-between mb-5">
          <h2 className="font-black text-lg">آخرین سفارش‌ها</h2>
          <Link href="/customer-panel/orders" className="text-xs font-bold text-[#2563EB]">
            مشاهده همه
          </Link>
        </div>
        {page.recentOrders.length === 0 ? (
          <div className="py-12 text-center text-gray-500">
            <Package className="w-10 h-10 mx-auto mb-3 text-gray-300" />
            <p className="font-bold">هنوز سفارشی ثبت نشده است.</p>
            <Link href="/products" className="inline-flex mt-4 bg-[#2563EB] text-white rounded-xl px-4 py-2 text-sm">
              شروع خرید
            </Link>
          </div>
        ) : (
          <div className="space-y-3">
            {page.recentOrders.map((order) => (
              <Link
                key={order.checkoutId}
                href={`/customer-panel/orders/${order.checkoutId}`}
                className="flex flex-wrap items-center gap-3 border border-gray-100 rounded-xl px-4 py-3 hover:border-blue-200"
              >
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-bold truncate">سفارش {order.reference}</p>
                  <p className="text-xs text-gray-400 mt-1">
                    {order.itemCount.toLocaleString("fa-IR")} کالا · {new Date(order.submittedAt).toLocaleDateString("fa-IR")}
                  </p>
                </div>
                <span className="text-xs rounded-lg bg-blue-50 text-[#2563EB] px-2.5 py-1 font-bold">
                  {formatCustomerOrderStatus(order.paymentState)}
                </span>
                <strong className="text-sm">{formatCustomerMoney(order.payableAmount, order.currency)}</strong>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function Metric({
  icon: Icon,
  value,
  label,
  tone,
  suffix,
}: {
  icon: typeof Package;
  value: number;
  label: string;
  tone: "blue" | "green" | "amber" | "pink";
  suffix?: string;
}) {
  const tones = {
    blue: "bg-blue-500",
    green: "bg-emerald-500",
    amber: "bg-amber-500",
    pink: "bg-pink-500",
  };
  return (
    <article className="bg-white rounded-2xl border border-gray-100 p-4 md:p-5 shadow-sm min-h-28">
      <div className={`w-10 h-10 ${tones[tone]} text-white rounded-xl flex items-center justify-center mb-3`}>
        <Icon className="w-5 h-5" />
      </div>
      <strong className="text-xl md:text-2xl">{value.toLocaleString("fa-IR")}</strong>
      <p className="text-xs text-gray-500 mt-1">{label}</p>
      {suffix ? <p className="text-[10px] text-gray-400 mt-1">{suffix}</p> : null}
    </article>
  );
}

function CustomerPanelLoading() {
  return <div className="bg-white rounded-2xl border p-8 text-center text-gray-500">در حال دریافت اطلاعات مشتری...</div>;
}

function CustomerPanelError() {
  return (
    <div className="bg-white rounded-2xl border border-red-100 p-8 text-center">
      <h1 className="font-black text-lg">پنل مشتری در دسترس نیست</h1>
      <p className="text-sm text-gray-500 mt-2">نشست معتبر نیست یا Host پاسخ نمی‌دهد.</p>
    </div>
  );
}
