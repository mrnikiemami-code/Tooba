"use client";

import dayjs from "dayjs";
import jalaliday from "jalaliday";
import { JalaliDatePicker } from "../../design-system/app-data-grid/jalali-date-picker";
import { formatJalaliDateTime } from "../../design-system/app-data-grid/jalali";

dayjs.extend(jalaliday);

function toDatetimeLocalValue(iso: string): string {
  const d = dayjs(iso);
  if (!d.isValid()) return "";
  return d.format("YYYY-MM-DDTHH:mm");
}

function fromDatetimeLocalValue(value: string): string | undefined {
  if (!value.trim()) return undefined;
  const d = dayjs(value);
  return d.isValid() ? d.toDate().toISOString() : undefined;
}

function combineJalaliDateAndTime(dateIso: string | undefined, timeHm: string): string | undefined {
  if (!dateIso) return undefined;
  const [hh, mm] = timeHm.split(":").map((p) => Number.parseInt(p, 10));
  const hour = Number.isFinite(hh) ? hh : 12;
  const minute = Number.isFinite(mm) ? mm : 0;
  const d = dayjs(dateIso).hour(hour).minute(minute).second(0).millisecond(0);
  return d.isValid() ? d.toDate().toISOString() : undefined;
}

function timePartFromIso(iso: string): string {
  const d = dayjs(iso);
  if (!d.isValid()) return "12:00";
  return d.format("HH:mm");
}

/**
 * ورودی زمان انتشار Admin:
 * - fa: Jalali date picker + time
 * - en: Gregorian datetime-local
 * DB همیشه ISO/UTC می‌ماند.
 */
export function ContentArticlePublishDateField({
  locale,
  valueIso,
  disabled,
  onChangeIso,
}: {
  locale: string;
  valueIso: string;
  disabled?: boolean;
  onChangeIso: (iso: string) => void;
}) {
  const fa = locale.trim().toLowerCase().startsWith("fa");
  const display = formatJalaliDateTime(valueIso || undefined, fa ? "fa" : "en");

  if (fa) {
    return (
      <div className="space-y-2" data-testid="content-article-publish-date-fa" dir="rtl">
        <span className="mb-1 block text-sm text-muted">زمان انتشار (جلالی — ذخیره UTC)</span>
        <div className="flex flex-wrap items-end gap-2">
          <div className="min-w-[14rem] flex-1">
            <JalaliDatePicker
              value={valueIso || undefined}
              locale="fa"
              ariaLabel="تاریخ انتشار جلالی"
              onChange={(iso) => {
                if (!iso) return;
                const next = combineJalaliDateAndTime(iso, timePartFromIso(valueIso || iso));
                if (next) onChangeIso(next);
              }}
            />
          </div>
          <label className="block text-sm">
            <span className="mb-1 block text-muted">ساعت</span>
            <input
              type="time"
              className="rounded-xl border px-3 py-2"
              dir="ltr"
              disabled={disabled}
              value={timePartFromIso(valueIso)}
              data-testid="content-article-publish-time-fa"
              onChange={(e) => {
                const next = combineJalaliDateAndTime(valueIso || new Date().toISOString(), e.target.value);
                if (next) onChangeIso(next);
              }}
            />
          </label>
        </div>
        <p className="text-xs text-muted">
          نمایش: {display} · UTC: <span dir="ltr">{valueIso || "—"}</span>
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-2" data-testid="content-article-publish-date-en" dir="ltr">
      <label className="block text-sm">
        <span className="mb-1 block text-muted">Publish date/time (Gregorian → UTC)</span>
        <input
          type="datetime-local"
          className="w-full rounded-xl border px-3 py-2"
          disabled={disabled}
          value={toDatetimeLocalValue(valueIso)}
          data-testid="content-article-publish-datetime-en"
          onChange={(e) => {
            const next = fromDatetimeLocalValue(e.target.value);
            if (next) onChangeIso(next);
          }}
        />
      </label>
      <p className="text-xs text-muted">Display: {display}</p>
    </div>
  );
}
