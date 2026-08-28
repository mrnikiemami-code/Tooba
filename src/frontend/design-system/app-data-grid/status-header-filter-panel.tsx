"use client";

import { useState } from "react";
import type { GridFilterValue } from "../data-grid/types";

export type StatusFilterOption = { value: string; label: string };

/** پنل فیلتر وضعیت/enum — app-owned. */
export function StatusHeaderFilterPanel({
  locale = "fa",
  value,
  options,
  onApply,
  onClear,
}: {
  locale?: "fa" | "en";
  value?: Extract<GridFilterValue, { kind: "status" }>;
  options: StatusFilterOption[];
  onApply: (value: GridFilterValue) => void;
  onClear: () => void;
}) {
  const [selected, setSelected] = useState<string[]>(value?.values ?? []);

  function toggle(optionValue: string) {
    setSelected((current) =>
      current.includes(optionValue) ? current.filter((item) => item !== optionValue) : [...current, optionValue],
    );
  }

  function commit() {
    if (selected.length === 0) {
      onClear();
      return;
    }
    onApply({
      kind: "status",
      operator: selected.length === 1 ? "equals" : "in",
      values: selected,
    });
  }

  return (
    <div className="grid gap-3" dir={locale === "fa" ? "rtl" : "ltr"} data-testid="status-header-filter-panel">
      <fieldset className="grid gap-2">
        <legend className="text-xs font-medium text-muted">{locale === "fa" ? "وضعیت" : "Status"}</legend>
        {options.map((option) => (
          <label key={option.value} className="flex min-h-9 items-center gap-2 text-sm">
            <input
              type="checkbox"
              className="size-4 rounded border-border"
              checked={selected.includes(option.value)}
              onChange={() => toggle(option.value)}
            />
            <span>{option.label}</span>
          </label>
        ))}
      </fieldset>
      <div className="flex items-center justify-between gap-2 border-t border-border pt-3">
        <button type="button" className="min-h-[2.75rem] rounded-ds px-3 text-sm text-muted hover:text-foreground" onClick={onClear}>
          {locale === "fa" ? "پاک کردن" : "Clear"}
        </button>
        <button type="button" className="min-h-[2.75rem] rounded-ds bg-primary px-4 text-sm text-primary-foreground" onClick={commit}>
          {locale === "fa" ? "اعمال" : "Apply"}
        </button>
      </div>
    </div>
  );
}
