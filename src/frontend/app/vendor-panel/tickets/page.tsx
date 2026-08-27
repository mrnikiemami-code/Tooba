"use client";

import { useCallback, useEffect, useState } from "react";
import { loadSellerTickets, type TicketListRow } from "../../support/support-api.ts";
import { SupportTicketsList } from "../../support/support-ui.tsx";
import { readSellerPartyId, type HostReadSource } from "../seller-api.ts";

/** فهرست تیکت‌های فروشنده. */
export default function VendorTicketsPage() {
  const [rows, setRows] = useState<TicketListRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [status, setStatus] = useState<string | "all">("all");
  const [source, setSource] = useState<HostReadSource | "loading">("loading");

  const refresh = useCallback(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setLoading(false);
      setSource("error");
      setError("seller.identity.missing");
      return;
    }
    setLoading(true);
    setError(null);
    setDenied(false);
    void loadSellerTickets(sellerPartyId, {
      status: status === "all" ? undefined : status,
      pageSize: 100,
    }).then((result) => {
      setLoading(false);
      setSource(result.source);
      setRows(result.page.items);
      setDenied(Boolean(result.denied));
      if (result.denied) setError("دسترسی مجاز نیست");
      else if (result.source === "error") setError(result.message ?? "خطا در خواندن تیکت‌ها");
    });
  }, [status]);

  useEffect(refresh, [refresh]);

  if (denied) {
    return (
      <main data-testid="seller-auth-denied" className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700" dir="rtl">
        این Actor مجوز مشاهدهٔ تیکت‌های این فروشنده را ندارد.
        <button type="button" onClick={refresh} className="mr-3 text-[#E53935] font-bold hover:underline">
          تلاش مجدد
        </button>
      </main>
    );
  }

  return (
    <div data-source={source}>
      <SupportTicketsList
        audience="seller"
        basePath="/vendor-panel/tickets"
        rows={rows}
        loading={loading}
        error={error}
        onRetry={refresh}
        statusFilter={status}
        onFilterStatus={setStatus}
      />
    </div>
  );
}
