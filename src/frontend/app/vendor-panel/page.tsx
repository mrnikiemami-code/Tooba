"use client";

import Link from "next/link";
import { useEffect, useState, type ReactNode } from "react";
import { Package, ShoppingBag, Wallet } from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "./seller-api";

/**
 * داشبورد فروشنده با کارت‌های واقعی و تراکم پنل Shopeiva؛ بدون نمودار جعلی.
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

  return (
    <main data-testid="seller-auth-allowed">
      <div className="mb-6 flex flex-wrap items-end justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / داشبورد</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">داشبورد فروشنده</h1>
          <p className="mt-1 text-base text-muted">{summary?.sellerDisplayName ?? "خلاصهٔ عملیاتی زنده"}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link
            className="inline-flex min-h-11 items-center rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground shadow-sm"
            href="/vendor-panel/products"
          >
            محصولات
          </Link>
          <Link
            className="inline-flex min-h-11 items-center rounded-ds border border-border bg-surface px-4 text-sm font-medium"
            href="/vendor-panel/orders"
          >
            سفارش‌ها
          </Link>
        </div>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-3">
          <SummaryCard
            label="پیشنهاد فعال"
            value={summary?.activeOffers}
            loading={source === "loading"}
            icon={<Package className="size-5" />}
            tone="bg-[rgb(239_246_255)] text-primary"
          />
          <SummaryCard
            label="سفارش باز"
            value={summary?.openOrders}
            loading={source === "loading"}
            icon={<ShoppingBag className="size-5" />}
            tone="bg-[rgb(255_247_237)] text-[rgb(194_65_12)]"
          />
          <SummaryCard
            label="سفارش پرداخت‌شده"
            value={summary?.paidOrders}
            loading={source === "loading"}
            icon={<Wallet className="size-5" />}
            tone="bg-[rgb(240_253_244)] text-[rgb(22_163_74)]"
          />
        </div>
      )}

      <div className="mt-6 grid gap-4 lg:grid-cols-2">
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
          <h2 className="text-lg font-semibold">وضعیت عملیاتی</h2>
          <p className="mt-2 text-sm text-muted">
            خلاصه از دادهٔ زندهٔ Host است. نمودار فروش یا هدف ماهانهٔ ساختگی نمایش داده نمی‌شود.
          </p>
          <ul className="mt-4 space-y-3 text-sm">
            <li className="flex items-center justify-between rounded-ds bg-secondary/60 px-3 py-3">
              <span>پیشنهادهای فعال</span>
              <span className="font-semibold tabular-nums">{summary?.activeOffers?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
            <li className="flex items-center justify-between rounded-ds bg-secondary/60 px-3 py-3">
              <span>سفارش‌های در انتظار پرداخت</span>
              <span className="font-semibold tabular-nums">{summary?.openOrders?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
            <li className="flex items-center justify-between rounded-ds bg-secondary/60 px-3 py-3">
              <span>سفارش‌های پرداخت‌شده</span>
              <span className="font-semibold tabular-nums">{summary?.paidOrders?.toLocaleString("fa-IR") ?? "…"}</span>
            </li>
          </ul>
        </section>
        <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
          <h2 className="text-lg font-semibold">میان‌برهای پنل</h2>
          <div className="mt-4 grid gap-3">
            <Link className="rounded-ds border border-border px-4 py-3 text-sm hover:bg-secondary" href="/vendor-panel/products">
              مدیریت محصولات و Offer
            </Link>
            <Link className="rounded-ds border border-border px-4 py-3 text-sm hover:bg-secondary" href="/vendor-panel/orders">
              پیگیری سفارش‌های فروشنده
            </Link>
          </div>
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
    <div className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm text-muted">{label}</p>
          <p className="mt-2 text-3xl font-semibold tabular-nums">
            {loading ? "…" : (value ?? 0).toLocaleString("fa-IR")}
          </p>
        </div>
        <span className={`inline-flex size-11 items-center justify-center rounded-full ${tone}`}>{icon}</span>
      </div>
    </div>
  );
}
