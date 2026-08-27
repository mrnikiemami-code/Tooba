"use client";

import Link from "next/link";
import { useEffect, useState, type ReactNode } from "react";
import {
  Package,
  Settings,
  ShieldCheck,
  ShoppingBag,
  Store,
  Truck,
  Wallet,
} from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "../seller-api";

/**
 * تنظیمات فروشنده زنده از Seller Dashboard — بدون فرم ذخیرهٔ جعلی پروفایل/لوگو.
 */
export default function VendorSettingsPage() {
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
      <main data-testid="vendor-settings-page">
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
    <main className="space-y-6" data-testid="vendor-settings-page">
      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 md:p-6">
        <div className="flex items-center gap-3">
          <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">
            <Settings className="w-5 h-5" />
          </span>
          <div>
            <h1 className="font-black text-lg">تنظیمات فروشنده</h1>
            <p className="text-xs text-gray-500 mt-1">خلاصهٔ زنده از Host؛ بدون فرم ذخیرهٔ جعلی</p>
          </div>
        </div>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <>
          <section className="bg-gradient-to-l from-[#2563EB] to-[#3B82F6] rounded-2xl p-5 md:p-6 text-white shadow-lg shadow-[#2563EB]/20">
            <div className="flex items-start gap-3">
              <span className="w-11 h-11 rounded-xl bg-white/15 flex items-center justify-center shrink-0">
                <Store className="w-5 h-5" />
              </span>
              <div>
                <p className="text-white/80 text-sm">فروشگاه</p>
                <h2 className="mt-1 text-2xl font-black">{source === "loading" ? "…" : name}</h2>
                <p className="mt-2 text-sm text-white/90 leading-7">
                  نام نمایشی از Seller Dashboard API؛ ویرایش پروفایل کسب‌وکار هنوز capability جدا ندارد.
                </p>
              </div>
            </div>
          </section>

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
        </>
      )}

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[
          { label: "محصولات", href: "/vendor-panel/products", icon: Package, color: "bg-[#2563EB]" },
          { label: "سفارشات", href: "/vendor-panel/orders", icon: ShoppingBag, color: "bg-blue-500" },
          { label: "کیف پول", href: "/vendor-panel/wallet", icon: Wallet, color: "bg-amber-500" },
          { label: "ارسال", href: "/vendor-panel/fulfillments", icon: Truck, color: "bg-indigo-500" },
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

      <section className="bg-white rounded-2xl border border-dashed border-gray-200 shadow-sm p-5">
        <div className="flex items-start gap-3">
          <span className="w-10 h-10 bg-gray-100 text-gray-400 rounded-xl flex items-center justify-center shrink-0">
            <ShieldCheck className="w-5 h-5" />
          </span>
          <div>
            <h2 className="font-black text-base text-gray-900">پروفایل کسب‌وکار</h2>
            <p className="mt-1 text-sm text-gray-500 leading-7">
              ویرایش پروفایل کسب‌وکار / لوگوی فروشگاه هنوز capability جدا ندارد
            </p>
            <p className="mt-3 text-[11px] font-bold text-gray-400">بدون فرم ذخیرهٔ جعلی</p>
          </div>
        </div>
      </section>
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
