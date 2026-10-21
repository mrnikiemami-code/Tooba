"use client";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2 — جدول هفتگی ساعات کاری روش (فقط پیش‌نمایش).
 *
 * یک ردیف برای هر روز هفته با وضعیت باز/تعطیل و بازهٔ مستقل همان روز؛ هیچ بازهٔ مشترک
 * یا سازندهٔ تکرار/تقویم در این قطعه ساخته نمی‌شود. فقط presentation + ویرایش draft محلی.
 */

import { useId } from "react";
import { Field, Input } from "../../../../design-system";
import {
  PREVIEW_WEEKDAY_IDS,
  previewDayHoursFieldErrors,
  previewScheduleWithDay,
  type PreviewDayHours,
  type PreviewLocale,
  type PreviewSchedule,
  type PreviewWeekdayId,
} from "./preview-model.ts";
import {
  previewDayHoursIssueMessage,
  previewWeekdayLabel,
  type PreviewMessages,
} from "./preview-i18n.ts";

export function PreviewScheduleEditor({
  schedule,
  readOnly,
  locale,
  messages,
  onChange,
}: {
  schedule: PreviewSchedule;
  readOnly: boolean;
  locale: PreviewLocale;
  messages: PreviewMessages;
  onChange: (next: PreviewSchedule) => void;
}) {
  const fieldId = useId();
  const setDay = (day: PreviewWeekdayId, hours: PreviewDayHours): void =>
    onChange(previewScheduleWithDay(schedule, day, hours));

  return (
    <div className="grid gap-2" data-testid="shipping-preview-weekly-schedule">
      <div className="hidden gap-2 px-2 text-[11px] font-bold text-gray-500 sm:grid sm:grid-cols-[7rem_6rem_minmax(0,1fr)]">
        <span>{messages.scheduleTableDay}</span>
        <span>{messages.scheduleTableStatus}</span>
        <span>{`${messages.scheduleFrom} / ${messages.scheduleTo}`}</span>
      </div>

      {PREVIEW_WEEKDAY_IDS.map((day) => {
        const hours = schedule.hours[day];
        const dayLabel = previewWeekdayLabel(day, locale);
        const errors = previewDayHoursFieldErrors(hours);
        const statusLabel = hours.working ? messages.scheduleDayOpen : messages.scheduleDayClosed;
        return (
          <div
            key={day}
            className={
              hours.working
                ? "grid gap-2 rounded-xl border border-border bg-surface p-2 sm:grid-cols-[7rem_6rem_minmax(0,1fr)] sm:items-start"
                : "grid gap-2 rounded-xl border border-gray-200 bg-gray-50 p-2 sm:grid-cols-[7rem_6rem_minmax(0,1fr)] sm:items-start"
            }
            data-testid={`shipping-preview-day-row-${day}`}
            data-day-state={hours.working ? "open" : "closed"}
          >
            <div className="flex items-center justify-between gap-2 sm:block">
              <p className="text-xs font-black text-gray-900">{dayLabel}</p>
              <label className="inline-flex items-center gap-1 text-[11px] font-bold text-gray-700">
                <input
                  type="checkbox"
                  className="h-4 w-4"
                  checked={hours.working}
                  disabled={readOnly}
                  aria-label={`${dayLabel} — ${statusLabel}`}
                  data-testid={`shipping-preview-day-${day}-working`}
                  onChange={(event) =>
                    setDay(day, { working: event.target.checked, fromTime: hours.fromTime, toTime: hours.toTime })
                  }
                />
                <span data-testid={`shipping-preview-day-${day}-status`}>{statusLabel}</span>
              </label>
            </div>

            {hours.working ? (
              <div className="grid min-w-0 grid-cols-2 gap-2 sm:col-span-2">
                <Field
                  id={`${fieldId}-${day}-from`}
                  label={messages.scheduleFrom}
                  error={previewDayHoursIssueMessage(errors.start, locale)}
                >
                  <Input
                    type="time"
                    step={60}
                    dir="ltr"
                    className="min-w-0"
                    value={hours.fromTime}
                    disabled={readOnly}
                    data-testid={`shipping-preview-day-${day}-from`}
                    onChange={(event) =>
                      setDay(day, { working: hours.working, fromTime: event.target.value, toTime: hours.toTime })
                    }
                  />
                </Field>
                <Field
                  id={`${fieldId}-${day}-to`}
                  label={messages.scheduleTo}
                  error={previewDayHoursIssueMessage(errors.end ?? errors.interval, locale)}
                >
                  <Input
                    type="time"
                    step={60}
                    dir="ltr"
                    className="min-w-0"
                    value={hours.toTime}
                    disabled={readOnly}
                    data-testid={`shipping-preview-day-${day}-to`}
                    onChange={(event) =>
                      setDay(day, { working: hours.working, fromTime: hours.fromTime, toTime: event.target.value })
                    }
                  />
                </Field>
              </div>
            ) : (
              <p className="text-[11px] text-gray-500 sm:col-span-2">{messages.scheduleClosedDayNote}</p>
            )}
          </div>
        );
      })}
    </div>
  );
}
