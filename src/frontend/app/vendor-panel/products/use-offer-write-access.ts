"use client";

import { useEffect, useState } from "react";
import { createSellerAccessApi, hasGlobalOwnerCapability } from "../../access-control/access-control-api";
import { readActorUserId } from "../seller-api";

export type OfferWriteAccess = "loading" | "allowed" | "denied" | "unavailable";

/** Presentation only: AccessControl remains authoritative; the Host rechecks every write. */
export function useOfferWriteAccess(sellerPartyId: string | null, permission: "product.create" | "product.edit"): OfferWriteAccess {
  const [state, setState] = useState<{ seller: string; permission: string; status: OfferWriteAccess } | null>(null);
  useEffect(() => {
    if (!sellerPartyId) return;
    let active = true;
    const actor = readActorUserId();
    setState({ seller: sellerPartyId, permission, status: "loading" });
    void createSellerAccessApi().getMyCapabilities().then(effective => {
      if (!active) return;
      const allowed = effective.userId === actor && hasGlobalOwnerCapability(effective, permission);
      setState({ seller: sellerPartyId, permission, status: allowed ? "allowed" : "denied" });
    }).catch(() => {
      if (active) setState({ seller: sellerPartyId, permission, status: "unavailable" });
    });
    return () => { active = false; };
  }, [sellerPartyId, permission]);
  return state && state.seller === sellerPartyId && state.permission === permission ? state.status : "loading";
}
