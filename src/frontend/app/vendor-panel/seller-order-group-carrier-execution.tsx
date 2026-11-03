"use client";

import { OrderGroupCarrierExecution } from "../../features/order-carrier-execution/order-group-carrier-execution.tsx";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "./seller-api";

export function SellerOrderGroupCarrierExecution({ checkoutId }: { checkoutId: string }) {
  const sellerPartyId =
    readSellerPartyId(typeof window === "undefined" ? "" : window.location.search) ?? "";
  const actor = readActorUserId();
  const ownerContextKey =
    sellerPartyId.trim().length > 0 && actor && actor.trim().length > 0
      ? `seller:${sellerPartyId.trim()}:${actor.trim()}`
      : null;
  return (
    <OrderGroupCarrierExecution
      checkoutId={checkoutId}
      apiPrefix="/v1/seller"
      testIdPrefix="seller-order-group-carrier-execution"
      ownerContextKey={ownerContextKey}
      requestHeaders={() => {
        const headers: Record<string, string> = {
          Accept: "application/json",
          [SELLER_PARTY_HEADER]: sellerPartyId,
        };
        if (actor) headers[DEV_ACTOR_HEADER] = actor;
        return headers;
      }}
    />
  );
}
