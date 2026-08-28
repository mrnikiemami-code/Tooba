"use client";

import { Fragment } from "react";
import { Input, Select } from "../primitives/core";
import { FilterControl } from "../data-grid/FilterControl";
import type { GridFilterValue } from "../data-grid/types";
import type { AppGridFilterColumnDef } from "./filter-column-def";
import { toFilterControlColumn } from "./filter-column-def";
import { JalaliDateFilterControl } from "./JalaliDateFilterControl";
import {
  createAdvancedCondition,
  normalizeAdvancedFilterExpression,
  type AdvancedFilterConnector,
  type AdvancedFilterExpression,
} from "./advanced-filter-expression";
import { filterOperatorLabelsFor } from "../data-grid/messages";

export function AdvancedFilterBuilder({
  columns,
  expression,
  onChange,
  locale = "fa",
  andLabel,
  orLabel,
  removeLabel,
  fieldLabel,
  operatorLabel,
  valueLabel,
}: {
  columns: AppGridFilterColumnDef[];
  expression: AdvancedFilterExpression;
  onChange: (expression: AdvancedFilterExpression) => void;
  locale?: "fa" | "en";
  andLabel: string;
  orLabel: string;
  removeLabel: string;
  fieldLabel: string;
  operatorLabel: string;
  valueLabel: string;
}) {
  const normalized = normalizeAdvancedFilterExpression(expression);
  const columnById = Object.fromEntries(columns.map((column) => [column.id, column]));
  const ops = filterOperatorLabelsFor(locale);

  function updateExpression(next: AdvancedFilterExpression) {
    onChange(normalizeAdvancedFilterExpression(next));
  }

  function updateCondition(id: string, patch: Partial<{ field: string; value: GridFilterValue }>) {
    updateExpression({
      ...normalized,
      conditions: normalized.conditions.map((condition) =>
        condition.id === id ? { ...condition, ...patch } : condition,
      ),
    });
  }

  function setConnector(index: number, connector: AdvancedFilterConnector) {
    const connectors = [...normalized.connectors];
    connectors[index] = connector;
    updateExpression({ ...normalized, connectors });
  }

  function removeCondition(id: string) {
    const index = normalized.conditions.findIndex((c) => c.id === id);
    if (index < 0) return;
    const conditions = normalized.conditions.filter((c) => c.id !== id);
    const connectors = normalized.connectors.filter((_, i) => i !== index && i !== index - 1);
    updateExpression({ conditions, connectors });
  }

  const rows =
    normalized.conditions.length > 0
      ? normalized.conditions
      : [createAdvancedCondition(columns[0]?.id ?? "title")];

  return (
    <div className="flex flex-col gap-4" data-testid="advanced-filter-builder">
      {rows.map((condition, index) => {
        const column = columnById[condition.field] ?? columns[0];
        if (!column) return null;
        const connector = normalized.connectors[index - 1] ?? "and";
        return (
          <Fragment key={condition.id}>
            {index > 0 ? (
              <div className="flex justify-center" data-advanced-filter-connector role="group" aria-label={andLabel}>
                <div className="inline-flex rounded-full border border-border bg-secondary p-0.5">
                  <button
                    type="button"
                    className={`min-h-[2.75rem] min-w-[3.25rem] rounded-full px-4 text-sm font-medium ${
                      connector === "and" ? "bg-primary text-primary-foreground shadow-sm" : "text-foreground"
                    }`}
                    aria-pressed={connector === "and"}
                    onClick={() => setConnector(index - 1, "and")}
                  >
                    {andLabel}
                  </button>
                  <button
                    type="button"
                    className={`min-h-[2.75rem] min-w-[3.25rem] rounded-full px-4 text-sm font-medium ${
                      connector === "or" ? "bg-primary text-primary-foreground shadow-sm" : "text-foreground"
                    }`}
                    aria-pressed={connector === "or"}
                    onClick={() => setConnector(index - 1, "or")}
                  >
                    {orLabel}
                  </button>
                </div>
              </div>
            ) : null}

            <article
              className="rounded-ds border border-border bg-surface-elevated p-4 shadow-sm md:p-5"
              data-advanced-filter-card
            >
              <div className="grid gap-4 md:grid-cols-3">
                <label className="grid gap-1.5 text-sm">
                  <span className="text-xs font-medium text-muted">{fieldLabel}</span>
                  <Select
                    aria-label={fieldLabel}
                    className="min-h-[2.75rem]"
                    value={condition.field}
                    onChange={(event) => {
                      const nextField = event.target.value;
                      const nextColumn = columnById[nextField];
                      updateCondition(condition.id, {
                        field: nextField,
                        value: nextColumn
                          ? defaultValueForKind(nextColumn.filterKind)
                          : condition.value,
                      });
                    }}
                  >
                    {columns.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.header}
                      </option>
                    ))}
                  </Select>
                </label>

                <label className="grid gap-1.5 text-sm">
                  <span className="text-xs font-medium text-muted">{operatorLabel}</span>
                  <ConditionOperatorSelect
                    column={column}
                    value={condition.value}
                    locale={locale}
                    ops={ops}
                    onChange={(value) => updateCondition(condition.id, { value })}
                  />
                </label>

                <div className="grid min-w-0 gap-1.5 text-sm">
                  <span className="text-xs font-medium text-muted">{valueLabel}</span>
                  <div className="min-h-[2.75rem]">
                    {column.filterKind === "date" && locale === "fa" ? (
                      <AdvancedDateValue
                        column={column}
                        value={condition.value}
                        onChange={(value) => updateCondition(condition.id, { value })}
                        locale={locale}
                      />
                    ) : (
                      <CompactFilterValue
                        column={column}
                        value={condition.value}
                        onChange={(value) => updateCondition(condition.id, { value })}
                      />
                    )}
                  </div>
                </div>
              </div>

              <div className="mt-4 flex justify-end">
                <button
                  type="button"
                  className="inline-flex min-h-[2.75rem] items-center gap-1 rounded-ds px-3 text-sm text-danger hover:bg-danger/5"
                  aria-label={removeLabel}
                  data-advanced-filter-delete
                  onClick={() => removeCondition(condition.id)}
                >
                  <span aria-hidden>🗑</span>
                  {removeLabel}
                </button>
              </div>
            </article>
          </Fragment>
        );
      })}
    </div>
  );
}

function ConditionOperatorSelect({
  column,
  value,
  locale,
  ops,
  onChange,
}: {
  column: AppGridFilterColumnDef;
  value?: GridFilterValue;
  locale: "fa" | "en";
  ops: ReturnType<typeof filterOperatorLabelsFor>;
  onChange: (value: GridFilterValue) => void;
}) {
  if (column.filterKind === "text") {
    const operator = value?.kind === "text" ? value.operator : "contains";
    return (
      <Select
        aria-label={locale === "fa" ? "عملگر" : "Operator"}
        className="min-h-[2.75rem] w-full"
        value={operator}
        onChange={(event) =>
          onChange({
            kind: "text",
            operator: event.target.value as "contains" | "equals" | "startsWith",
            query: value?.kind === "text" ? value.query : "",
          })
        }
      >
        <option value="contains">{ops.contains}</option>
        <option value="equals">{ops.equals}</option>
        <option value="startsWith">{ops.startsWith}</option>
      </Select>
    );
  }

  if (column.filterKind === "number" || column.filterKind === "money") {
    const operator = value?.kind === "number" || value?.kind === "money" ? value.operator : "equals";
    return (
      <Select
        aria-label={locale === "fa" ? "عملگر" : "Operator"}
        className="min-h-[2.75rem] w-full"
        value={operator}
        onChange={(event) => {
          const nextOp = event.target.value as typeof operator;
          const amount = value?.kind === "number" ? value.value : 0;
          onChange({ kind: "number", operator: nextOp, value: amount, valueTo: value?.kind === "number" ? value.valueTo : undefined });
        }}
      >
        <option value="equals">{ops.equals}</option>
        <option value="greaterThan">{ops.greaterThan}</option>
        <option value="lessThan">{ops.lessThan}</option>
        <option value="between">{ops.between}</option>
      </Select>
    );
  }

  if (column.filterKind === "date") {
    const operator = value?.kind === "date" ? value.operator : "on";
    return (
      <Select
        aria-label={locale === "fa" ? "عملگر" : "Operator"}
        className="min-h-[2.75rem] w-full"
        value={operator}
        onChange={(event) =>
          onChange({
            kind: "date",
            operator: event.target.value as "on" | "before" | "after" | "between",
            iso: value?.kind === "date" ? value.iso : new Date().toISOString(),
            isoTo: value?.kind === "date" ? value.isoTo : undefined,
          })
        }
      >
        <option value="on">{ops.on}</option>
        <option value="before">{ops.before}</option>
        <option value="after">{ops.after}</option>
        <option value="between">{ops.between}</option>
      </Select>
    );
  }

  return (
    <Select aria-label={locale === "fa" ? "عملگر" : "Operator"} className="min-h-[2.75rem] w-full" value="in" disabled>
      <option value="in">{ops.in}</option>
    </Select>
  );
}

function CompactFilterValue({
  column,
  value,
  onChange,
}: {
  column: AppGridFilterColumnDef;
  value?: GridFilterValue;
  onChange: (value: GridFilterValue) => void;
}) {
  if (column.filterKind === "text") {
    return (
      <Input
        className="min-h-[2.75rem] w-full"
        value={value?.kind === "text" ? value.query : ""}
        onChange={(event) =>
          onChange({
            kind: "text",
            operator: value?.kind === "text" ? value.operator : "contains",
            query: event.target.value,
          })
        }
      />
    );
  }

  if (column.filterKind === "number" || column.filterKind === "money") {
    const operator = value?.kind === "number" ? value.operator : "equals";
    const amount = value?.kind === "number" ? value.value : 0;
    return (
      <Input
        type="number"
        className="min-h-[2.75rem] w-full"
        value={String(amount)}
        onChange={(event) =>
          onChange({
            kind: "number",
            operator,
            value: Number(event.target.value),
            valueTo: value?.kind === "number" ? value.valueTo : undefined,
          })
        }
      />
    );
  }

  if (column.filterKind === "status" || column.filterKind === "enum") {
    const selected = value?.kind === "status" || value?.kind === "enum" ? value.values : [];
    const kind = column.filterKind;
    return (
      <Select
        className="min-h-[2.75rem] w-full"
        value={selected[0] ?? ""}
        onChange={(event) =>
          onChange(
            kind === "status"
              ? { kind: "status", operator: "in", values: event.target.value ? [event.target.value] : [] }
              : { kind: "enum", operator: "in", values: event.target.value ? [event.target.value] : [] },
          )
        }
      >
        <option value="">{column.filterKind === "status" ? "انتخاب وضعیت" : "انتخاب"}</option>
        {(column.enumOptions ?? []).map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </Select>
    );
  }

  return (
    <FilterControl
      column={toFilterControlColumn(column)}
      value={value}
      onChange={onChange}
    />
  );
}

function AdvancedDateValue({
  column,
  value,
  onChange,
  locale,
}: {
  column: AppGridFilterColumnDef;
  value?: GridFilterValue;
  onChange: (value: GridFilterValue) => void;
  locale: "fa" | "en";
}) {
  return (
    <JalaliDateFilterControl
      header={column.header}
      locale={locale}
      compact
      value={value}
      onChange={onChange}
    />
  );
}

function defaultValueForKind(kind: AppGridFilterColumnDef["filterKind"]): GridFilterValue {
  switch (kind) {
    case "number":
      return { kind: "number", operator: "equals", value: 0 };
    case "date":
      return { kind: "date", operator: "on", iso: new Date().toISOString() };
    case "status":
    case "enum":
      return { kind, operator: "in", values: [] };
    default:
      return { kind: "text", operator: "contains", query: "" };
  }
}
