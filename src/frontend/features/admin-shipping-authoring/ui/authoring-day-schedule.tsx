"use client";

/**
 * ONE header for the seven-day calendars of a shipping method: customer delivery, carrier acceptance and
 * warehouse preparation all show the same four columns (day, status, from, to).
 *
 * The column titles used to be repeated inside every working row, right above each time field. That made a
 * working row two lines tall while a closed row stayed one line, and pushed the weekday off the axis of the
 * toggle and the times. The titles belong to the WEEK, so they are printed once here and the rows below carry
 * only their values; the geometry itself lives in the `ds-day-schedule` rules in `app/globals.css`.
 */
export function DayScheduleHeader({
  dayLabel,
  statusLabel,
  fromLabel,
  toLabel,
  testId,
}: {
  dayLabel: string;
  statusLabel: string;
  fromLabel: string;
  toLabel: string;
  testId: string;
}) {
  return (
    <div className="ds-day-schedule ds-day-schedule--head" data-testid={testId} aria-hidden="true">
      <span>{dayLabel}</span>
      <span>{statusLabel}</span>
      <span>{fromLabel}</span>
      <span>{toLabel}</span>
    </div>
  );
}
