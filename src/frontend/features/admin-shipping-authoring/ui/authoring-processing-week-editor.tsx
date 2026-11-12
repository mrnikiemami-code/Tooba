"use client";

/**
 * TB-264-R1: the ONE method-level WAREHOUSE-PROCESSING calendar, mode-aware.
 *
 * It is a schedule of its own: the operator configures an independent time zone and seven days with the
 * warehouse's own open/close bounds. Preparation minutes are counted inside these hours only, so this
 * calendar is what turns «ساعات کاری انبار» into a provable duration. Nothing is derived from the
 * customer delivery week or the carrier acceptance week; the only convenience is an explicit ONE-TIME
 * copy that fills ordinary local values, after which the three calendars are fully independent.
 */
import type { EditorProcessingSchedule } from "../model/editor-document.ts";
import {
  type ProcessingIssue,
  type ProcessingIssueCode,
  type ProcessingWeekFormState,
} from "../model/editor-processing-week.ts";
import { WEEK_DISPLAY_ORDER } from "../model/editor-week.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { weekdayLabel } from "./authoring-general-tab.tsx";
import { TimeBoundInput, TimeZoneSelect, formatDayRange } from "./authoring-week-editor.tsx";

/** Localized text of one processing issue (shared by the editor and the screen's issue summary). */
export function processingIssueMessage(code: ProcessingIssueCode, messages: AuthoringMessages): string {
  switch (code) {
    case "timeZoneRequired":
      return messages.weekTimeZoneRequired;
    case "timeZoneTooLong":
      return messages.weekTimeZoneTooLong;
    case "dayHoursRequired":
      return messages.processingDayHoursRequired;
    case "dayTimeInvalid":
      return messages.processingDayTimeInvalid;
    case "dayRangeInvalid":
      return messages.processingDayRangeInvalid;
    case "pendingTimeZoneUnapplied":
      return messages.processingPendingTimeZoneUnapplied;
    case "workingWeekRequiresProcessingWeek":
      return messages.workingWeekRequiresProcessingWeek;
  }
}

export function AuthoringProcessingWeekEditor({
  schedule,
  processing,
  issues,
  editable,
  locale,
  messages,
  customerConfigured,
  serverErrors,
  onPendingTimeZone,
  onConfigure,
  onTimeZone,
  onDayWorking,
  onDayTime,
  onCopyFromCustomer,
  onReset,
}: {
  schedule: EditorProcessingSchedule | null;
  processing: ProcessingWeekFormState;
  issues: readonly ProcessingIssue[];
  editable: boolean;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  /** The customer week is configured: the one-time copy action is offered (it still refuses problems). */
  customerConfigured: boolean;
  serverErrors?: { readonly timeZone?: string; readonly days?: string };
  onPendingTimeZone: (value: string) => void;
  onConfigure: () => void;
  onTimeZone: (value: string) => void;
  onDayWorking: (weekday: number, isWorking: boolean) => void;
  onDayTime: (weekday: number, bound: "opensAt" | "closesAt", value: string) => void;
  onCopyFromCustomer: () => void;
  onReset: () => void;
}) {
  const timeZoneIssue = issues.find((issue) => issue.code === "timeZoneRequired" || issue.code === "timeZoneTooLong");
  const configureTimeZone = processing.kind === "unconfigured" ? processing.pendingTimeZone : "";

  return (
    <section
      className="rounded-xl border border-border bg-surface-elevated p-3 md:p-4"
      data-testid="shipping-authoring-processing-week"
      tabIndex={-1}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-base font-black text-gray-900">{messages.processingWeekTitle}</h3>
        {editable && processing.kind === "configured" && schedule !== null ? (
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-lg px-2 text-[11px] font-semibold text-gray-500 underline-offset-2 hover:underline"
            data-testid="shipping-authoring-processing-reset"
            onClick={onReset}
          >
            {messages.weekResetAction}
          </button>
        ) : null}
      </div>

      <p className="mt-1 text-[11px] text-gray-500">{messages.processingHint}</p>

      {processing.kind === "unconfigured" ? (
        <div className="mt-2 grid gap-2 text-xs text-gray-700">
          <p data-testid="shipping-authoring-processing-unconfigured">{messages.processingWeekUnconfigured}</p>
          {editable ? (
            <div className="flex flex-wrap items-end gap-2">
              <label className="grid w-full gap-0.5 md:w-72">
                <span className="text-xs font-bold text-gray-700">{messages.weekTimeZone}</span>
                <TimeZoneSelect
                  value={configureTimeZone}
                  locale={locale}
                  messages={messages}
                  testId="shipping-authoring-processing-configure-timezone"
                  onChange={onPendingTimeZone}
                />
              </label>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-processing-configure"
                disabled={configureTimeZone.trim().length === 0}
                onClick={onConfigure}
              >
                {messages.processingConfigureAction}
              </button>
              {configureTimeZone.length > 0 ? (
                <button
                  type="button"
                  className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800"
                  data-testid="shipping-authoring-processing-clear-pending"
                  onClick={onReset}
                >
                  {messages.weekResetAction}
                </button>
              ) : null}
            </div>
          ) : null}
          {editable && configureTimeZone.trim().length > 0 ? (
            <p
              role="alert"
              className="text-[11px] font-semibold text-danger"
              data-testid="shipping-authoring-processing-pending"
            >
              {messages.processingPendingTimeZoneUnapplied}
            </p>
          ) : null}
          {editable ? (
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-processing-copy"
                disabled={!customerConfigured}
                onClick={onCopyFromCustomer}
              >
                {messages.processingCopyAction}
              </button>
              <span className="text-[11px] text-gray-500">
                {customerConfigured ? messages.processingCopyHint : messages.processingCopyUnavailable}
              </span>
            </div>
          ) : null}
        </div>
      ) : (
        <div className="mt-2 grid gap-2 text-xs text-gray-700">
          {editable ? (
            <div className="flex flex-wrap items-end gap-2">
              <label className="grid w-full gap-0.5 md:w-72">
                <span className="text-xs font-bold text-gray-700">{messages.weekTimeZone}</span>
                <TimeZoneSelect
                  value={processing.timeZoneId}
                  locale={locale}
                  messages={messages}
                  testId="shipping-authoring-processing-timezone"
                  onChange={onTimeZone}
                />
              </label>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-processing-copy"
                disabled={!customerConfigured}
                onClick={onCopyFromCustomer}
              >
                {messages.processingCopyAction}
              </button>
              <span className="max-w-xs text-[11px] text-gray-500">
                {customerConfigured ? messages.processingCopyHint : messages.processingCopyUnavailable}
              </span>
            </div>
          ) : (
            <p>
              <span className="font-bold">{messages.weekTimeZone}: </span>
              <span dir="ltr">{processing.timeZoneId}</span>
            </p>
          )}
          {timeZoneIssue ? (
            <p role="alert" className="text-[11px] font-semibold text-danger">
              {processingIssueMessage(timeZoneIssue.code, messages)}
            </p>
          ) : null}
          {serverErrors?.timeZone ? (
            <p
              role="alert"
              className="text-[11px] font-semibold text-danger"
              data-testid="shipping-authoring-processing-timezone-error"
            >
              {serverErrors.timeZone}
            </p>
          ) : null}

          <ul
            className="mt-1 divide-y divide-border rounded-xl border border-border bg-surface"
            data-testid="shipping-authoring-processing-days"
          >
            {WEEK_DISPLAY_ORDER.map((weekday) => {
              const day = processing.days.find((candidate) => candidate.weekday === weekday);
              if (!day) return null;
              const dayIssue = issues.find((issue) => issue.weekday === weekday);
              return (
                <li
                  key={weekday}
                  className="flex min-h-14 flex-wrap items-center gap-x-3 gap-y-2 px-3 py-1.5"
                  data-testid={`shipping-authoring-processing-day-${weekday}`}
                >
                  <span className="w-20 shrink-0 text-sm font-bold text-gray-900">{weekdayLabel(weekday, locale)}</span>
                  {editable ? (
                    <div
                      className="inline-flex rounded-lg border border-border p-0.5"
                      role="group"
                      aria-label={messages.weekStatusColumn}
                    >
                      <button
                        type="button"
                        className={
                          day.isWorking
                            ? "inline-flex min-h-11 items-center rounded-md bg-primary px-3 text-xs font-bold text-primary-foreground"
                            : "inline-flex min-h-11 items-center rounded-md px-3 text-xs font-bold text-gray-600"
                        }
                        aria-pressed={day.isWorking}
                        data-testid={`shipping-authoring-processing-working-${weekday}`}
                        onClick={() => onDayWorking(weekday, true)}
                      >
                        {messages.processingWorking}
                      </button>
                      <button
                        type="button"
                        className={
                          !day.isWorking
                            ? "inline-flex min-h-11 items-center rounded-md bg-gray-800 px-3 text-xs font-bold text-white"
                            : "inline-flex min-h-11 items-center rounded-md px-3 text-xs font-bold text-gray-600"
                        }
                        aria-pressed={!day.isWorking}
                        data-testid={`shipping-authoring-processing-closed-${weekday}`}
                        onClick={() => onDayWorking(weekday, false)}
                      >
                        {messages.processingNotWorking}
                      </button>
                    </div>
                  ) : (
                    <span className="text-xs font-semibold text-gray-700">
                      {day.isWorking ? messages.processingWorking : messages.processingNotWorking}
                    </span>
                  )}
                  {day.isWorking ? (
                    editable ? (
                      <div className="flex flex-wrap items-end gap-2">
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.processingOpenColumn}</span>
                          <TimeBoundInput
                            value={day.opensAt}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-processing-open-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "opensAt", next)}
                          />
                        </label>
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.processingCloseColumn}</span>
                          <TimeBoundInput
                            value={day.closesAt}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-processing-close-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "closesAt", next)}
                          />
                        </label>
                      </div>
                    ) : (
                      <p
                        className="text-sm font-semibold text-gray-800"
                        data-testid={`shipping-authoring-processing-range-${weekday}`}
                      >
                        {formatDayRange(day.opensAt, day.closesAt, locale)}
                      </p>
                    )
                  ) : null}
                  {dayIssue ? (
                    <p role="alert" className="w-full text-xs font-semibold text-danger">
                      {processingIssueMessage(dayIssue.code, messages)}
                    </p>
                  ) : null}
                </li>
              );
            })}
          </ul>
          {serverErrors?.days ? (
            <p
              role="alert"
              className="text-[11px] font-semibold text-danger"
              data-testid="shipping-authoring-processing-days-error"
            >
              {serverErrors.days}
            </p>
          ) : null}
        </div>
      )}
    </section>
  );
}
