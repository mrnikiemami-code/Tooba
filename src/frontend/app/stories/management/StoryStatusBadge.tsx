"use client";

import { STORY_REVIEW_LABELS, STORY_STATUS_LABELS } from "./story-management-copy";

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
export function StoryStatusBadge({ status }: { status: string }) {
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${lifecycleBadgeClass(status)}`}>
      {STORY_STATUS_LABELS[status] ?? status}
    </span>
  );
}

/** نشان وضعیت بازبینی استوری. */
export function StoryReviewBadge({ reviewStatus }: { reviewStatus: string }) {
  if (!reviewStatus || reviewStatus === "None") {
    return <span className="text-xs text-muted">—</span>;
  }
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${reviewBadgeClass(reviewStatus)}`}>
      {STORY_REVIEW_LABELS[reviewStatus] ?? reviewStatus}
    </span>
  );
}
