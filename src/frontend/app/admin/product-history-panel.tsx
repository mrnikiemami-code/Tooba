"use client";

import { useCallback, useEffect, useState } from "react";
import { getAdminProductHistory } from "./host-client.ts";
import {
  SECTION_FILTER_OPTIONS,
  formatHistoryTimestamp,
  historyHasBeforeAfter,
  historyPrimaryLabel,
  type ProductHistoryEntry,
} from "./product-history-panel-model.ts";

const PAGE_SIZE = 50;

/**
 * تب تاریخچه Workspace — فقط‌خواندنی؛ timeline از endpoint اختصاصی history.
 */
export function ProductHistoryPanel({
  productId,
  viewScope = false,
}: {
  productId: string;
  viewScope?: boolean;
}) {
  const [section, setSection] = useState("");
  const [items, setItems] = useState<ProductHistoryEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPage = useCallback(
    async (opts: { skip: number; append: boolean; sectionFilter: string }) => {
      if (opts.append) {
        setLoadingMore(true);
      } else {
        setLoading(true);
      }
      setError(null);
      const result = await getAdminProductHistory(productId, {
        skip: opts.skip,
        take: PAGE_SIZE,
        section: opts.sectionFilter || undefined,
        viewScope,
      });
      if (opts.append) {
        setLoadingMore(false);
      } else {
        setLoading(false);
      }
      if (!result.ok) {
        setError(result.message);
        if (!opts.append) {
          setItems([]);
          setTotalCount(0);
        }
        return;
      }
      setTotalCount(result.page.totalCount);
      setItems((prev) => (opts.append ? [...prev, ...result.page.items] : result.page.items));
    },
    [productId, viewScope],
  );

  useEffect(() => {
    void loadPage({ skip: 0, append: false, sectionFilter: section });
  }, [loadPage, section]);

  function onSectionChange(next: string) {
    setSection(next);
  }

  function onLoadMore() {
    if (loading || loadingMore || items.length >= totalCount) return;
    void loadPage({ skip: items.length, append: true, sectionFilter: section });
  }

  const busy = loading || loadingMore;
  const empty = !loading && !error && items.length === 0;
  const canLoadMore = !loading && !error && totalCount > items.length;

  return (
    <div className="space-y-4" data-testid="product-history-panel" aria-busy={busy || undefined}>
      <div className="flex flex-wrap items-end gap-3">
        <label className="flex min-w-[12rem] flex-1 flex-col gap-1 text-sm">
          <span className="text-muted">فیلتر بخش</span>
          <select
            className="min-h-11 rounded-ds border border-border bg-surface px-3"
            value={section}
            onChange={(e) => onSectionChange(e.target.value)}
            disabled={loading}
            aria-label="فیلتر بخش تاریخچه"
          >
            {SECTION_FILTER_OPTIONS.map((opt) => (
              <option key={opt.value || "all"} value={opt.value}>
                {opt.labelFa}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error ? (
        <p className="rounded-ds border border-danger/40 bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}

      {loading ? <p className="text-sm text-muted">در حال بارگذاری تاریخچه…</p> : null}

      {empty ? <p className="text-sm text-muted">تاریخچه‌ای ثبت نشده است.</p> : null}

      {!loading && items.length > 0 ? (
        <ol className="space-y-3 border-s-2 border-border ps-4">
          {items.map((entry) => (
            <li key={entry.historyId} className="space-y-1">
              <p className="font-medium">{historyPrimaryLabel(entry)}</p>
              <p className="text-sm text-muted">
                {entry.actorDisplayName}
                {" · "}
                {formatHistoryTimestamp(entry.occurredAt)}
                {entry.sectionLabelFa ? ` · ${entry.sectionLabelFa}` : null}
              </p>
              {historyHasBeforeAfter(entry) ? (
                <p className="text-sm text-muted">
                  {entry.beforeSummary?.trim() || "—"}
                  {" → "}
                  {entry.afterSummary?.trim() || "—"}
                </p>
              ) : null}
            </li>
          ))}
        </ol>
      ) : null}

      {canLoadMore ? (
        <button
          type="button"
          className="min-h-11 rounded-ds border border-border px-4 text-sm hover:bg-secondary disabled:opacity-50"
          onClick={onLoadMore}
          disabled={loadingMore}
        >
          {loadingMore ? "در حال بارگذاری…" : "بارگذاری بیشتر"}
        </button>
      ) : null}
    </div>
  );
}
