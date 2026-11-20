"use client";

import { JalaliDatePicker } from "../../../design-system/app-data-grid/jalali-date-picker";
import type { StoryCopy } from "./story-management-copy";
import type { StoryScheduleDraft } from "./story-schedule-time";

/** بازهٔ انتشار مشترک — «بدون پایان» کنار برچسب پایان؛ ستون‌ها هم‌ارتفاع. */
export function StoryPublicationWindowFields({
  testIdPrefix,
  copy,
  draftWindow,
  noPublicationEnd,
  error,
  onDraftChange,
  onNoEndChange,
}: {
  testIdPrefix: "story-create-window" | "story-edit-window";
  copy: StoryCopy;
  draftWindow: StoryScheduleDraft;
  noPublicationEnd: boolean;
  error: string | null;
  onDraftChange: (next: StoryScheduleDraft) => void;
  onNoEndChange: (noEnd: boolean) => void;
}) {
  const startDayId = `${testIdPrefix}-start-day-label`;
  const startTimeId = `${testIdPrefix}-start-time`;
  const endDayId = `${testIdPrefix}-end-day-label`;
  const endTimeId = `${testIdPrefix}-end-time`;
  const noEndId = `${testIdPrefix}-no-end`;
  const endDisabled = noPublicationEnd;

  return (
    <fieldset
      className="space-y-4 rounded-xl border border-border bg-background/40 p-4"
      data-testid={testIdPrefix === "story-create-window" ? "story-create-publication-window" : "story-edit-publication-window"}
    >
      <legend className="float-none mb-1 w-full px-0">
        <span className="text-sm font-bold text-foreground">{copy.publicationWindowHeading}</span>
      </legend>
      <div className="grid gap-4 md:grid-cols-2">
        <div className="space-y-2 text-sm">
          <div className="flex min-h-11 items-center">
            <p id={startDayId} className="text-muted">
              {copy.publicationStartLabel}
            </p>
          </div>
          <div data-testid={`${testIdPrefix}-start-day`}>
            <JalaliDatePicker
              ariaLabel={copy.publicationStartLabel}
              value={draftWindow.startDayIso || undefined}
              onChange={(iso) => onDraftChange({ ...draftWindow, startDayIso: iso ?? "" })}
            />
          </div>
          <input
            id={startTimeId}
            type="time"
            className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
            value={draftWindow.startTime}
            onChange={(e) => onDraftChange({ ...draftWindow, startTime: e.target.value })}
            data-testid={`${testIdPrefix}-start-time`}
            aria-labelledby={startDayId}
          />
        </div>
        <div className={`space-y-2 text-sm ${endDisabled ? "opacity-55" : ""}`}>
          <div className="flex min-h-11 flex-wrap items-center gap-2" data-testid={`${testIdPrefix}-end-header`}>
            <p id={endDayId} className="text-muted">
              {copy.publicationEndLabel}
            </p>
            <span className="inline-flex items-center gap-2 text-sm font-normal text-foreground">
              <input
                id={noEndId}
                type="checkbox"
                className="h-4 w-4"
                checked={noPublicationEnd}
                onChange={(e) => onNoEndChange(e.target.checked)}
                data-testid={noEndId}
              />
              <label htmlFor={noEndId} className="cursor-pointer select-none">
                {copy.publicationNoEndToggle}
              </label>
            </span>
          </div>
          <div data-testid={`${testIdPrefix}-end-day`} className={endDisabled ? "pointer-events-none" : undefined}>
            <JalaliDatePicker
              ariaLabel={copy.publicationEndLabel}
              value={draftWindow.endDayIso || undefined}
              onChange={(iso) => {
                if (endDisabled) return;
                onDraftChange({ ...draftWindow, endDayIso: iso ?? "" });
              }}
            />
          </div>
          <input
            id={endTimeId}
            type="time"
            className="min-h-11 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm disabled:cursor-not-allowed disabled:bg-secondary"
            value={draftWindow.endTime}
            disabled={endDisabled}
            onChange={(e) => onDraftChange({ ...draftWindow, endTime: e.target.value })}
            data-testid={`${testIdPrefix}-end-time`}
            aria-labelledby={endDayId}
            aria-disabled={endDisabled}
          />
        </div>
      </div>
      {error ? <p className="text-sm text-danger">{error}</p> : null}
    </fieldset>
  );
}
