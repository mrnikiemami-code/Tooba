"use client";

import { useState } from "react";
import {
  articleReadinessCheckLabel,
  type ArticlePublicationReadiness,
} from "./content-article-publication-model.ts";

/** خلاصهٔ فشردهٔ آمادگی انتشار در هدر workspace. */
export function ContentArticleReadinessSummary({
  readiness,
  locale,
  onNavigate,
}: {
  readiness: ArticlePublicationReadiness | null;
  locale: string;
  onNavigate: (tab: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const fa = locale.trim().toLowerCase().startsWith("fa");
  if (!readiness) {
    return (
      <span
        className="rounded-full border px-2.5 py-0.5 text-xs text-muted"
        data-testid="content-article-readiness-summary"
      >
        {fa ? "آمادگی…" : "Readiness…"}
      </span>
    );
  }

  const mandatoryCount = readiness.requiredMissing.length;
  const ready = readiness.canPublish;
  const title = ready
    ? fa
      ? "آماده انتشار"
      : "Ready to publish"
    : fa
      ? `نیازمند تکمیل (${mandatoryCount})`
      : `Needs completion (${mandatoryCount})`;

  return (
    <div className="relative" data-testid="content-article-readiness-summary">
      <button
        type="button"
        className={
          ready
            ? "rounded-full border border-emerald-300 bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-900"
            : "rounded-full border border-amber-300 bg-amber-50 px-2.5 py-0.5 text-xs font-semibold text-amber-950"
        }
        data-testid="content-article-readiness-toggle"
        onClick={() => setOpen((v) => !v)}
      >
        {title}
      </button>
      {open ? (
        <div
          className="absolute z-20 mt-2 w-80 max-w-[90vw] rounded-xl border bg-white p-3 shadow-lg"
          data-testid="content-article-readiness-checklist"
          dir={fa ? "rtl" : "ltr"}
        >
          <p className="mb-2 text-xs text-muted">
            {fa
              ? "موارد الزامی مانع انتشارند؛ توصیه‌ها اختیاری‌اند."
              : "Required items block publish; recommendations are optional."}
          </p>
          <ul className="max-h-64 space-y-2 overflow-auto text-sm">
            {readiness.checks.map((check) => (
              <li key={check.key} className="flex items-start justify-between gap-2">
                <button
                  type="button"
                  className={
                    check.satisfied
                      ? "text-left text-muted line-through"
                      : check.required
                        ? "text-left font-medium text-danger"
                        : "text-left text-amber-800"
                  }
                  data-testid={`content-article-readiness-item-${check.key}`}
                  onClick={() => {
                    if (check.actionTarget) onNavigate(check.actionTarget);
                    setOpen(false);
                  }}
                >
                  {articleReadinessCheckLabel(check, locale)}
                  {!check.required && !check.satisfied ? (
                    <span className="ms-1 text-[10px] font-normal text-muted">
                      ({fa ? "توصیه" : "recommended"})
                    </span>
                  ) : null}
                </button>
                <span className="shrink-0 text-[10px] text-muted">
                  {check.satisfied ? (fa ? "انجام" : "ok") : fa ? "ناقص" : "missing"}
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}
