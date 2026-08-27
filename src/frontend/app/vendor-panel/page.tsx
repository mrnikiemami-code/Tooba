"use client";

import Link from "next/link";
import { useEffect, useState, type ReactNode } from "react";
import {
  BarChart3,
  Package,
  Settings,
  ShoppingBag,
  Wallet,
} from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "./seller-api";

/**
 * داشبورد فروشنده با تراکم Shopeiva؛ فقط کارت‌های زنده Host.
 * بدون نمودار فروش، هدف ماهانه، یا درآمد جعلی.
 */
export default function VendorDashboardPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [summary, setSummary] = useState<SellerDashboardSummary | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerDashboard(sellerPartyId).then((result) => {
      setSource(result.source);
      setSummary(result.summary);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }

  useEffect(() => {
    refresh();
  }, []);

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهدهٔ این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  const name = summary?.sellerDisplayName ?? "فروشنده";

  return (
    <main className="space-y-6" data-testid="seller-auth-allowed">
      <div className="bg-gradient-to-l from-[#2563EB] to-[#3B82F6] rounded-2xl p-5 md:p-6 text-white shadow-lg shadow-[#2563EB]/20">
        <p className="text-white/80 text-sm">خانه / داشبورد</p>
        <h1 className="mt-1 text-2xl md:text-3xl font-black">سلام، {name}</h1>
        <p className="mt-2 text-sm text-white/90 max-w-2xl leading-7">
          خلاصهٔ عملیاتی از Host زنده است. درآمد، هدف ماهانه و نمودار ساختگی نمایش داده نمی‌شود.
        </p>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <SummaryCard
            label="پیشنهاد فعال"
            value={summary?.activeOffers}
            loading={source === "loading"}
            icon={<Package className="w-5 h-5" />}
            tone="from-blue-500 to-blue-600"
          />
          <SummaryCard
            label="سفارش باز"
            value={summary?.openOrders}
            loading={source === "loading"}
            icon={<ShoppingBag className="w-5 h-5" />}
            tone="from-amber-500 to-amber-600"
          />
          <SummaryCard
            label="سفارش پرداخت‌شده"
            value={summary?.paidOrders}
            loading={source === "loading"}
            icon={<Wallet className="w-5 h-5" />}
            tone="from-emerald-500 to-emerald-600"
          />
        </div>
      )}

      {/* فقط مسیرهای زنده؛ مشتریان/تخفیف از ناوبری حذف شده‌اند. */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {[
          { label: "محصولات", href: "/vendor-panel/products", icon: Package, color: "bg-[#2563EB]" },
          { label: "سفارشات", href: "/vendor-panel/orders", icon: ShoppingBag, color: "bg-blue-500" },
          { label: "آمار", href: "/vendor-panel/analytics", icon: BarChart3, color: "bg-indigo-500" },
          { label: "کیف پول", href: "/vendor-panel/wallet", icon: Wallet, color: "bg-amber-500" },
          { label: "تنظیمات", href: "/vendor-panel/settings", icon: Settings, color: "bg-slate-600" },
        ].map((action) => (
          <Link
            key={action.href}
            href={action.href}
            className="bg-white rounded-2xl border border-gray-200 p-4 flex flex-col items-center gap-2 hover:shadow-md transition-shadow text-center"
          >
            <span className={`w-10 h-10 ${action.color} text-white rounded-xl flex items-center justify-center`}>
              <action.icon className="w-5 h-5" />
            </span>
            <span className="text-xs font-bold text-gray-800">{action.label}</span>
          </Link>
        ))}
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <section className="bg-white rounded-2xl border border-gray-200 p-5 shadow-sm">
          <h2 className="font-black text-lg">وضعیت عملیاتی زنده</h2>
          <p className="mt-1 text-sm text-gray-500">اعداد فقط از Seller Dashboard API.</p>
          <ul className="mt-4 space-y-3 text-sm">
            <li className="flex items-center justify-between rounded-xl bg-gray-50 px-3 py-3">
              <span>پیشنهادهای فعال</span>
              <span className="font-bold tabular-nums">{summary?.activeOffers?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
            <li className="flex items-center justify-between rounded-xl bg-gray-50 px-3 py-3">
              <span>سفارش‌های در انتظار پرداخت</span>
              <span className="font-bold tabular-nums">{summary?.openOrders?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
            <li className="flex items-center justify-between rounded-xl bg-gray-50 px-3 py-3">
              <span>سفارش‌های پرداخت‌شده</span>
              <span className="font-bold tabular-nums">{summary?.paidOrders?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
          </ul>
        </section>
        <section className="bg-white rounded-2xl border border-gray-200 p-5 shadow-sm">
          <h2 className="font-black text-lg">قابلیت‌های هنوز متصل‌نشده</h2>
          <p className="mt-1 text-sm text-gray-500">
            از ناوبری حذف شده‌اند؛ deep-link صفحهٔ صادقانه نشان می‌دهد.
          </p>
          <ul className="mt-4 grid gap-2 text-sm text-gray-700">
            <li className="rounded-xl bg-gray-50 px-3 py-2">مشتریان · نظرات · تخفیف فروشنده</li>
            <li className="rounded-xl bg-gray-50 px-3 py-2">تیکت · کارت هدیه</li>
          </ul>
        </section>
      </div>
    </main>
  );
}

function SummaryCard({
  label,
  value,
  loading,
  icon,
  tone,
}: {
  label: string;
  value?: number;
  loading: boolean;
  icon: ReactNode;
  tone: string;
}) {
  return (
    <div className="bg-white rounded-2xl border border-gray-200 p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm text-gray-500">{label}</p>
          <p className="mt-2 text-3xl font-black tabular-nums">
            {loading ? "…" : (value ?? 0).toLocaleString("fa-IR")}
          </p>
        </div>
        <span className={`inline-flex size-11 items-center justify-center rounded-xl bg-gradient-to-br ${tone} text-white`}>
          {icon}
        </span>
      </div>
    </div>
  );
}
