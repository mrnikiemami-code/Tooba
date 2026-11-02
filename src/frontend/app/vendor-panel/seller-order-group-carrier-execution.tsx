"use client";

import { AdminOrderGroupCarrierExecution } from "../admin/admin-order-group-carrier-execution";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "./seller-api";

export function SellerOrderGroupCarrierExecution({ checkoutId }: { checkoutId: string }) {
  return (
    <AdminOrderGroupCarrierExecution
      checkoutId={checkoutId}
      apiPrefix="/v1/seller"
      requestHeaders={() => {
        const headers: Record<string, string> = {
          Accept: "application/json",
          [SELLER_PARTY_HEADER]:
            readSellerPartyId(typeof window === "undefined" ? "" : window.location.search) ?? "",
        };
        const actor = readActorUserId();
        if (actor) headers[DEV_ACTOR_HEADER] = actor;
        return headers;
      }}
    />
  );
}
