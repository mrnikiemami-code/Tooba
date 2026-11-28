import { useEffect, useState } from "react";
import type { OfferFulfillmentResponsibility, OfferShippingMethodOption, OfferShippingMethodOptionsPage } from "./types.ts";

export interface OfferShippingMethodsLoader {
  loadShippingMethodOptions?: (input: {
    offerId?: string;
    sellerPartyId?: string | null;
    responsibility: OfferFulfillmentResponsibility | null;
    shippingTypeId: string | null;
  }) => Promise<OfferShippingMethodOptionsPage | null>;
}

/**
 * Loads the bounded published active method list for the current owner/type from the typed adapter.
 * It never fabricates options: a missing/failed read is an explicit error, distinct from a true empty list.
 */
export function useOfferShippingMethods(args: {
  adapter: OfferShippingMethodsLoader & { role: "admin" | "seller"; offerId?: string };
  isEdit: boolean;
  sellerPartyId: string | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  errorMessage: string;
}) {
  const { adapter, isEdit, sellerPartyId, responsibility, shippingTypeId, errorMessage } = args;
  const scope = JSON.stringify([isEdit, adapter.offerId, sellerPartyId, responsibility, shippingTypeId]);
  const [result, setResult] = useState<{
    scope: string;
    adapter: typeof adapter;
    options: OfferShippingMethodOption[];
    error: string | null;
  } | null>(null);

  useEffect(() => {
    const loader = adapter.loadShippingMethodOptions;
    if (adapter.role !== "admin" || !responsibility || !shippingTypeId) {
      setResult(null);
      return;
    }
    let active = true;
    setResult(null);
    const finish = (options: OfferShippingMethodOption[], error: string | null) => {
      if (active) setResult({ scope, adapter, options, error });
    };
    if (!loader) {
      finish([], errorMessage);
    } else {
      void loader({
        offerId: isEdit ? adapter.offerId : undefined,
        sellerPartyId,
        responsibility,
        shippingTypeId,
      }).then(page => {
        if (!page || page.responsibility !== responsibility || page.shippingTypeId !== shippingTypeId) {
          finish([], errorMessage);
        } else {
          finish(page.methods, null);
        }
      }).catch(() => finish([], errorMessage));
    }
    return () => { active = false; };
  }, [adapter, isEdit, sellerPartyId, responsibility, shippingTypeId, scope, errorMessage]);

  // A previous scope's options must not be selectable for even the render before an effect runs.
  if (adapter.role !== "admin" || !responsibility || !shippingTypeId) {
    return { options: [], loading: false, error: null };
  }
  if (!result || result.scope !== scope || result.adapter !== adapter) {
    return { options: [], loading: true, error: null };
  }
  return { options: result.options, loading: false, error: result.error };
}
