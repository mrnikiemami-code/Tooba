"use client";

import { Button, Select } from "../primitives/core";
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

export function AdvancedFilterBuilder({
  columns,
  expression,
  onChange,
  locale = "fa",
  andLabel,
  orLabel,
  addLabel,
  removeLabel,
  fieldLabel,
}: {
  columns: AppGridFilterColumnDef[];
  expression: AdvancedFilterExpression;
  onChange: (expression: AdvancedFilterExpression) => void;
  locale?: "fa" | "en";
  andLabel: string;
  orLabel: string;
  addLabel: string;
  removeLabel: string;
  fieldLabel: string;
}) {
  const normalized = normalizeAdvancedFilterExpression(expression);
  const columnById = Object.fromEntries(columns.map((column) => [column.id, column]));

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

  function addCondition() {
    const first = columns[0];
    if (!first) return;
    updateExpression({
      conditions: [...normalized.conditions, createAdvancedCondition(first.id)],
      connectors: [...normalized.connectors, "and"],
    });
  }

  const rows =
    normalized.conditions.length > 0
      ? normalized.conditions
      : [createAdvancedCondition(columns[0]?.id ?? "title")];

  return (
    <div className="flex flex-col gap-3" data-testid="advanced-filter-builder">
      {rows.map((condition, index) => {
        const column = columnById[condition.field] ?? columns[0];
        if (!column) return null;
        return (
          <div key={condition.id} className="flex flex-col gap-2 rounded-ds border border-border bg-surface p-3">
            {index > 0 ? (
              <div className="flex flex-wrap items-center gap-2" role="group" aria-label={andLabel}>
                <button
                  type="button"
                  className={`min-h-9 rounded-full px-3 text-sm font-medium ${
                    (normalized.connectors[index - 1] ?? "and") === "and"
                      ? "bg-primary text-primary-foreground"
                      : "border border-border bg-secondary"
                  }`}
                  aria-pressed={(normalized.connectors[index - 1] ?? "and") === "and"}
                  onClick={() => setConnector(index - 1, "and")}
                >
                  {andLabel}
                </button>
                <button
                  type="button"
                  className={`min-h-9 rounded-full px-3 text-sm font-medium ${
                    normalized.connectors[index - 1] === "or"
                      ? "bg-primary text-primary-foreground"
                      : "border border-border bg-secondary"
                  }`}
                  aria-pressed={normalized.connectors[index - 1] === "or"}
                  onClick={() => setConnector(index - 1, "or")}
                >
                  {orLabel}
                </button>
              </div>
            ) : null}
            <div className="flex flex-col gap-2 sm:flex-row sm:items-start">
              <label className="min-w-[8rem] text-sm">
                {fieldLabel}
                <Select
                  aria-label={fieldLabel}
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
              <div className="min-w-0 flex-1">
                {column.filterKind === "date" && locale === "fa" ? (
                  <JalaliDateFilterControl
                    header={column.header}
                    locale={locale}
                    value={condition.value}
                    onChange={(value) => updateCondition(condition.id, { value })}
                  />
                ) : (
                  <FilterControl
                    column={toFilterControlColumn(column)}
                    value={condition.value}
                    onChange={(value) => updateCondition(condition.id, { value })}
                  />
                )}
              </div>
              <Button type="button" tone="ghost" aria-label={removeLabel} onClick={() => removeCondition(condition.id)}>
                {removeLabel}
              </Button>
            </div>
          </div>
        );
      })}
      <Button type="button" tone="secondary" onClick={addCondition}>
        {addLabel}
      </Button>
    </div>
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
      return { kind: kind, operator: "in", values: [] };
    default:
      return { kind: "text", operator: "contains", query: "" };
  }
}
