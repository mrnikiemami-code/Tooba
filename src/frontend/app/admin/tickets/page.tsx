"use client";

import { useCallback, useEffect, useState } from "react";
import { loadAdminTickets, type TicketListRow } from "../../support/support-api.ts";
import { SupportTicketsList } from "../../support/support-ui.tsx";

/** فهرست تیکت‌های Admin با جستجو/فیلتر. */
export default function AdminTicketsPage() {
  const [rows, setRows] = useState<TicketListRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [status, setStatus] = useState<string | "all">("all");
  const [q, setQ] = useState("");
  const [qDraft, setQDraft] = useState("");

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    setDenied(false);
    void loadAdminTickets({
      status: status === "all" ? undefined : status,
      q: q || undefined,
      pageSize: 100,
    }).then((result) => {
      setLoading(false);
      if (result.state === "denied") {
        setDenied(true);
        setError("دسترسی Admin مجاز نیست");
        setRows([]);
        return;
      }
      if (result.state === "error" || !result.data) {
        setError(result.message ?? "خطا در خواندن تیکت‌ها");
        setRows([]);
        return;
      }
      setRows(result.data.items);
    });
  }, [status, q]);

  useEffect(refresh, [refresh]);

  if (denied) {
    return (
      <main data-testid="admin-auth-denied" className="rounded-2xl border border-red-200 bg-red-50 p-6 text-sm text-red-700" dir="rtl">
        دسترسی به تیکت‌های پشتیبانی مجاز نیست.
      </main>
    );
  }

  return (
    <div className="space-y-4" dir="rtl">
      <form
        className="flex gap-2 flex-wrap"
        onSubmit={(e) => {
          e.preventDefault();
          setQ(qDraft.trim());
        }}
      >
        <input
          value={qDraft}
          onChange={(e) => setQDraft(e.target.value)}
          placeholder="جستجو در موضوع..."
          className="flex-1 min-w-[200px] px-4 py-2.5 bg-white rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
        />
        <button
          type="submit"
          className="px-4 py-2.5 bg-[#E53935] text-white rounded-xl text-sm font-bold hover:bg-[#c62828]"
        >
          جستجو
        </button>
      </form>
      <SupportTicketsList
        audience="admin"
        basePath="/admin/tickets"
        rows={rows}
        loading={loading}
        error={error}
        onRetry={refresh}
        statusFilter={status}
        onFilterStatus={setStatus}
        searchEnabled={false}
        showRequester
      />
    </div>
  );
}
