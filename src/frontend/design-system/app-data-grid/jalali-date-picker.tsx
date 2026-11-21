"use client";

import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import dayjs from "dayjs";
import { cn } from "../cn";
import { Portal } from "../primitives/Portal";
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

/** تقویم جلالی تعاملی — بدون date picker مرورگر؛ پنل از طریق Portal. */
export function JalaliDatePicker({
  value,
  onChange,
  onDraftIsoChange,
  onCommit,
  commitOnChange = true,
  placeholder = "۱۴۰۴/۰۱/۰۱",
  ariaLabel,
  locale = "fa",
  panelMinWidth = 288,
}: {
  value?: string;
  onChange?: (iso: string | undefined) => void;
  /** وقتی commitOnChange=false — فقط draft را به والد اطلاع می‌دهد. */
  onDraftIsoChange?: (iso: string | undefined) => void;
  /** Enter یا انتخاب از تقویم — commit صریح. */
  onCommit?: (iso: string | undefined) => void;
  commitOnChange?: boolean;
  placeholder?: string;
  ariaLabel: string;
  locale?: "fa" | "en";
  panelMinWidth?: number;
}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const rootRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const [panelStyle, setPanelStyle] = useState<{ top: number; left: number; width: number }>({
    top: 0,
    left: 0,
    width: panelMinWidth,
  });

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

  function repositionPanel() {
    const node = inputRef.current;
    if (!node) return;
    const rect = node.getBoundingClientRect();
    const width = Math.max(rect.width, panelMinWidth, 288);
    let left = rect.left;
    const maxLeft = window.innerWidth - width - 8;
    if (left > maxLeft) left = Math.max(8, maxLeft);

    const margin = 8;
    const panelHeight = panelRef.current?.offsetHeight ?? 320;
    let top = rect.bottom + 6;
    const spaceBelow = window.innerHeight - top - margin;
    const spaceAbove = rect.top - margin;
    // Flip above when the calendar cannot fit below; otherwise clamp so the last week stays clickable.
    if (panelHeight > spaceBelow && panelHeight <= spaceAbove) {
      top = rect.top - panelHeight - 6;
    } else if (panelHeight > spaceBelow) {
      top = Math.max(margin, window.innerHeight - panelHeight - margin);
    }

    setPanelStyle({ top, left, width });
  }

  useEffect(() => {
    if (!open) return;
    repositionPanel();
    const onDoc = (event: MouseEvent) => {
      const target = event.target as Node;
      if (rootRef.current?.contains(target)) return;
      if ((event.target as HTMLElement | null)?.closest?.("[data-jalali-picker-panel]")) return;
      setOpen(false);
    };
    const onKey = (event: globalThis.KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    const onLayout = () => repositionPanel();
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onKey);
    window.addEventListener("resize", onLayout);
    window.addEventListener("scroll", onLayout, true);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onKey);
      window.removeEventListener("resize", onLayout);
      window.removeEventListener("scroll", onLayout, true);
    };
  }, [open, panelMinWidth]);

  useEffect(() => {
    if (!open) return;
    const frame = requestAnimationFrame(() => repositionPanel());
    return () => cancelAnimationFrame(frame);
    // Re-place with the real panel height after it mounts or its month changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, viewMonth, viewYear]);

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
    if (commitOnChange) {
      onChange?.(iso);
    } else {
      onDraftIsoChange?.(iso);
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== "Enter") return;
    const iso = locale === "fa" ? jalaliInputToIso(text) : text.trim() || undefined;
    onCommit?.(iso);
    if (!commitOnChange) {
      onDraftIsoChange?.(iso);
    } else {
      onChange?.(iso);
    }
    setOpen(false);
  }

  function selectIso(iso: string | undefined) {
    if (!iso) return;
    setText(formatJalaliDate(iso, locale));
    if (commitOnChange) {
      onChange?.(iso);
    } else {
      onDraftIsoChange?.(iso);
      onCommit?.(iso);
    }
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
  const today = dayjs().calendar("jalali");
  // Native <dialog> sits in the top layer; a body portal would render behind it and become unclickable.
  // Portal the floating panel into the owning dialog when present, otherwise keep body for normal pages.
  const portalContainer =
    typeof document !== "undefined" ? (inputRef.current?.closest("dialog") as HTMLElement | null) : null;

  return (
    <div ref={rootRef} className="relative w-full">
      <input
        ref={inputRef}
        type="text"
        inputMode="numeric"
        aria-label={ariaLabel}
        aria-expanded={open}
        placeholder={placeholder}
        value={text}
        onChange={(event) => commitText(event.target.value)}
        onKeyDown={handleKeyDown}
        onFocus={() => {
          if (locale === "fa") {
            repositionPanel();
            setOpen(true);
          }
        }}
        className="min-h-[2.75rem] w-full rounded-ds border border-border bg-surface px-3 text-sm"
        dir={locale === "fa" ? "rtl" : "ltr"}
      />
      {open && locale === "fa" ? (
        <Portal container={portalContainer}>
          <div
            ref={panelRef}
            data-jalali-picker-panel
            className="fixed z-[var(--z-popover)] max-h-[calc(100vh-1rem)] overflow-y-auto rounded-ds border border-border bg-surface-elevated p-3 shadow-ds"
            style={{
              top: panelStyle.top,
              left: panelStyle.left,
              width: panelStyle.width,
              minWidth: panelMinWidth,
            }}
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
                      "min-h-9 rounded-ds text-sm hover:bg-secondary",
                      selectedJalali?.year === viewYear &&
                        selectedJalali.month === viewMonth &&
                        selectedJalali.day === cell.day
                        ? "bg-primary text-primary-foreground"
                        : today.year() === viewYear && today.month() + 1 === viewMonth && today.date() === cell.day
                          ? "border border-primary/40 bg-surface"
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
        </Portal>
      ) : null}
    </div>
  );
}
