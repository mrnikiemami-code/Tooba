"use client";

import { useState, type KeyboardEvent } from "react";
import type { GridFilterValue } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages";

/** پنل فیلتر متنی — draft تا Enter/Apply؛ بدون درخواست سرور هنگام تایپ. */
export function TextHeaderFilterPanel({
  locale = "fa",
  value,
  onApply,
  onClear,
}: {
  locale?: "fa" | "en";
  value?: Extract<GridFilterValue, { kind: "text" }>;
  onApply: (value: GridFilterValue) => void;
  onClear: () => void;
}) {
  const ops = filterOperatorLabelsFor(locale);
  const [operator, setOperator] = useState<"contains" | "equals" | "startsWith">(value?.operator ?? "contains");
  const [draft, setDraft] = useState(value?.query ?? "");

  function commit() {
    const query = draft.trim();
    if (!query) {
      onClear();
      return;
    }
    onApply({ kind: "text", operator, query });
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      commit();
    }
  }

  return (
    <div className="grid gap-3" dir={locale === "fa" ? "rtl" : "ltr"} data-testid="text-header-filter-panel">
      <label className="grid gap-1.5 text-sm">
        <span className="text-xs font-medium text-muted">{locale === "fa" ? "عملگر" : "Operator"}</span>
        <select
          className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-2 text-sm"
          value={operator}
          onChange={(event) => setOperator(event.target.value as typeof operator)}
        >
          <option value="contains">{ops.contains}</option>
          <option value="equals">{ops.equals}</option>
          <option value="startsWith">{ops.startsWith}</option>
        </select>
      </label>
      <label className="grid gap-1.5 text-sm">
        <span className="text-xs font-medium text-muted">{locale === "fa" ? "مقدار" : "Value"}</span>
        <input
          type="text"
          className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-3 text-sm"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={locale === "fa" ? "جستجو…" : "Search…"}
        />
      </label>
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
