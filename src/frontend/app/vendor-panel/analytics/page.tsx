"use client";

import { useEffect, useState } from "react";
import {
  Activity,
  Award,
  BarChart3,
  Calendar,
  ChevronDown,
  Package,
  PieChart,
  ShoppingBag,
  TrendingUp,
  Users,
  Wallet,
} from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "../seller-api";

const periods = ["امروز", "این هفته", "این ماه", "۳ ماه اخیر", "سالانه"] as const;

function toFa(num: number | null | undefined): string {
  if (num == null || !Number.isFinite(num)) return "۰";
  return num.toLocaleString("fa-IR");
}

/**
 * آمار فروشنده — کروم Shopeiva analytics؛ متریک زنده Host؛ نمودارها پوسته خالی.
 */
export default function VendorAnalyticsPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [summary, setSummary] = useState<SellerDashboardSummary | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [period, setPeriod] = useState<(typeof periods)[number]>("این هفته");
  const [isPeriodOpen, setIsPeriodOpen] = useState(false);

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
      <main data-testid="seller-analytics-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهدهٔ آمار این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  const loading = source === "loading";
  const orderCount = (summary?.openOrders ?? 0) + (summary?.paidOrders ?? 0);

  const stats = [
    {
      id: 1,
      label: "کل فروش",
      value: "—",
      change: "—",
      icon: TrendingUp,
      color: "text-emerald-500",
      hostBound: false,
    },
    {
      id: 2,
      label: "تعداد سفارشات",
      value: loading ? "…" : toFa(orderCount),
      change: "—",
      icon: ShoppingBag,
      color: "text-blue-500",
      hostBound: true,
    },
    {
      id: 3,
      label: "محصولات فروخته شده",
      value: loading ? "…" : toFa(summary?.activeOffers ?? 0),
      change: "—",
      icon: Package,
      color: "text-purple-500",
      hostBound: true,
      note: "پیشنهاد فعال (نه تعداد فروخته‌شده)",
    },
    {
      id: 4,
      label: "مشتریان جدید",
      value: "۰",
      change: "—",
      icon: Users,
      color: "text-amber-500",
      hostBound: false,
    },
  ];

  const chartShells = [
    { title: "روند فروش و بازدید", icon: Activity },
    { title: "دسته‌بندی فروش", icon: PieChart },
    { title: "عملکرد فروشندگان", icon: Award },
    { title: "رشد مشتریان", icon: TrendingUp },
  ];

  return (
    <main className="space-y-4" data-testid="seller-analytics-page">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10 rounded-xl bg-[#2563EB]/10 flex items-center justify-center">
            <BarChart3 className="w-5 h-5 text-[#2563EB]" />
          </div>
          <div>
            <h2 className="text-lg font-bold text-gray-900">آمار و نمودارها</h2>
            <p className="text-xs text-gray-500">تحلیل جامع فروش و عملکرد فروشگاه</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <div className="relative">
            <button
              type="button"
              onClick={() => setIsPeriodOpen(!isPeriodOpen)}
              className="px-4 py-2 bg-white rounded-xl text-sm text-gray-700 border border-gray-200 hover:border-[#2563EB]/50 transition-colors flex items-center gap-2"
            >
              <Calendar className="w-4 h-4" />
              {period}
              <ChevronDown className={`w-4 h-4 transition-transform ${isPeriodOpen ? "rotate-180" : ""}`} />
            </button>
            {isPeriodOpen ? (
              <div className="absolute top-full right-0 mt-1 bg-white rounded-xl border border-gray-200 shadow-lg z-10 min-w-[120px]">
                {periods.map((p) => (
                  <button
                    key={p}
                    type="button"
                    onClick={() => {
                      setPeriod(p);
                      setIsPeriodOpen(false);
                    }}
                    className={`block w-full text-right px-4 py-2 text-sm hover:bg-gray-100 transition-colors ${
                      period === p ? "text-[#2563EB] font-bold" : "text-gray-700"
                    }`}
                  >
                    {p}
                  </button>
                ))}
              </div>
            ) : null}
          </div>
          <button
            type="button"
            disabled
            title="خروجی گزارش تا endpoint Host فعال نیست"
            className="px-4 py-2 bg-gray-200 text-gray-500 rounded-xl text-xs font-bold cursor-not-allowed flex items-center gap-1"
          >
            <Wallet className="w-4 h-4" />
            خروجی
          </button>
        </div>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          {stats.map((stat) => {
            const Icon = stat.icon;
            return (
              <div
                key={stat.id}
                className="bg-white rounded-2xl p-4 border border-gray-200 hover:shadow-lg transition-all duration-300 group"
              >
                <div className="flex items-center justify-between">
                  <div className="w-10 h-10 rounded-xl bg-[#2563EB]/10 flex items-center justify-center group-hover:scale-110 transition-transform">
                    <Icon className={`w-5 h-5 ${stat.color}`} />
                  </div>
                  <span className="text-xs font-bold text-gray-400">{stat.change}</span>
                </div>
                <p className="text-xl font-black text-gray-900 mt-2 tabular-nums">{stat.value}</p>
                <p className="text-xs text-gray-500">{stat.label}</p>
                {"note" in stat && stat.note ? <p className="text-[10px] text-gray-400 mt-1">{stat.note}</p> : null}
                {!stat.hostBound ? <p className="text-[10px] text-gray-400 mt-1">بدون متریک Host</p> : null}
              </div>
            );
          })}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4" data-testid="seller-analytics-charts-unavailable">
        {chartShells.map((shell) => {
          const Icon = shell.icon;
          return (
            <div
              key={shell.title}
              className="bg-white rounded-2xl p-4 border border-gray-200 hover:shadow-lg transition-all"
            >
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-bold text-gray-900 flex items-center gap-2">
                  <Icon className="w-4 h-4 text-[#2563EB]" />
                  {shell.title}
                </h3>
                <span className="text-[10px] text-gray-400">{period}</span>
              </div>
              <div className="h-40 rounded-xl bg-gray-50 border border-dashed border-gray-200 flex items-center justify-center text-sm text-gray-400">
                پوسته نمودار · بدون دادهٔ جعلی
              </div>
            </div>
          );
        })}
      </div>
    </main>
  );
}
