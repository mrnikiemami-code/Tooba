"use client";

import { formatJalaliDateTime } from "../../design-system";
import type { ArticleHistoryEntry } from "./content-article-publication-model.ts";

/** تایم‌لاین انسانی تاریخچهٔ چرخهٔ عمر — بدون کلید خام رویداد. */
export function ContentArticleHistoryTimeline({
  entries,
  locale,
}: {
  entries: ArticleHistoryEntry[];
  locale: string;
}) {
  const fa = locale.trim().toLowerCase().startsWith("fa");
  if (entries.length === 0) {
    return (
      <p className="text-sm text-muted" data-testid="content-article-history-empty">
        {fa ? "هنوز رویدادی ثبت نشده است." : "No history events yet."}
      </p>
    );
  }

  return (
    <ol className="space-y-3" data-testid="content-article-history-timeline">
      {entries.map((entry) => {
        const label = fa ? entry.eventLabelFa : entry.eventLabelEn;
        const summary = fa ? entry.summaryFa : entry.summaryEn;
        const when = formatJalaliDateTime(entry.occurredAt, fa ? "fa" : "en");
        const actor = entry.actorDisplayName || (fa ? "سیستم" : "System");
        const transition =
          entry.previousState && entry.newState
            ? `${entry.previousState} → ${entry.newState}`
            : null;
        return (
          <li
            key={entry.historyId}
            className="rounded-xl border p-3 text-sm"
            data-testid={`content-article-history-event-${entry.eventType}`}
          >
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="font-semibold">{label}</span>
              <span className="text-xs text-muted" dir="ltr">
                {when}
              </span>
            </div>
            <p className="mt-1 text-muted">{summary}</p>
            <div className="mt-2 flex flex-wrap gap-3 text-xs text-muted">
              <span>
                {fa ? "بازیگر:" : "Actor:"} {actor}
              </span>
              {transition ? <span>{transition}</span> : null}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
