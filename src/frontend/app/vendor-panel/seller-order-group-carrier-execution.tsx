"use client";

import { OrderGroupCarrierExecution } from "../../features/order-carrier-execution/order-group-carrier-execution.tsx";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "./seller-api";

export function SellerOrderGroupCarrierExecution({ checkoutId }: { checkoutId: string }) {
  return (
    <OrderGroupCarrierExecution
      checkoutId={checkoutId}
      apiPrefix="/v1/seller"
      testIdPrefix="seller-order-group-carrier-execution"
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
