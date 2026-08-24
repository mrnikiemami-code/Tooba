"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../design-system";
import {
  loadSellerDashboard,
  readSellerPartyId,
  type HostReadSource,
  type SellerDashboardSummary,
} from "./seller-api";

/**
 * داشبورد باریک فروشنده با کارت‌های واقعی بدون نمودار جعلی.
 */
export default function VendorDashboardPage() {
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [summary, setSummary] = useState<SellerDashboardSummary | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);

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
    });
  }

  useEffect(() => {
    refresh();
  }, []);

  return (
    <main className="w-full p-6 md:p-8">
      <div className="mb-6">
        <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">داشبورد فروشنده</h1>
        <p className="mt-1 text-[length:var(--type-body)] text-muted">
          {summary?.sellerDisplayName ?? "خلاصهٔ عملیاتی زنده"}
        </p>
      </div>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-3">
          <SummaryCard label="پیشنهاد فعال" value={summary?.activeOffers} loading={source === "loading"} />
          <SummaryCard label="سفارش باز" value={summary?.openOrders} loading={source === "loading"} />
          <SummaryCard label="سفارش پرداخت‌شده" value={summary?.paidOrders} loading={source === "loading"} />
        </div>
      )}
      <div className="mt-8 flex flex-wrap gap-3">
        <Link className="inline-flex min-h-11 items-center rounded-ds bg-primary px-4 text-sm font-medium text-primary-foreground" href="/vendor-panel/products">
          محصولات
        </Link>
        <Link className="inline-flex min-h-11 items-center rounded-ds border border-border px-4 text-sm font-medium" href="/vendor-panel/orders">
          سفارش‌ها
        </Link>
      </div>
    </main>
  );
}

function SummaryCard({ label, value, loading }: { label: string; value?: number; loading: boolean }) {
  return (
    <div className="rounded-ds border border-border bg-surface-elevated p-5 shadow-sm">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-2 text-3xl font-semibold tabular-nums">{loading ? "…" : value ?? 0}</p>
    </div>
  );
}
