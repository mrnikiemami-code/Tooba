"use client";

import { forwardRef, useCallback, useImperativeHandle, useState } from "react";
import type { IFilterParams } from "ag-grid-community";
import { JalaliDatePicker } from "./jalali-date-picker";
import { filterOperatorLabelsFor } from "../data-grid/messages";

export type JalaliDateAgFilterModel = {
  filterType: "date";
  type: "equals" | "lessThan" | "greaterThan" | "inRange";
  dateFrom?: string | null;
  dateTo?: string | null;
};

type DateOperator = "on" | "before" | "after" | "between";

function operatorToAgType(operator: DateOperator): JalaliDateAgFilterModel["type"] {
  switch (operator) {
    case "before":
      return "lessThan";
    case "after":
      return "greaterThan";
    case "between":
      return "inRange";
    default:
      return "equals";
  }
}

function agTypeToOperator(type: JalaliDateAgFilterModel["type"] | undefined): DateOperator {
  switch (type) {
    case "lessThan":
      return "before";
    case "greaterThan":
      return "after";
    case "inRange":
      return "between";
    default:
      return "on";
  }
}

/** فیلتر ستونی تاریخ جلالی — draft تا Enter/Apply؛ بدون date picker مرورگر. */
export const JalaliDateColumnFilter = forwardRef(function JalaliDateColumnFilter(
  props: IFilterParams,
  ref,
) {
  const locale = ((props.context?.locale as "fa" | "en" | undefined) ?? "fa") as "fa" | "en";
  const ops = filterOperatorLabelsFor(locale);
  const [operator, setOperator] = useState<DateOperator>("on");
  const [draftFrom, setDraftFrom] = useState<string | undefined>();
  const [draftTo, setDraftTo] = useState<string | undefined>();
  const [applied, setApplied] = useState<JalaliDateAgFilterModel | null>(null);

  const buildModel = useCallback((): JalaliDateAgFilterModel | null => {
    if (!draftFrom) return null;
    if (operator === "between" && !draftTo) return null;
    return {
      filterType: "date",
      type: operatorToAgType(operator),
      dateFrom: draftFrom,
      dateTo: operator === "between" ? draftTo ?? null : null,
    };
  }, [draftFrom, draftTo, operator]);

  const applyFilter = useCallback(() => {
    const model = buildModel();
    setApplied(model);
    props.filterChangedCallback();
  }, [buildModel, props]);

  const resetFilter = useCallback(() => {
    setOperator("on");
    setDraftFrom(undefined);
    setDraftTo(undefined);
    setApplied(null);
    props.filterChangedCallback();
  }, [props]);

  useImperativeHandle(
    ref,
    () => ({
      isFilterActive() {
        return applied !== null;
      },
      doesFilterPass() {
        return true;
      },
      getModel(): JalaliDateAgFilterModel | null {
        return applied;
      },
      setModel(model: JalaliDateAgFilterModel | null) {
        if (!model) {
          setApplied(null);
          setDraftFrom(undefined);
          setDraftTo(undefined);
          setOperator("on");
          return;
        }
        setApplied(model);
        setDraftFrom(model.dateFrom ?? undefined);
        setDraftTo(model.dateTo ?? undefined);
        setOperator(agTypeToOperator(model.type));
      },
    }),
    [applied],
  );

  return (
    <div className="grid min-w-[15rem] gap-2 p-3" dir={locale === "fa" ? "rtl" : "ltr"} data-testid="jalali-column-filter">
      <label className="grid gap-1 text-sm">
        <span className="text-xs text-muted">{locale === "fa" ? "عملگر" : "Operator"}</span>
        <select
          className="min-h-9 rounded-ds border border-border bg-surface px-2 text-sm"
          value={operator}
          onChange={(event) => setOperator(event.target.value as DateOperator)}
        >
          <option value="on">{ops.on}</option>
          <option value="before">{ops.before}</option>
          <option value="after">{ops.after}</option>
          <option value="between">{ops.between}</option>
        </select>
      </label>
      <label className="grid gap-1 text-sm">
        <span className="text-xs text-muted">{operator === "between" ? (locale === "fa" ? "از تاریخ" : "From") : locale === "fa" ? "تاریخ" : "Date"}</span>
        <JalaliDatePicker
          ariaLabel={locale === "fa" ? "تاریخ" : "Date"}
          locale={locale}
          value={draftFrom}
          commitOnChange={false}
          onDraftIsoChange={setDraftFrom}
          onCommit={(iso) => {
            setDraftFrom(iso);
            if (iso && operator !== "between") applyFilter();
          }}
        />
      </label>
      {operator === "between" ? (
        <label className="grid gap-1 text-sm">
          <span className="text-xs text-muted">{locale === "fa" ? "تا تاریخ" : "To"}</span>
          <JalaliDatePicker
            ariaLabel={locale === "fa" ? "تا تاریخ" : "To date"}
            locale={locale}
            value={draftTo}
            commitOnChange={false}
            onDraftIsoChange={setDraftTo}
            onCommit={(iso) => {
              setDraftTo(iso);
              if (iso && draftFrom) applyFilter();
            }}
          />
        </label>
      ) : null}
      <div className="flex flex-wrap gap-2 pt-1">
        <button type="button" className="min-h-9 rounded-ds bg-primary px-3 text-sm text-primary-foreground" onClick={applyFilter}>
          {locale === "fa" ? "اعمال" : "Apply"}
        </button>
        <button type="button" className="min-h-9 rounded-ds border border-border bg-surface px-3 text-sm" onClick={resetFilter}>
          {locale === "fa" ? "پاک کردن" : "Clear"}
        </button>
      </div>
    </div>
  );
});
