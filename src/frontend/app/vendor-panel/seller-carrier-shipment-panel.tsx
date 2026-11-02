"use client";

import { useCallback, useEffect, useState } from "react";
import { authoringMessages } from "../../features/admin-shipping-authoring/messages/authoring-messages.ts";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "./seller-api";

function sellerHeaders(): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: readSellerPartyId(typeof window === "undefined" ? "" : window.location.search) ?? "",
  };
  const actor = readActorUserId();
  if (actor) headers[DEV_ACTOR_HEADER] = actor;
  return headers;
}

export function SellerCarrierShipmentPanel({
  checkoutId,
}: {
  checkoutId: string;
}) {
  const messages = authoringMessages("fa");
  const [status, setStatus] = useState("");
  const [empty, setEmpty] = useState(true);

  const refresh = useCallback(async () => {
    if (!checkoutId) {
      setStatus(messages.carrierShipmentEmpty);
      return;
    }
    const response = await fetch(
      `/v1/seller/shipping-services/shipments?checkoutId=${encodeURIComponent(checkoutId)}`,
      { headers: sellerHeaders() },
    );
    const payload = (await response.json().catch(() => null)) as { errorCode?: string } | unknown[] | null;
    if (!response.ok) {
      const code = payload && !Array.isArray(payload) ? payload.errorCode : null;
      setStatus(code === "shipping.carrier.unconfigured" ? messages.carrierUnconfigured : messages.carrierSaveFailed);
      return;
    }
    const rows = Array.isArray(payload) ? payload : [];
    setEmpty(rows.length === 0);
    setStatus(rows.length === 0 ? messages.carrierShipmentEmpty : "");
  }, [checkoutId, messages.carrierSaveFailed, messages.carrierShipmentEmpty, messages.carrierUnconfigured]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return (
    <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="seller-carrier-shipment-panel">
      <h2 className="text-base font-semibold">{messages.carrierShipmentTitle}</h2>
      <p className="mt-1 text-sm text-muted">{messages.carrierShipmentHint}</p>
      <p className="mt-2 text-sm" data-testid="seller-carrier-shipment-status">
        {status || (empty ? messages.carrierShipmentEmpty : "")}
      </p>
      <p className="mt-1 text-xs text-muted">{messages.carrierLabelUnsupported}</p>
    </section>
  );
}
