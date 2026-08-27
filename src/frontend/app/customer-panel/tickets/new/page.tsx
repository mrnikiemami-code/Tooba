"use client";

import { createCustomerTicket, type CreateTicketInput } from "../../../support/support-api.ts";
import { SupportTicketForm } from "../../../support/support-ui.tsx";

/** فرم تیکت جدید مشتری. */
export default function CustomerNewTicketPage() {
  return (
    <SupportTicketForm
      listHref="/customer-panel/tickets"
      onSubmit={async (input: CreateTicketInput) => {
        const result = await createCustomerTicket(input);
        if (!result.ok) return { ok: false, errorCode: result.errorCode };
        return { ok: true, ticketId: result.snapshot.ticketId };
      }}
    />
  );
}
