"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import {
  closeSellerTicket,
  loadSellerTicketDetail,
  reopenSellerTicket,
  replySellerTicket,
  type TicketSnapshot,
} from "../../../support/support-api.ts";
import { SupportTicketThread } from "../../../support/support-ui.tsx";
import { readSellerPartyId } from "../../seller-api.ts";

/** جزئیات و گفتگوی تیکت فروشنده. */
export default function VendorTicketDetailPage() {
  const params = useParams<{ id: string }>();
  const ticketId = params?.id ?? "";
  const [snapshot, setSnapshot] = useState<TicketSnapshot | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId || !ticketId) {
      setLoading(false);
      setError("seller.identity.missing");
      return;
    }
    setLoading(true);
    setError(null);
    void loadSellerTicketDetail(sellerPartyId, ticketId).then((result) => {
      setLoading(false);
      if (result.denied || !result.snapshot) {
        setError(result.message ?? "تیکت پیدا نشد یا دسترسی ندارید");
        setSnapshot(null);
        return;
      }
      setSnapshot(result.snapshot);
    });
  }, [ticketId]);

  useEffect(refresh, [refresh]);

  return (
    <SupportTicketThread
      audience="seller"
      listHref="/vendor-panel/tickets"
      snapshot={snapshot}
      loading={loading}
      error={error}
      onReply={async (body) => {
        const sellerPartyId = readSellerPartyId(window.location.search);
        if (!sellerPartyId) return { ok: false, errorCode: "seller.identity.missing" };
        const result = await replySellerTicket(sellerPartyId, ticketId, {
          body,
          idempotencyKey: crypto.randomUUID(),
        });
        if (!result.ok) return { ok: false, errorCode: result.errorCode };
        setSnapshot(result.snapshot);
        return { ok: true };
      }}
      onClose={async () => {
        const sellerPartyId = readSellerPartyId(window.location.search);
        if (!sellerPartyId) return;
        const result = await closeSellerTicket(sellerPartyId, ticketId);
        if (result.ok) setSnapshot(result.snapshot);
      }}
      onReopen={async () => {
        const sellerPartyId = readSellerPartyId(window.location.search);
        if (!sellerPartyId) return;
        const result = await reopenSellerTicket(sellerPartyId, ticketId);
        if (result.ok) setSnapshot(result.snapshot);
      }}
    />
  );
}
