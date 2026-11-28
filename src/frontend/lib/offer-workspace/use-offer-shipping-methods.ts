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
  const [options, setOptions] = useState<OfferShippingMethodOption[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loader = adapter.loadShippingMethodOptions;
    if (!loader || adapter.role !== "admin") {
      setOptions([]);
      return;
    }
    if (!responsibility || !shippingTypeId) {
      setOptions([]);
      setLoading(false);
      setError(null);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    void loader({
      offerId: isEdit ? adapter.offerId : undefined,
      sellerPartyId,
      responsibility,
      shippingTypeId,
    })
      .then((page) => {
        if (!active) return;
        if (!page) {
          setOptions([]);
          setError(errorMessage);
        } else {
          setOptions(page.methods);
        }
        setLoading(false);
      })
      .catch(() => {
        if (!active) return;
        setOptions([]);
        setError(errorMessage);
        setLoading(false);
      });
    return () => { active = false; };
  }, [adapter, isEdit, sellerPartyId, responsibility, shippingTypeId, errorMessage]);

  return { options, loading, error };
}
