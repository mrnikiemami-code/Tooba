"use client";

import { useState } from "react";
import type { GridFilterValue } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages";
import { JalaliDatePicker } from "./jalali-date-picker";

/** پنل فیلتر تاریخ جلالی — app-owned؛ خارج از AG Grid menu. */
export function JalaliHeaderFilterPanel({
  locale = "fa",
  value,
  onApply,
  onClear,
}: {
  locale?: "fa" | "en";
  value?: Extract<GridFilterValue, { kind: "date" }>;
  onApply: (value: GridFilterValue) => void;
  onClear: () => void;
}) {
  const ops = filterOperatorLabelsFor(locale);
  const [operator, setOperator] = useState<"on" | "before" | "after" | "between">(value?.operator ?? "on");
  const [draftFrom, setDraftFrom] = useState<string | undefined>(value?.iso);
  const [draftTo, setDraftTo] = useState<string | undefined>(value?.isoTo);

  function handleApply() {
    if (!draftFrom) return;
    if (operator === "between" && !draftTo) return;
    onApply({
      kind: "date",
      operator,
      iso: draftFrom,
      isoTo: operator === "between" ? draftTo : undefined,
    });
  }

  return (
    <div className="grid gap-3" dir={locale === "fa" ? "rtl" : "ltr"} data-testid="jalali-header-filter-panel">
      <label className="grid gap-1.5 text-sm">
        <span className="text-xs font-medium text-muted">{locale === "fa" ? "عملگر" : "Operator"}</span>
        <select
          className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-2 text-sm"
          value={operator}
          onChange={(event) => setOperator(event.target.value as typeof operator)}
        >
          <option value="on">{ops.on}</option>
          <option value="before">{ops.before}</option>
          <option value="after">{ops.after}</option>
          <option value="between">{ops.between}</option>
        </select>
      </label>
      <label className="grid gap-1.5 text-sm">
        <span className="text-xs font-medium text-muted">
          {operator === "between" ? (locale === "fa" ? "از تاریخ" : "From") : locale === "fa" ? "تاریخ" : "Date"}
        </span>
        <JalaliDatePicker
          ariaLabel={locale === "fa" ? "تاریخ" : "Date"}
          locale={locale}
          value={draftFrom}
          panelMinWidth={320}
          commitOnChange={false}
          onDraftIsoChange={setDraftFrom}
          onCommit={setDraftFrom}
        />
      </label>
      {operator === "between" ? (
        <label className="grid gap-1.5 text-sm">
          <span className="text-xs font-medium text-muted">{locale === "fa" ? "تا تاریخ" : "To"}</span>
          <JalaliDatePicker
            ariaLabel={locale === "fa" ? "تا تاریخ" : "To date"}
            locale={locale}
            value={draftTo}
            panelMinWidth={320}
            commitOnChange={false}
            onDraftIsoChange={setDraftTo}
            onCommit={setDraftTo}
          />
        </label>
      ) : null}
      <div className="flex items-center justify-between gap-2 border-t border-border pt-3">
        <button type="button" className="min-h-[2.75rem] rounded-ds px-3 text-sm text-muted hover:text-foreground" onClick={onClear}>
          {locale === "fa" ? "پاک کردن" : "Clear"}
        </button>
        <button type="button" className="min-h-[2.75rem] rounded-ds bg-primary px-4 text-sm text-primary-foreground" onClick={handleApply}>
          {locale === "fa" ? "اعمال" : "Apply"}
        </button>
      </div>
    </div>
  );
}
