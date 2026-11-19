"use client";

import {
  resolveStoryUiLocale,
  STORY_REVIEW_LABELS_BY_LOCALE,
  STORY_STATUS_LABELS_BY_LOCALE,
  type StoryUiLocale,
} from "./story-management-copy";

function lifecycleBadgeClass(status: string): string {
  if (status === "Active") return "bg-emerald-50 text-emerald-700";
  if (status === "Scheduled") return "bg-sky-50 text-sky-700";
  if (status === "Disabled" || status === "Expired") return "bg-rose-50 text-rose-700";
  return "bg-amber-50 text-amber-700";
}

function reviewBadgeClass(reviewStatus: string): string {
  if (reviewStatus === "Approved") return "bg-emerald-50 text-emerald-700";
  if (reviewStatus === "Submitted") return "bg-sky-50 text-sky-700";
  if (reviewStatus === "Rejected") return "bg-rose-50 text-rose-700";
  return "bg-gray-50 text-gray-600";
}

/** نشان وضعیت چرخهٔ عمر استوری — همان زبان بصری Admin قبلی. */
export function StoryStatusBadge({
  status,
  locale,
}: {
  status: string;
  locale?: StoryUiLocale;
}) {
  const ui = locale ?? resolveStoryUiLocale();
  const labels = STORY_STATUS_LABELS_BY_LOCALE[ui];
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${lifecycleBadgeClass(status)}`}>
      {labels[status] ?? status}
    </span>
  );
}

/** نشان وضعیت بازبینی استوری. */
export function StoryReviewBadge({
  reviewStatus,
  locale,
}: {
  reviewStatus: string;
  locale?: StoryUiLocale;
}) {
  const ui = locale ?? resolveStoryUiLocale();
  const labels = STORY_REVIEW_LABELS_BY_LOCALE[ui];
  if (!reviewStatus || reviewStatus === "None") {
    return <span className="text-xs text-muted">{labels.None}</span>;
  }
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${reviewBadgeClass(reviewStatus)}`}>
      {labels[reviewStatus] ?? reviewStatus}
    </span>
  );
}
