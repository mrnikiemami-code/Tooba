"use client";

import { useEffect, useState, type ReactNode } from "react";
import { BarChart3, Package, ShoppingBag, Wallet } from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "../seller-api";

/**
 * آمار فروشنده — متریک‌های زنده از Seller Dashboard؛ نمودار درآمد بدون backend نمایش داده نمی‌شود.
 */
export default function VendorAnalyticsPage() {
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

  return (
    <main className="space-y-6" data-testid="seller-analytics-page">
      <div className="bg-gradient-to-l from-[#2563EB] to-[#3B82F6] rounded-2xl p-5 md:p-6 text-white shadow-lg shadow-[#2563EB]/20">
        <p className="text-white/80 text-sm">خانه / آمار و نمودار</p>
        <h1 className="mt-1 text-2xl md:text-3xl font-black flex items-center gap-2">
          <BarChart3 className="w-7 h-7" />
          آمار عملیاتی
        </h1>
        <p className="mt-2 text-sm text-white/90 max-w-2xl leading-7">
          اعداد از همان Seller Dashboard API داشبورد هستند. نمودار درآمد و سری زمانی تا capability معتبر Host نمایش
          داده نمی‌شود.
        </p>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <MetricCard
            label="پیشنهاد فعال"
            value={summary?.activeOffers}
            loading={source === "loading"}
            icon={<Package className="w-5 h-5" />}
            tone="from-blue-500 to-blue-600"
          />
          <MetricCard
            label="سفارش باز"
            value={summary?.openOrders}
            loading={source === "loading"}
            icon={<ShoppingBag className="w-5 h-5" />}
            tone="from-amber-500 to-amber-600"
          />
          <MetricCard
            label="سفارش پرداخت‌شده"
            value={summary?.paidOrders}
            loading={source === "loading"}
            icon={<Wallet className="w-5 h-5" />}
            tone="from-emerald-500 to-emerald-600"
          />
        </div>
      )}

      <section
        className="bg-white rounded-2xl border border-dashed border-gray-200 p-5 shadow-sm"
        data-testid="seller-analytics-charts-unavailable"
      >
        <h2 className="font-black text-lg text-gray-900">نمودارها فعلاً در دسترس نیست</h2>
        <p className="mt-2 text-sm text-gray-500 leading-7 max-w-2xl">
          سری زمانی درآمد، Chart.js یا هر نمودار فروش ساختگی نمایش داده نمی‌شود تا endpoint گزارش‌گیری معتبر در Host
          متصل شود.
        </p>
        <div className="mt-4 h-40 rounded-xl bg-gray-50 border border-gray-100 flex items-center justify-center text-sm text-gray-400">
          محل نمودار · بدون دادهٔ جعلی
        </div>
      </section>
    </main>
  );
}

function MetricCard({
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
