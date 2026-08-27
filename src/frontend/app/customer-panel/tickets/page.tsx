"use client";

import { useCallback, useEffect, useState } from "react";
import { loadCustomerTickets, type TicketListRow } from "../../support/support-api.ts";
import { SupportTicketsList } from "../../support/support-ui.tsx";

/** فهرست تیکت‌های مشتری — زنده از Host via BFF. */
export default function CustomerTicketsPage() {
  const [rows, setRows] = useState<TicketListRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | "all">("all");

  const refresh = useCallback(() => {
    setLoading(true);
    setError(null);
    void loadCustomerTickets({
      status: status === "all" ? undefined : status,
      pageSize: 100,
    }).then((page) => {
      setLoading(false);
      if (!page) {
        setError("خواندن تیکت‌ها از Host ناموفق بود");
        setRows([]);
        return;
      }
      setRows(page.items);
    });
  }, [status]);

  useEffect(refresh, [refresh]);

  return (
    <SupportTicketsList
      audience="customer"
      basePath="/customer-panel/tickets"
      rows={rows}
      loading={loading}
      error={error}
      onRetry={refresh}
      statusFilter={status}
      onFilterStatus={setStatus}
    />
  );
}
