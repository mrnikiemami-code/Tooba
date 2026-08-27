"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import {
  closeCustomerTicket,
  loadCustomerTicketDetail,
  reopenCustomerTicket,
  replyCustomerTicket,
  type TicketSnapshot,
} from "../../../support/support-api.ts";
import { SupportTicketThread } from "../../../support/support-ui.tsx";

/** جزئیات و گفتگوی تیکت مشتری. */
export default function CustomerTicketDetailPage() {
  const params = useParams<{ id: string }>();
  const ticketId = params?.id ?? "";
  const [snapshot, setSnapshot] = useState<TicketSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    if (!ticketId) return;
    setLoading(true);
    setError(null);
    void loadCustomerTicketDetail(ticketId).then((row) => {
      setLoading(false);
      if (!row) {
        setError("تیکت پیدا نشد یا دسترسی ندارید");
        setSnapshot(null);
        return;
      }
      setSnapshot(row);
    });
  }, [ticketId]);

  useEffect(refresh, [refresh]);

  return (
    <SupportTicketThread
      audience="customer"
      listHref="/customer-panel/tickets"
      snapshot={snapshot}
      loading={loading}
      error={error}
      onReply={async (body) => {
        const result = await replyCustomerTicket(ticketId, { body, idempotencyKey: crypto.randomUUID() });
        if (!result.ok) return { ok: false, errorCode: result.errorCode };
        setSnapshot(result.snapshot);
        return { ok: true };
      }}
      onClose={async () => {
        const result = await closeCustomerTicket(ticketId);
        if (result.ok) setSnapshot(result.snapshot);
      }}
      onReopen={async () => {
        const result = await reopenCustomerTicket(ticketId);
        if (result.ok) setSnapshot(result.snapshot);
      }}
    />
  );
}
