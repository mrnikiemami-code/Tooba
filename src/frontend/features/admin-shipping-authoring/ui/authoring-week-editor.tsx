"use client";

/**
 * The ONE method-level weekly schedule, mode-aware: read-only in view, editable in edit.
 *
 * An unconfigured schedule is NOT auto-created: it stays explicitly unconfigured until the operator
 * supplies a time zone and deliberately configures it. Working/closed toggles keep the remembered
 * hours, a working day requires a real start/end pair, and an untouched stored `TimeText` (including
 * its seconds/fractions) is never rewritten by simply rendering this component.
 */
import type { EditorWeeklySchedule } from "../model/editor-document.ts";
import type { WeekFormState, WeekIssue } from "../model/editor-week.ts";
import { WEEK_DISPLAY_ORDER, type WeekIssueCode } from "../model/editor-week.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { formatTimeText } from "./authoring-format.ts";
import { weekdayLabel } from "./authoring-general-tab.tsx";
import { TIME_ZONE_OPTIONS, isOfferedTimeZone, timeZoneLabel } from "./time-zone-options.ts";

import { SLOT_DURATION_OPTIONS } from "../model/slot-duration-options.ts";
export { SLOT_DURATION_OPTIONS };

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
    case "slotDurationInvalid":
      return messages.weekSlotDurationInvalid;
  }
}

/** `HH:mm` for a `time` input: an untouched stored token is shown truncated but never rewritten. */
function inputTime(value: string): string {
  const match = /^([01][0-9]|2[0-3]):([0-5][0-9])/.exec(value);
  return match ? `${match[1]}:${match[2]}` : value;
}

/** Western digits → Persian digits for FA display (never mutates the stored value). */
function toFaDigits(text: string): string {
  return text.replace(/\d/g, (digit) => "۰۱۲۳۴۵۶۷۸۹"[Number(digit)]!);
}

/** Readable 24-hour range for FA without AM/PM chrome. */
function formatDayRange(start: string, end: string, locale: AuthoringLocale): string {
  const from = formatTimeText(start);
  const to = formatTimeText(end);
  const open = locale === "fa" ? `${toFaDigits(from)} تا ${toFaDigits(to)}` : `${from}–${to}`;
  return open;
}

/** FA uses a plain 24h text field so the browser never paints AM/PM. */
function TimeBoundInput({
  value,
  locale,
  disabled,
  testId,
  onChange,
}: {
  value: string;
  locale: AuthoringLocale;
  disabled: boolean;
  testId: string;
  onChange: (next: string) => void;
}) {
  if (locale === "fa") {
    return (
      <input
        type="text"
        inputMode="numeric"
        placeholder="۰۹:۰۰"
        dir="ltr"
        className="min-h-11 w-[7.5rem] max-w-full rounded-lg border border-border bg-surface px-2 text-sm tabular-nums disabled:cursor-not-allowed disabled:opacity-50"
        value={inputTime(value)}
        disabled={disabled}
        pattern="^([01][0-9]|2[0-3]):[0-5][0-9]$"
        onChange={(event) => onChange(event.target.value)}
        data-testid={testId}
        data-time-format="24h"
      />
    );
  }
  return (
    <input
      type="time"
      dir="ltr"
      className="min-h-11 w-[7.5rem] max-w-full rounded-lg border border-border bg-surface px-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
      value={inputTime(value)}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
      data-testid={testId}
      data-time-format="24h"
    />
  );
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
      className="min-h-11 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
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
  onSlotDuration,
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
  /** TB-241: explicit slot length in minutes, or null for one full-day window. */
  onSlotDuration: (minutes: number | null) => void;
  onDayWorking: (weekday: number, isWorking: boolean) => void;
  onDayTime: (weekday: number, bound: "startTime" | "endTime", value: string) => void;
  onReset: () => void;
}) {
  const timeZoneIssue = issues.find((issue) => issue.code === "timeZoneRequired" || issue.code === "timeZoneTooLong");
  const configureTimeZone = week.kind === "unconfigured" ? week.pendingTimeZone : "";

  return (
    <section
      className="rounded-xl border border-border bg-surface-elevated p-3 md:p-4"
      data-testid="shipping-authoring-week"
      tabIndex={-1}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-base font-black text-gray-900">{messages.weekTitle}</h3>
        {editable && week.kind === "configured" && schedule !== null ? (
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-lg px-2 text-[11px] font-semibold text-gray-500 underline-offset-2 hover:underline"
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
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-week-configure"
                disabled={configureTimeZone.trim().length === 0}
                onClick={onConfigure}
              >
                {messages.weekConfigureAction}
              </button>
              {configureTimeZone.length > 0 ? (
                <button
                  type="button"
                  className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800"
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

          {/* TB-241 values unchanged; R2: one segmented mode; duration only when hourly. */}
          <fieldset className="grid gap-2" data-testid="shipping-authoring-week-slot-duration">
            <legend className="text-xs font-bold text-gray-800">{messages.weekSlotDuration}</legend>
            {editable ? (
              <>
                <div
                  className="inline-flex max-w-full flex-wrap rounded-xl border border-border bg-surface p-0.5"
                  role="radiogroup"
                  aria-label={messages.weekSlotDuration}
                >
                  <label
                    className={
                      week.slotDurationMinutes === null
                        ? "inline-flex min-h-11 cursor-pointer items-center gap-2 rounded-lg bg-primary px-3 text-sm font-bold text-primary-foreground focus-within:ring-2 focus-within:ring-primary/40"
                        : "inline-flex min-h-11 cursor-pointer items-center gap-2 rounded-lg px-3 text-sm font-semibold text-gray-700 focus-within:ring-2 focus-within:ring-primary/40"
                    }
                  >
                    <input
                      type="radio"
                      className="h-4 w-4 shrink-0 accent-primary"
                      name="shipping-authoring-slot-mode"
                      checked={week.slotDurationMinutes === null}
                      onChange={() => onSlotDuration(null)}
                      data-testid="shipping-authoring-week-slot-full-day"
                    />
                    <span>{messages.weekSlotDurationFullDay}</span>
                  </label>
                  <label
                    className={
                      week.slotDurationMinutes !== null
                        ? "inline-flex min-h-11 cursor-pointer items-center gap-2 rounded-lg bg-primary px-3 text-sm font-bold text-primary-foreground focus-within:ring-2 focus-within:ring-primary/40"
                        : "inline-flex min-h-11 cursor-pointer items-center gap-2 rounded-lg px-3 text-sm font-semibold text-gray-700 focus-within:ring-2 focus-within:ring-primary/40"
                    }
                  >
                    <input
                      type="radio"
                      className="h-4 w-4 shrink-0 accent-primary"
                      name="shipping-authoring-slot-mode"
                      checked={week.slotDurationMinutes !== null}
                      onChange={() => {
                        if (week.slotDurationMinutes !== null) return;
                        onSlotDuration(60);
                      }}
                      data-testid="shipping-authoring-week-slot-hourly"
                    />
                    <span>{messages.weekSlotDurationHourly}</span>
                  </label>
                </div>
                {week.slotDurationMinutes !== null ? (
                  <label className="grid max-w-xs gap-0.5">
                    <span className="text-[11px] font-semibold text-gray-500">{messages.weekSlotDurationHourly}</span>
                    <select
                      className="min-h-11 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900"
                      value={String(week.slotDurationMinutes)}
                      data-testid="shipping-authoring-week-slot-duration-select"
                      onChange={(event) => onSlotDuration(Number(event.target.value))}
                      aria-label={messages.weekSlotDurationHourly}
                    >
                      {!(SLOT_DURATION_OPTIONS as readonly number[]).includes(week.slotDurationMinutes) ? (
                        <option value={String(week.slotDurationMinutes)}>
                          {week.slotDurationMinutes} {messages.weekSlotDurationMinutes}
                        </option>
                      ) : null}
                      {SLOT_DURATION_OPTIONS.map((minutes) => (
                        <option key={minutes} value={String(minutes)}>
                          {minutes} {messages.weekSlotDurationMinutes}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : null}
              </>
            ) : (
              <p>
                <span data-testid="shipping-authoring-week-slot-duration-value">
                  {week.slotDurationMinutes === null
                    ? messages.weekSlotDurationFullDay
                    : `${week.slotDurationMinutes} ${messages.weekSlotDurationMinutes}`}
                </span>
              </p>
            )}
            {editable ? (
              <p className="text-[11px] text-gray-500">
                {week.slotDurationMinutes === null
                  ? messages.weekSlotDurationFullDayExample
                  : messages.weekSlotDurationHint}
              </p>
            ) : null}
            {issues.some((issue) => issue.code === "slotDurationInvalid") ? (
              <p role="alert" className="text-[11px] font-semibold text-danger">
                {messages.weekSlotDurationInvalid}
              </p>
            ) : null}
          </fieldset>

          <ul className="mt-1 divide-y divide-border rounded-xl border border-border bg-surface" data-testid="shipping-authoring-week-days">
            {WEEK_DISPLAY_ORDER.map((weekday) => {
              const day = week.days.find((candidate) => candidate.weekday === weekday);
              if (!day) return null;
              const dayIssue = issues.find((issue) => issue.weekday === weekday);
              return (
                <li
                  key={weekday}
                  className="flex min-h-14 flex-wrap items-center gap-x-3 gap-y-2 px-3 py-1.5"
                  data-testid={`shipping-authoring-week-day-${weekday}`}
                >
                  <span className="w-20 shrink-0 text-sm font-bold text-gray-900">{weekdayLabel(weekday, locale)}</span>
                  {editable ? (
                    <div className="inline-flex rounded-lg border border-border p-0.5" role="group" aria-label={messages.weekStatusColumn}>
                      <button
                        type="button"
                        className={
                          day.isWorking
                            ? "inline-flex min-h-11 items-center rounded-md bg-primary px-3 text-xs font-bold text-primary-foreground"
                            : "inline-flex min-h-11 items-center rounded-md px-3 text-xs font-bold text-gray-600"
                        }
                        aria-pressed={day.isWorking}
                        data-testid={`shipping-authoring-week-working-${weekday}`}
                        onClick={() => onDayWorking(weekday, true)}
                      >
                        {messages.weekActive}
                      </button>
                      <button
                        type="button"
                        className={
                          !day.isWorking
                            ? "inline-flex min-h-11 items-center rounded-md bg-gray-800 px-3 text-xs font-bold text-white"
                            : "inline-flex min-h-11 items-center rounded-md px-3 text-xs font-bold text-gray-600"
                        }
                        aria-pressed={!day.isWorking}
                        data-testid={`shipping-authoring-week-closed-${weekday}`}
                        onClick={() => onDayWorking(weekday, false)}
                      >
                        {messages.weekClosed}
                      </button>
                    </div>
                  ) : (
                    <span className="text-xs font-semibold text-gray-700">
                      {day.isWorking ? messages.weekActive : messages.weekClosed}
                    </span>
                  )}
                  {day.isWorking ? (
                    editable ? (
                      <div className="flex flex-wrap items-end gap-2">
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.weekFromColumn}</span>
                          <TimeBoundInput
                            value={day.startTime}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-week-start-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "startTime", next)}
                          />
                        </label>
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.weekToColumn}</span>
                          <TimeBoundInput
                            value={day.endTime}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-week-end-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "endTime", next)}
                          />
                        </label>
                      </div>
                    ) : (
                      <p
                        className="text-sm font-semibold text-gray-800"
                        data-testid={`shipping-authoring-week-range-${weekday}`}
                      >
                        {formatDayRange(day.startTime, day.endTime, locale)}
                      </p>
                    )
                  ) : null}
                  {dayIssue ? (
                    <p role="alert" className="w-full text-xs font-semibold text-danger">
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

