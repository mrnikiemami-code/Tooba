"use client";

/**
 * TB-256: the ONE method-level CARRIER-ACCEPTANCE calendar, mode-aware.
 *
 * It is a schedule of its own: the operator configures an independent time zone and seven days with
 * the carrier's own open/close bounds. Nothing is auto-derived from the customer delivery week; the
 * only convenience is an explicit ONE-TIME copy that fills ordinary local values, after which the two
 * calendars are fully independent.
 */
import type { EditorCarrierSchedule } from "../model/editor-document.ts";
import {
  type CarrierIssue,
  type CarrierIssueCode,
  type CarrierWeekFormState,
} from "../model/editor-carrier-week.ts";
import { WEEK_DISPLAY_ORDER } from "../model/editor-week.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import { weekdayLabel } from "./authoring-general-tab.tsx";
import { TimeBoundInput, TimeZoneSelect, formatDayRange } from "./authoring-week-editor.tsx";

/** Localized text of one carrier issue (shared by the editor and the screen's issue summary). */
export function carrierIssueMessage(code: CarrierIssueCode, messages: AuthoringMessages): string {
  switch (code) {
    case "timeZoneRequired":
      return messages.weekTimeZoneRequired;
    case "timeZoneTooLong":
      return messages.weekTimeZoneTooLong;
    case "dayHoursRequired":
      return messages.carrierDayHoursRequired;
    case "dayTimeInvalid":
      return messages.carrierDayTimeInvalid;
    case "dayRangeInvalid":
      return messages.carrierDayRangeInvalid;
    case "pendingTimeZoneUnapplied":
      return messages.carrierPendingTimeZoneUnapplied;
  }
}

export function AuthoringCarrierWeekEditor({
  schedule,
  carrier,
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
  schedule: EditorCarrierSchedule | null;
  carrier: CarrierWeekFormState;
  issues: readonly CarrierIssue[];
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
  const configureTimeZone = carrier.kind === "unconfigured" ? carrier.pendingTimeZone : "";

  return (
    <section
      className="rounded-xl border border-border bg-surface-elevated p-3 md:p-4"
      data-testid="shipping-authoring-carrier-week"
      tabIndex={-1}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-base font-black text-gray-900">{messages.carrierWeekTitle}</h3>
        {editable && carrier.kind === "configured" && schedule !== null ? (
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-lg px-2 text-[11px] font-semibold text-gray-500 underline-offset-2 hover:underline"
            data-testid="shipping-authoring-carrier-reset"
            onClick={onReset}
          >
            {messages.weekResetAction}
          </button>
        ) : null}
      </div>

      <p className="mt-1 text-[11px] text-gray-500">{messages.carrierHint}</p>

      {carrier.kind === "unconfigured" ? (
        <div className="mt-2 grid gap-2 text-xs text-gray-700">
          <p data-testid="shipping-authoring-carrier-unconfigured">{messages.carrierWeekUnconfigured}</p>
          {editable ? (
            <div className="flex flex-wrap items-end gap-2">
              <label className="grid w-full gap-0.5 md:w-72">
                <span className="text-xs font-bold text-gray-700">{messages.weekTimeZone}</span>
                <TimeZoneSelect
                  value={configureTimeZone}
                  locale={locale}
                  messages={messages}
                  testId="shipping-authoring-carrier-configure-timezone"
                  onChange={onPendingTimeZone}
                />
              </label>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-carrier-configure"
                disabled={configureTimeZone.trim().length === 0}
                onClick={onConfigure}
              >
                {messages.carrierConfigureAction}
              </button>
              {configureTimeZone.length > 0 ? (
                <button
                  type="button"
                  className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800"
                  data-testid="shipping-authoring-carrier-clear-pending"
                  onClick={onReset}
                >
                  {messages.weekResetAction}
                </button>
              ) : null}
            </div>
          ) : null}
          {editable && configureTimeZone.trim().length > 0 ? (
            <p role="alert" className="text-[11px] font-semibold text-danger" data-testid="shipping-authoring-carrier-pending">
              {messages.carrierPendingTimeZoneUnapplied}
            </p>
          ) : null}
          {editable ? (
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-carrier-copy"
                disabled={!customerConfigured}
                onClick={onCopyFromCustomer}
              >
                {messages.carrierCopyAction}
              </button>
              <span className="text-[11px] text-gray-500">
                {customerConfigured ? messages.carrierCopyHint : messages.carrierCopyUnavailable}
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
                  value={carrier.timeZoneId}
                  locale={locale}
                  messages={messages}
                  testId="shipping-authoring-carrier-timezone"
                  onChange={onTimeZone}
                />
              </label>
              <button
                type="button"
                className="inline-flex min-h-11 items-center rounded-lg border border-border bg-surface px-3 text-xs font-bold text-gray-800 disabled:opacity-50"
                data-testid="shipping-authoring-carrier-copy"
                disabled={!customerConfigured}
                onClick={onCopyFromCustomer}
              >
                {messages.carrierCopyAction}
              </button>
              <span className="max-w-xs text-[11px] text-gray-500">
                {customerConfigured ? messages.carrierCopyHint : messages.carrierCopyUnavailable}
              </span>
            </div>
          ) : (
            <p>
              <span className="font-bold">{messages.weekTimeZone}: </span>
              <span dir="ltr">{carrier.timeZoneId}</span>
            </p>
          )}
          {timeZoneIssue ? (
            <p role="alert" className="text-[11px] font-semibold text-danger">
              {carrierIssueMessage(timeZoneIssue.code, messages)}
            </p>
          ) : null}
          {serverErrors?.timeZone ? (
            <p
              role="alert"
              className="text-[11px] font-semibold text-danger"
              data-testid="shipping-authoring-carrier-timezone-error"
            >
              {serverErrors.timeZone}
            </p>
          ) : null}

          <ul
            className="mt-1 divide-y divide-border rounded-xl border border-border bg-surface"
            data-testid="shipping-authoring-carrier-days"
          >
            {WEEK_DISPLAY_ORDER.map((weekday) => {
              const day = carrier.days.find((candidate) => candidate.weekday === weekday);
              if (!day) return null;
              const dayIssue = issues.find((issue) => issue.weekday === weekday);
              return (
                <li
                  key={weekday}
                  className="flex min-h-14 flex-wrap items-center gap-x-3 gap-y-2 px-3 py-1.5"
                  data-testid={`shipping-authoring-carrier-day-${weekday}`}
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
                        data-testid={`shipping-authoring-carrier-accepting-${weekday}`}
                        onClick={() => onDayWorking(weekday, true)}
                      >
                        {messages.carrierAccepting}
                      </button>
                      <button
                        type="button"
                        className={
                          !day.isWorking
                            ? "inline-flex min-h-11 items-center rounded-md bg-gray-800 px-3 text-xs font-bold text-white"
                            : "inline-flex min-h-11 items-center rounded-md px-3 text-xs font-bold text-gray-600"
                        }
                        aria-pressed={!day.isWorking}
                        data-testid={`shipping-authoring-carrier-notaccepting-${weekday}`}
                        onClick={() => onDayWorking(weekday, false)}
                      >
                        {messages.carrierNotAccepting}
                      </button>
                    </div>
                  ) : (
                    <span className="text-xs font-semibold text-gray-700">
                      {day.isWorking ? messages.carrierAccepting : messages.carrierNotAccepting}
                    </span>
                  )}
                  {day.isWorking ? (
                    editable ? (
                      <div className="flex flex-wrap items-end gap-2">
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.carrierOpenColumn}</span>
                          <TimeBoundInput
                            value={day.opensAt}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-carrier-open-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "opensAt", next)}
                          />
                        </label>
                        <label className="grid gap-0.5">
                          <span className="text-[11px] font-semibold text-gray-500">{messages.carrierCloseColumn}</span>
                          <TimeBoundInput
                            value={day.closesAt}
                            locale={locale}
                            disabled={!day.isWorking}
                            testId={`shipping-authoring-carrier-close-${weekday}`}
                            onChange={(next) => onDayTime(weekday, "closesAt", next)}
                          />
                        </label>
                      </div>
                    ) : (
                      <p
                        className="text-sm font-semibold text-gray-800"
                        data-testid={`shipping-authoring-carrier-range-${weekday}`}
                      >
                        {formatDayRange(day.opensAt, day.closesAt, locale)}
                      </p>
                    )
                  ) : null}
                  {dayIssue ? (
                    <p role="alert" className="w-full text-xs font-semibold text-danger">
                      {carrierIssueMessage(dayIssue.code, messages)}
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
              data-testid="shipping-authoring-carrier-days-error"
            >
              {serverErrors.days}
            </p>
          ) : null}
        </div>
      )}
    </section>
  );
}
