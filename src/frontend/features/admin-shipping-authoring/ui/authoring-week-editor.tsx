"use client";

/**
 * The ONE method-level weekly schedule, mode-aware: read-only in view, editable in edit.
 *
 * An unconfigured schedule is NOT auto-created: it stays explicitly unconfigured until the operator
 * supplies a time zone and deliberately configures it. Working/closed toggles keep the remembered
 * hours, a working day requires a real start/end pair, and an untouched stored `TimeText` (including
 * its seconds/fractions) is never rewritten by simply rendering this component.
 */
import { useEffect, useState, type CSSProperties } from "react";
import type { EditorWeeklySchedule } from "../model/editor-document.ts";
import type { WeekFormState, WeekIssue } from "../model/editor-week.ts";
import { WEEK_DISPLAY_ORDER, type WeekIssueCode } from "../model/editor-week.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { formatTimeText } from "./authoring-format.ts";
import { weekdayLabel } from "./authoring-general-tab.tsx";
import { TIME_ZONE_OPTIONS, isOfferedTimeZone, timeZoneLabel } from "./time-zone-options.ts";

/**
 * The ONE column contract of the seven-day grid. Header and every day row use the same template, so
 * day names, working controls and start/end inputs each share one x position and width.
 */
const WEEK_COLUMNS_DESKTOP = "minmax(6rem, 1fr) 8.5rem 7.5rem 7.5rem";
/** Narrow viewport: day + status on the first line, from/to together on the second. */
const WEEK_COLUMNS_NARROW = "minmax(0, 1fr) auto";

/** True when the week table may use its four desktop columns. */
function useWideWeekGrid(): boolean {
  const [wide, setWide] = useState(false);
  useEffect(() => {
    const query = window.matchMedia("(min-width: 640px)");
    const sync = () => setWide(query.matches);
    sync();
    query.addEventListener("change", sync);
    return () => query.removeEventListener("change", sync);
  }, []);
  return wide;
}

/**
 * The paired from/to bounds: on desktop they are two ordinary grid cells (columns 3 and 4), so their
 * x/width stay on the shared column contract; on narrow screens the pair becomes ONE equal-halves line.
 */
function boundPairStyle(wide: boolean): CSSProperties {
  return wide
    ? { display: "contents" }
    : { gridColumn: "1 / -1", display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)", columnGap: 8 };
}

/** Localized text of one week issue (shared by the editor and the screen's issue summary). */
export function weekIssueMessage(code: WeekIssueCode, messages: AuthoringMessages): string {
  switch (code) {
    case "timeZoneRequired":
      return messages.weekTimeZoneRequired;
    case "timeZoneTooLong":
      return messages.weekTimeZoneTooLong;
    case "dayHoursRequired":
      return messages.weekDayHoursRequired;
    case "dayTimeInvalid":
      return messages.weekDayTimeInvalid;
    case "dayRangeInvalid":
      return messages.weekDayRangeInvalid;
    case "pendingTimeZoneUnapplied":
      return messages.weekPendingTimeZoneUnapplied;
  }
}

/** `HH:mm` for a `time` input: an untouched stored token is shown truncated but never rewritten. */
function inputTime(value: string): string {
  const match = /^([01][0-9]|2[0-3]):([0-5][0-9])/.exec(value);
  return match ? `${match[1]}:${match[2]}` : value;
}

/**
 * Time-zone chooser: the operator picks a human-labelled entry whose VALUE is the real identifier the
 * server can resolve. A stored identifier outside this list is shown honestly as its own option and is
 * never rewritten or silently replaced.
 */
function TimeZoneSelect({
  value,
  locale,
  messages,
  testId,
  onChange,
}: {
  value: string;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  testId: string;
  onChange: (id: string) => void;
}) {
  const outsideList = value.trim().length > 0 && !isOfferedTimeZone(value.trim());
  return (
    <select
      dir="ltr"
      className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
      value={value}
      data-testid={testId}
      onChange={(event) => onChange(event.target.value)}
    >
      <option value="">—</option>
      {TIME_ZONE_OPTIONS.map((option) => (
        <option key={option.id} value={option.id}>
          {option.label[locale]} — {option.id}
        </option>
      ))}
      {outsideList ? (
        <option value={value}>
          {messages.weekTimeZoneOther} — {value}
        </option>
      ) : null}
    </select>
  );
}

export function AuthoringWeekEditor({
  schedule,
  week,
  issues,
  editable,
  locale,
  messages,
  serverErrors,
  onPendingTimeZone,
  onConfigure,
  onTimeZone,
  onDayWorking,
  onDayTime,
  onReset,
}: {
  schedule: EditorWeeklySchedule | null;
  week: WeekFormState;
  issues: readonly WeekIssue[];
  editable: boolean;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  /** Localized server reasons placed on the time zone or the day list of THIS section. */
  serverErrors?: { readonly timeZone?: string; readonly days?: string };
  onPendingTimeZone: (value: string) => void;
  onConfigure: () => void;
  onTimeZone: (value: string) => void;
  onDayWorking: (weekday: number, isWorking: boolean) => void;
  onDayTime: (weekday: number, bound: "startTime" | "endTime", value: string) => void;
  onReset: () => void;
}) {
  const timeZoneIssue = issues.find((issue) => issue.code === "timeZoneRequired" || issue.code === "timeZoneTooLong");
  const configureTimeZone = week.kind === "unconfigured" ? week.pendingTimeZone : "";
  const wide = useWideWeekGrid();

  return (
    <section
      className="rounded-xl border border-border bg-surface-elevated p-3 md:p-4"
      data-testid="shipping-authoring-week"
      tabIndex={-1}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-black text-gray-900">{messages.weekTitle}</h3>
        {editable && week.kind === "configured" && schedule !== null ? (
          <button
            type="button"
            className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
            data-testid="shipping-authoring-week-reset"
            onClick={onReset}
          >
            {messages.weekResetAction}
          </button>
        ) : null}
      </div>

      {week.kind === "unconfigured" ? (
        <div className="mt-2 grid gap-2 text-xs text-gray-700">
          <p>{messages.weekUnconfigured}</p>
          {editable ? (
            <div className="flex flex-wrap items-end gap-2">
              <label className="grid w-full gap-0.5 md:w-72">
                <span className="text-xs font-bold text-gray-700">{messages.weekTimeZone}</span>
                <TimeZoneSelect
                  value={configureTimeZone}
                  locale={locale}
                  messages={messages}
                  testId="shipping-authoring-week-configure-timezone"
                  onChange={onPendingTimeZone}
                />
              </label>
              <button
                type="button"
                className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-week-configure"
                disabled={configureTimeZone.trim().length === 0}
                onClick={onConfigure}
              >
                {messages.weekConfigureAction}
              </button>
              {configureTimeZone.length > 0 ? (
                <button
                  type="button"
                  className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
                  data-testid="shipping-authoring-week-clear-pending"
                  onClick={onReset}
                >
                  {messages.weekResetAction}
                </button>
              ) : null}
            </div>
          ) : null}
          {editable && configureTimeZone.trim().length > 0 ? (
            <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-week-pending">
              {messages.weekPendingTimeZoneUnapplied}
            </p>
          ) : null}
          {editable ? <p className="text-[11px] text-gray-500">{messages.weekTimeZoneHint}</p> : null}
        </div>
      ) : (
        <div className="mt-2 grid gap-2 text-xs text-gray-700">
          {editable ? (
            <label className="grid w-full gap-0.5 md:w-72">
              <span className="text-xs font-bold text-gray-700">{messages.weekTimeZone}</span>
              <TimeZoneSelect
                value={week.timeZoneId}
                locale={locale}
                messages={messages}
                testId="shipping-authoring-week-timezone"
                onChange={onTimeZone}
              />
            </label>
          ) : (
            <p>
              <span className="font-bold">{messages.weekTimeZone}: </span>
              <span>{timeZoneLabel(week.timeZoneId, locale) ?? week.timeZoneId}</span>{" "}
              <span dir="ltr" className="text-gray-500">
                {timeZoneLabel(week.timeZoneId, locale) ? week.timeZoneId : ""}
              </span>
            </p>
          )}
          {timeZoneIssue ? (
            <p role="alert" className="text-[11px] font-semibold text-danger">
              {weekIssueMessage(timeZoneIssue.code, messages)}
            </p>
          ) : null}
          {serverErrors?.timeZone ? (
            <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-week-timezone-error">
              {serverErrors.timeZone}
            </p>
          ) : null}

          {/* ONE column contract for all seven days: header and every row share the same template,
              so day labels, status controls and start/end inputs each line up on one x position. */}
          <div
            className="mt-1 hidden rounded-lg bg-surface px-2 py-1 text-xs font-bold text-gray-500 sm:grid sm:gap-2"
            style={{ gridTemplateColumns: WEEK_COLUMNS_DESKTOP }}
            data-testid="shipping-authoring-week-head"
            aria-hidden
          >
            <span>{messages.weekDayColumn}</span>
            <span>{messages.weekStatusColumn}</span>
            <span>{messages.weekFromColumn}</span>
            <span>{messages.weekToColumn}</span>
          </div>
          <ul
            className="grid gap-1 sm:gap-0 sm:divide-y sm:divide-border sm:rounded-lg sm:bg-surface"
            data-testid="shipping-authoring-week-days"
          >
            {WEEK_DISPLAY_ORDER.map((weekday) => {
              const day = week.days.find((candidate) => candidate.weekday === weekday);
              if (!day) return null;
              const dayIssue = issues.find((issue) => issue.weekday === weekday);
              return (
                <li
                  key={weekday}
                  className="grid items-center gap-x-2 gap-y-1 rounded-lg bg-surface px-2 py-1.5 sm:rounded-none sm:py-1"
                  style={{ gridTemplateColumns: wide ? WEEK_COLUMNS_DESKTOP : WEEK_COLUMNS_NARROW, columnGap: 8 }}
                  data-testid={`shipping-authoring-week-day-${weekday}`}
                >
                  <span className="text-sm font-semibold text-gray-900">{weekdayLabel(weekday, locale)}</span>
                  {editable ? (
                    <label className="flex items-center gap-1.5 text-xs text-gray-700">
                      <input
                        type="checkbox"
                        className="size-4 shrink-0"
                        checked={day.isWorking}
                        onChange={(event) => onDayWorking(weekday, event.target.checked)}
                        data-testid={`shipping-authoring-week-working-${weekday}`}
                      />
                      <span>{messages.weekWorking}</span>
                    </label>
                  ) : (
                    /* Truthful status column: working/closed as text, the bounds live in their own columns. */
                    <span className="text-xs font-semibold text-gray-700">
                      {day.isWorking ? messages.weekWorking : messages.weekClosed}
                    </span>
                  )}
                  {/* Paired bounds: on narrow screens they share ONE second line with equal widths and
                      visible From/To labels; on desktop they are the two existing header columns. */}
                  <div style={boundPairStyle(wide)}>
                    <label className="grid min-w-0 gap-0.5">
                      <span className="sr-only">{weekdayLabel(weekday, locale)}</span>
                      <span className={wide ? "sr-only" : "text-[11px] font-semibold text-gray-500"}>
                        {messages.weekFromColumn}
                      </span>
                      {editable ? (
                        <input
                          type="time"
                          dir="ltr"
                          className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs disabled:cursor-not-allowed disabled:opacity-50"
                          value={inputTime(day.startTime)}
                          disabled={!day.isWorking}
                          onChange={(event) => onDayTime(weekday, "startTime", event.target.value)}
                          data-testid={`shipping-authoring-week-start-${weekday}`}
                        />
                      ) : (
                        <span dir="ltr" className="text-xs text-gray-700">
                          {day.isWorking ? formatTimeText(day.startTime) : "—"}
                        </span>
                      )}
                    </label>
                    <label className="grid min-w-0 gap-0.5">
                      <span className="sr-only">{weekdayLabel(weekday, locale)}</span>
                      <span className={wide ? "sr-only" : "text-[11px] font-semibold text-gray-500"}>
                        {messages.weekToColumn}
                      </span>
                      {editable ? (
                        <input
                          type="time"
                          dir="ltr"
                          className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs disabled:cursor-not-allowed disabled:opacity-50"
                          value={inputTime(day.endTime)}
                          disabled={!day.isWorking}
                          onChange={(event) => onDayTime(weekday, "endTime", event.target.value)}
                          data-testid={`shipping-authoring-week-end-${weekday}`}
                        />
                      ) : (
                        <span dir="ltr" className="text-xs text-gray-700">
                          {day.isWorking ? formatTimeText(day.endTime) : "—"}
                        </span>
                      )}
                    </label>
                  </div>
                  {dayIssue ? (
                    <p
                      role="alert"
                      className="text-xs font-semibold text-danger"
                      style={{ gridColumn: "1 / -1" }}
                    >
                      {weekIssueMessage(dayIssue.code, messages)}
                    </p>
                  ) : null}
                </li>
              );
            })}
          </ul>
          {serverErrors?.days ? (
            <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-week-days-error">
              {serverErrors.days}
            </p>
          ) : null}
        </div>
      )}
    </section>
  );
}

