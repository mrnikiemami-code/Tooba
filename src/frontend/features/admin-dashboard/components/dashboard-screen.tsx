"use client";

import Link from "next/link";
import { useEffect, useState, type ReactNode } from "react";
import { Package, ShoppingBag, Star, Store, Users } from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import type { AdminResult } from "../../../lib/admin/admin-result.ts";
import { loadAdminDashboard, type AdminDashboard } from "../api/dashboard-api.ts";

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="سامانه هویت فعلی را مدیر تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function Summary({ label, value, icon, tone }: { label: string; value?: number; icon: ReactNode; tone: string }) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="mt-2 text-3xl font-black tabular-nums">{value?.toLocaleString("fa-IR") ?? "…"}</p>
        </div>
        <span className={`inline-flex size-11 items-center justify-center rounded-xl bg-gradient-to-br ${tone} text-white`}>{icon}</span>
      </div>
    </div>
  );
}

function Metric({ label, value }: { label: string; value?: number }) {
  return (
    <div className="flex items-center justify-between rounded-xl bg-gray-50 px-4 py-3">
      <span>{label}</span>
      <strong className="tabular-nums">{value?.toLocaleString("fa-IR") ?? "…"}</strong>
    </div>
  );
}

/** داشبورد Admin با تراکم Shopeiva و فقط متریک‌های زندهٔ Host؛ بدون نمودار/درآمد جعلی. */
export function AdminDashboardScreen() {
  const [result, setResult] = useState<AdminResult<AdminDashboard>>({ state: "ok", data: null, status: 0 });
  const refresh = () => void loadAdminDashboard().then(setResult);
  useEffect(refresh, []);
  if (result.state === "denied") return <Denied retry={refresh} />;
  return (
    <main className="space-y-6" data-testid="admin-dashboard">
      <div className="bg-gradient-to-l from-[#2563EB] to-[#3B82F6] rounded-2xl p-5 md:p-6 text-white shadow-lg shadow-[#2563EB]/20">
        <p className="text-white/80 text-sm">خانه / داشبورد</p>
        <h1 className="mt-1 text-2xl md:text-3xl font-black">مرکز عملیات توبا</h1>
        <p className="mt-2 text-sm text-white/90 max-w-2xl leading-7">
          خلاصهٔ زنده از فروشگاه. درآمد، مجموع فروش، نرخ تبدیل و نمودار ساختگی نمایش داده نمی‌شود.
        </p>
      </div>
      {result.state === "error" ? (
        <ErrorState title="فروشگاه در دسترس نیست" detail={result.message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Summary label="محصول فعال" value={result.data?.activeProducts} icon={<Package className="size-5" />} tone="from-blue-500 to-blue-600" />
            <Summary label="سفارش باز" value={result.data?.openOrders} icon={<ShoppingBag className="size-5" />} tone="from-amber-500 to-amber-600" />
            <Summary label="فروشنده" value={result.data?.sellersCount} icon={<Store className="size-5" />} tone="from-violet-500 to-violet-600" />
            <Summary label="مشتری سفارش‌دهنده" value={result.data?.customersCount} icon={<Users className="size-5" />} tone="from-emerald-500 to-emerald-600" />
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
            {[
              { label: "محصولات", href: "/admin/products", icon: Package },
              { label: "سفارش‌ها", href: "/admin/orders", icon: ShoppingBag },
              { label: "فروشندگان", href: "/admin/sellers", icon: Store },
              { label: "مشتریان", href: "/admin/customers", icon: Users },
              { label: "نظرات", href: "/admin/reviews", icon: Star },
            ].map((action) => (
              <Link
                key={action.href}
                href={action.href}
                className="bg-white rounded-2xl border border-gray-200 p-4 flex flex-col items-center gap-2 hover:shadow-md transition-shadow text-center"
              >
                <span className="w-10 h-10 bg-[#2563EB] text-white rounded-xl flex items-center justify-center">
                  <action.icon className="w-5 h-5" />
                </span>
                <span className="text-xs font-bold text-gray-800">{action.label}</span>
              </Link>
            ))}
          </div>
          <section className="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm">
            <h2 className="text-lg font-black">وضعیت سفارش و عرضه</h2>
            <p className="mt-1 text-sm text-gray-500">اعداد فقط از داشبورد عملیاتی فروشگاه.</p>
            <div className="mt-4 grid gap-3 sm:grid-cols-3">
              <Metric label="پیشنهاد فعال" value={result.data?.activeOffers} />
              <Metric label="پرداخت‌شده" value={result.data?.paidOrders} />
              <Metric label="در انتظار پرداخت" value={result.data?.pendingOrders} />
            </div>
          </section>
        </>
      )}
    </main>
  );
}
