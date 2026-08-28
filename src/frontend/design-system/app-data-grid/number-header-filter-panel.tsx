"use client";

import { useState, type KeyboardEvent } from "react";
import type { GridFilterValue, NumberFilterOperator } from "../data-grid/types";
import { filterOperatorLabelsFor } from "../data-grid/messages";

const NUMBER_OPERATORS: NumberFilterOperator[] = [
  "equals",
  "notEqual",
  "greaterThan",
  "greaterThanOrEqual",
  "lessThan",
  "lessThanOrEqual",
  "between",
  "blank",
  "notBlank",
];

/** پنل فیلتر عددی — draft تا Enter/Apply؛ بدون درخواست سرور هنگام تایپ. */
export function NumberHeaderFilterPanel({
  locale = "fa",
  value,
  valueLabel,
  onApply,
  onClear,
}: {
  locale?: "fa" | "en";
  value?: Extract<GridFilterValue, { kind: "number" }>;
  valueLabel?: string;
  onApply: (value: GridFilterValue) => void;
  onClear: () => void;
}) {
  const ops = filterOperatorLabelsFor(locale);
  const [operator, setOperator] = useState<NumberFilterOperator>(value?.operator ?? "greaterThanOrEqual");
  const [draft, setDraft] = useState(value?.value != null ? String(value.value) : "");
  const [draftTo, setDraftTo] = useState(value?.valueTo != null ? String(value.valueTo) : "");

  const needsValue = operator !== "blank" && operator !== "notBlank";
  const needsSecondValue = operator === "between";

  function commit() {
    if (!needsValue) {
      onApply({ kind: "number", operator, value: 0 });
      return;
    }
    const parsed = Number(draft.replace(/,/g, "").trim());
    if (!Number.isFinite(parsed)) {
      onClear();
      return;
    }
    if (needsSecondValue) {
      const parsedTo = Number(draftTo.replace(/,/g, "").trim());
      if (!Number.isFinite(parsedTo)) return;
      onApply({ kind: "number", operator, value: parsed, valueTo: parsedTo });
      return;
    }
    onApply({ kind: "number", operator, value: parsed });
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      commit();
    }
  }

  function operatorLabel(op: NumberFilterOperator): string {
    switch (op) {
      case "equals":
        return ops.equals;
      case "notEqual":
        return ops.notEqual;
      case "greaterThan":
        return ops.greaterThan;
      case "greaterThanOrEqual":
        return ops.greaterThanOrEqual;
      case "lessThan":
        return ops.lessThan;
      case "lessThanOrEqual":
        return ops.lessThanOrEqual;
      case "between":
        return ops.between;
      case "blank":
        return ops.blank;
      case "notBlank":
        return ops.notBlank;
      default:
        return op;
    }
  }

  return (
    <div className="grid gap-3" dir={locale === "fa" ? "rtl" : "ltr"} data-testid="number-header-filter-panel">
      <label className="grid gap-1.5 text-sm">
        <span className="text-xs font-medium text-muted">{locale === "fa" ? "عملگر" : "Operator"}</span>
        <select
          className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-2 text-sm"
          value={operator}
          onChange={(event) => setOperator(event.target.value as NumberFilterOperator)}
        >
          {NUMBER_OPERATORS.map((op) => (
            <option key={op} value={op}>
              {operatorLabel(op)}
            </option>
          ))}
        </select>
      </label>
      {needsValue ? (
        <>
          <label className="grid gap-1.5 text-sm">
            <span className="text-xs font-medium text-muted">{valueLabel ?? (locale === "fa" ? "مقدار" : "Value")}</span>
            <input
              type="text"
              inputMode="decimal"
              className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-3 text-sm"
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={locale === "fa" ? "مقدار…" : "Value…"}
            />
          </label>
          {needsSecondValue ? (
            <label className="grid gap-1.5 text-sm">
              <span className="text-xs font-medium text-muted">{locale === "fa" ? "تا" : "To"}</span>
              <input
                type="text"
                inputMode="decimal"
                className="min-h-[2.75rem] rounded-ds border border-border bg-surface px-3 text-sm"
                value={draftTo}
                onChange={(event) => setDraftTo(event.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={locale === "fa" ? "مقدار…" : "Value…"}
              />
            </label>
          ) : null}
        </>
      ) : null}
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
