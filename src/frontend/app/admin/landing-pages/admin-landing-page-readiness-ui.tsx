"use client";

import { AlertTriangle, CheckCircle2, XCircle } from "lucide-react";
import {
  landingCheckDescription,
  landingCheckTitle,
  type LandingPublishCheck,
  type LandingPublishReadiness,
} from "./admin-landing-page-readiness.ts";

export function LandingPublishReadinessCard({
  readiness,
  locale,
  onOpenChecklist,
}: {
  readiness: LandingPublishReadiness;
  locale: string;
  onOpenChecklist: () => void;
}) {
  const fa = !locale.trim().toLowerCase().startsWith("en");
  const complete = readiness.canPublish;
  const tone = complete ? "success" : "warning";
  const valueTone = tone === "success" ? "text-emerald-700" : "text-amber-800";
  const barTone = tone === "success" ? "bg-emerald-500" : "bg-amber-500";
  const progress = readiness.totalCount > 0 ? readiness.readyCount / readiness.totalCount : 0;

  return (
    <div
      className="min-w-[10.5rem] rounded-2xl border border-border bg-surface-elevated p-3.5 shadow-sm"
      data-testid="landing-publish-readiness-card"
    >
      <p className="text-xs font-medium text-muted">{fa ? "آمادگی انتشار" : "Publish readiness"}</p>
      <p className={`mt-1.5 text-lg font-semibold leading-snug ${valueTone}`}>
        {`${readiness.readyCount.toLocaleString(fa ? "fa-IR" : "en-US")} ${fa ? "از" : "of"} ${readiness.totalCount.toLocaleString(fa ? "fa-IR" : "en-US")}`}
      </p>
      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-secondary" aria-hidden>
        <div className={`h-full rounded-full ${barTone}`} style={{ width: `${Math.round(progress * 100)}%` }} />
      </div>
      <p className="mt-1.5 text-xs text-muted">
        {complete
          ? fa
            ? "آماده انتشار"
            : "Ready to publish"
          : fa
            ? `${readiness.missing.length.toLocaleString("fa-IR")} مورد باقی‌مانده`
            : `${readiness.missing.length} remaining`}
      </p>
      <button
        type="button"
        className="mt-2 text-xs font-semibold text-primary underline-offset-2 hover:underline"
        data-testid="landing-view-checklist"
        onClick={onOpenChecklist}
      >
        {fa ? "مشاهده چک‌لیست" : "View checklist"}
      </button>
    </div>
  );
}

export function LandingPublishIssuesModal({
  open,
  locale,
  title,
  description,
  issues,
  onClose,
  primaryLabel,
  onPrimary,
  secondaryLabel,
}: {
  open: boolean;
  locale: string;
  title: string;
  description?: string;
  issues: LandingPublishCheck[];
  onClose: () => void;
  primaryLabel?: string;
  onPrimary?: () => void;
  secondaryLabel?: string;
}) {
  const fa = !locale.trim().toLowerCase().startsWith("en");
  if (!open) return null;
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      data-testid="landing-publish-issues-modal"
      role="dialog"
      aria-modal="true"
      aria-labelledby="landing-publish-issues-title"
    >
      <div className="w-full max-w-lg rounded-2xl bg-white p-5 shadow-xl" dir={fa ? "rtl" : "ltr"}>
        <div className="flex items-start gap-3">
          <span className="mt-0.5 rounded-full bg-amber-50 p-2 text-amber-700">
            <AlertTriangle className="h-5 w-5" />
          </span>
          <div className="min-w-0 flex-1">
            <h3 id="landing-publish-issues-title" className="text-lg font-black">
              {title}
            </h3>
            {description ? <p className="mt-1 text-sm text-muted">{description}</p> : null}
          </div>
        </div>
        <ul className="mt-4 max-h-72 space-y-3 overflow-auto" data-testid="landing-publish-issues-list">
          {issues.map((issue) => (
            <li
              key={issue.key}
              className="flex items-start gap-3 rounded-xl border border-amber-100 bg-amber-50/60 px-3 py-2.5"
              data-testid={`landing-issue-${issue.key}`}
            >
              <XCircle className="mt-0.5 h-4 w-4 shrink-0 text-amber-700" />
              <div className="min-w-0">
                <p className="text-sm font-bold">{landingCheckTitle(issue, locale)}</p>
                <p className="mt-0.5 text-xs text-muted">{landingCheckDescription(issue, locale)}</p>
              </div>
            </li>
          ))}
        </ul>
        <div className="mt-4 flex flex-wrap justify-end gap-2">
          <button
            type="button"
            className="rounded-xl border px-4 py-2 text-sm font-bold"
            data-testid="landing-publish-issues-close"
            onClick={onClose}
          >
            {secondaryLabel ?? (fa ? "بستن" : "Close")}
          </button>
          {primaryLabel && onPrimary ? (
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              data-testid="landing-publish-issues-primary"
              onClick={onPrimary}
            >
              {primaryLabel}
            </button>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export function LandingChecklistModal({
  open,
  locale,
  readiness,
  onClose,
  onNavigate,
}: {
  open: boolean;
  locale: string;
  readiness: LandingPublishReadiness;
  onClose: () => void;
  onNavigate: (group: "page" | "seo" | "sections") => void;
}) {
  const fa = !locale.trim().toLowerCase().startsWith("en");
  if (!open) return null;
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      data-testid="landing-checklist-modal"
      role="dialog"
      aria-modal="true"
    >
      <div className="w-full max-w-lg rounded-2xl bg-white p-5 shadow-xl" dir={fa ? "rtl" : "ltr"}>
        <h3 className="text-lg font-black">{fa ? "چک‌لیست آمادگی انتشار" : "Publish checklist"}</h3>
        <p className="mt-1 text-sm text-muted">
          {fa
            ? "موارد الزامی مانع انتشارند؛ پس از تکمیل می‌توانید منتشر کنید."
            : "Required items block publishing until they are completed."}
        </p>
        <ul className="mt-4 max-h-80 space-y-2 overflow-auto">
          {readiness.checks.map((item) => (
            <li key={item.key}>
              <button
                type="button"
                className="flex w-full items-start gap-3 rounded-xl border px-3 py-2.5 text-start hover:bg-slate-50"
                data-testid={`landing-checklist-item-${item.key}`}
                onClick={() => {
                  if (item.actionTarget) onNavigate(item.actionTarget);
                  onClose();
                }}
              >
                {item.satisfied ? (
                  <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-emerald-600" />
                ) : (
                  <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
                )}
                <span className="min-w-0">
                  <span className={`block text-sm font-bold ${item.satisfied ? "text-muted line-through" : ""}`}>
                    {landingCheckTitle(item, locale)}
                  </span>
                  {!item.satisfied ? (
                    <span className="mt-0.5 block text-xs text-muted">{landingCheckDescription(item, locale)}</span>
                  ) : null}
                </span>
              </button>
            </li>
          ))}
        </ul>
        <div className="mt-4 flex justify-end">
          <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" onClick={onClose}>
            {fa ? "بستن" : "Close"}
          </button>
        </div>
      </div>
    </div>
  );
}
