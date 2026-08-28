"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import dayjs from "dayjs";
import { cn } from "../cn";
import { formatJalaliDate, isoToJalaliDisplay, jalaliInputToIso } from "./jalali";

const FA_MONTHS = [
  "فروردین",
  "اردیبهشت",
  "خرداد",
  "تیر",
  "مرداد",
  "شهریور",
  "مهر",
  "آبان",
  "آذر",
  "دی",
  "بهمن",
  "اسفند",
];
const FA_WEEKDAYS = ["ش", "ی", "د", "س", "چ", "پ", "ج"];

/** تقویم جلالی تعاملی — بدون date picker مرورگر. */
export function JalaliDatePicker({
  value,
  onChange,
  placeholder = "۱۴۰۴/۰۱/۰۱",
  ariaLabel,
  locale = "fa",
}: {
  value?: string;
  onChange: (iso: string | undefined) => void;
  placeholder?: string;
  ariaLabel: string;
  locale?: "fa" | "en";
}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const rootRef = useRef<HTMLDivElement>(null);

  const anchor = useMemo(() => {
    if (value) {
      const parsed = dayjs(value);
      if (parsed.isValid()) return parsed.calendar("jalali");
    }
    return dayjs().calendar("jalali");
  }, [value]);

  const [viewYear, setViewYear] = useState(anchor.year());
  const [viewMonth, setViewMonth] = useState(anchor.month() + 1);

  useEffect(() => {
    setText(value ? formatJalaliDate(value, locale) : "");
  }, [value, locale]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  const days = useMemo(() => {
    const first = dayjs()
      .calendar("jalali")
      .year(viewYear)
      .month(viewMonth - 1)
      .date(1);
    const daysInMonth = first.daysInMonth();
    const startWeekday = first.day();
    const cells: Array<{ day: number; iso?: string } | null> = [];
    for (let i = 0; i < startWeekday; i += 1) cells.push(null);
    for (let day = 1; day <= daysInMonth; day += 1) {
      const d = dayjs()
        .calendar("jalali")
        .year(viewYear)
        .month(viewMonth - 1)
        .date(day)
        .hour(12)
        .minute(0)
        .second(0);
      cells.push({ day, iso: d.isValid() ? d.toDate().toISOString() : undefined });
    }
    return cells;
  }, [viewMonth, viewYear]);

  function commitText(raw: string) {
    setText(raw);
    const iso = locale === "fa" ? jalaliInputToIso(raw) : raw.trim() || undefined;
    onChange(iso);
  }

  function selectIso(iso: string | undefined) {
    if (!iso) return;
    onChange(iso);
    setText(formatJalaliDate(iso, locale));
    setOpen(false);
  }

  function shiftMonth(delta: number) {
    let m = viewMonth + delta;
    let y = viewYear;
    while (m < 1) {
      m += 12;
      y -= 1;
    }
    while (m > 12) {
      m -= 12;
      y += 1;
    }
    setViewMonth(m);
    setViewYear(y);
  }

  const selectedJalali = value ? isoToJalaliDisplay(value) : null;

  return (
    <div ref={rootRef} className="relative w-full">
      <input
        type="text"
        inputMode="numeric"
        aria-label={ariaLabel}
        placeholder={placeholder}
        value={text}
        onChange={(event) => commitText(event.target.value)}
        onFocus={() => locale === "fa" && setOpen(true)}
        className="min-h-10 w-full rounded-ds border border-border bg-surface px-3 text-sm"
        dir={locale === "fa" ? "rtl" : "ltr"}
      />
      {open && locale === "fa" ? (
        <div
          className="absolute z-[var(--z-overlay)] mt-1 w-[min(18rem,100vw-2rem)] rounded-ds border border-border bg-surface-elevated p-3 shadow-ds"
          data-testid="jalali-date-picker-panel"
        >
          <div className="mb-2 flex items-center justify-between gap-2">
            <button type="button" className="rounded-ds px-2 py-1 text-sm hover:bg-secondary" onClick={() => shiftMonth(-1)} aria-label="ماه قبل">
              ‹
            </button>
            <span className="text-sm font-medium">
              {FA_MONTHS[viewMonth - 1]} {viewYear.toLocaleString("fa-IR")}
            </span>
            <button type="button" className="rounded-ds px-2 py-1 text-sm hover:bg-secondary" onClick={() => shiftMonth(1)} aria-label="ماه بعد">
              ›
            </button>
          </div>
          <div className="grid grid-cols-7 gap-1 text-center text-xs text-muted">
            {FA_WEEKDAYS.map((label) => (
              <span key={label}>{label}</span>
            ))}
          </div>
          <div className="mt-1 grid grid-cols-7 gap-1">
            {days.map((cell, index) =>
              cell ? (
                <button
                  key={`${cell.day}-${index}`}
                  type="button"
                  className={cn(
                    "min-h-8 rounded-ds text-sm hover:bg-secondary",
                    selectedJalali?.year === viewYear &&
                      selectedJalali.month === viewMonth &&
                      selectedJalali.day === cell.day
                      ? "bg-primary text-primary-foreground"
                      : "bg-surface",
                  )}
                  onClick={() => selectIso(cell.iso)}
                >
                  {cell.day.toLocaleString("fa-IR")}
                </button>
              ) : (
                <span key={`empty-${index}`} />
              ),
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
