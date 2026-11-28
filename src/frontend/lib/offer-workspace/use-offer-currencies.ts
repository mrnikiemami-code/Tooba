"use client";

import { useEffect, useState } from "react";
import type { OfferCurrencyOption, OfferWorkspaceAdapter } from "./types";

/**
 * Loads the Pricing-owned authoritative currency reference for the shared Offer price editor.
 * Adapters that do not expose currencies keep the store-default-only behavior.
 */
export function useOfferCurrencies(adapter: OfferWorkspaceAdapter): OfferCurrencyOption[] {
  const [rows, setRows] = useState<OfferCurrencyOption[]>([]);
  useEffect(() => {
    if (!adapter.loadCurrencies) return;
    let active = true;
    void adapter.loadCurrencies()
      .then((next) => { if (active) setRows(next); })
      .catch(() => undefined);
    return () => { active = false; };
  }, [adapter]);
  return rows;
}
