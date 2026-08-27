"use client";

import { createSellerTicket, type CreateTicketInput } from "../../../support/support-api.ts";
import { SupportTicketForm } from "../../../support/support-ui.tsx";
import { readSellerPartyId } from "../../seller-api.ts";

/** فرم تیکت جدید فروشنده. */
export default function VendorNewTicketPage() {
  return (
    <SupportTicketForm
      listHref="/vendor-panel/tickets"
      onSubmit={async (input: CreateTicketInput) => {
        const sellerPartyId = readSellerPartyId(window.location.search);
        if (!sellerPartyId) return { ok: false, errorCode: "seller.identity.missing" };
        const result = await createSellerTicket(sellerPartyId, input);
        if (!result.ok) return { ok: false, errorCode: result.errorCode };
        return { ok: true, ticketId: result.snapshot.ticketId };
      }}
    />
  );
}
