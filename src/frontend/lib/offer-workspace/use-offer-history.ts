"use client";

import { useEffect, useState } from "react";
import type { OfferHistoryPage, OfferTabId, OfferWorkspaceAdapter, OfferWorkspaceDetail } from "./types.ts";

export function useOfferHistory(
  adapter: OfferWorkspaceAdapter,
  isEdit: boolean,
  activeTab: OfferTabId,
  detail: OfferWorkspaceDetail | null,
) {
  const [data, setData] = useState<OfferHistoryPage | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isEdit || activeTab !== "history" || !detail || !adapter.loadHistory) return;
    let active = true;
    setLoading(true);
    setError(null);
    setData(null);
    void adapter.loadHistory(page, 20).then((result) => {
      if (active) setData(result);
    }).catch(() => {
      if (active) setError("history");
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [activeTab, adapter, detail, isEdit, page]);

  return { data, loading, error, page, setPage };
}
