"use client";

import { useEffect, useState } from "react";
import type { OfferCurrencyOption, OfferWorkspaceAdapter } from "./types";

type CurrencyStatus = "legacy" | "loading" | "ready" | "unavailable";

/** The existing shared editor reads the Pricing reference; failure never invents a list. */
export function useOfferCurrencies(adapter: OfferWorkspaceAdapter) {
  const [rows, setRows] = useState<OfferCurrencyOption[]>([]);
  const [status, setStatus] = useState<CurrencyStatus>(adapter.loadCurrencies ? "loading" : "legacy");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    setRows([]);
    if (!adapter.loadCurrencies) {
      setStatus("legacy");
      return;
    }
    setStatus("loading");
    void adapter.loadCurrencies()
      .then(next => { if (active) { setRows(next); setStatus(next.length ? "ready" : "unavailable"); } })
      .catch(() => { if (active) setStatus("unavailable"); });
    return () => { active = false; };
  }, [adapter, attempt]);
  return { rows, status, retry: () => setAttempt(value => value + 1) };
}
