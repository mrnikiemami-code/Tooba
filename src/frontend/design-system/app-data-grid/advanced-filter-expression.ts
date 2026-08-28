import type {
  GridFilterValue,
  AdvancedFilterExpression,
  AdvancedFilterCondition,
  AdvancedFilterConnector,
} from "../data-grid/types.ts";
import { isFilterActive } from "../data-grid/serialize.ts";

/** اتصال‌دهندهٔ منطقی بین شرط‌های فیلتر پیشرفته — left-to-right ارزیابی می‌شود. */
export type { AdvancedFilterConnector, AdvancedFilterCondition, AdvancedFilterExpression } from "../data-grid/types.ts";

export const EMPTY_ADVANCED_FILTER: AdvancedFilterExpression = { conditions: [], connectors: [] };

/** invariant: connectors.length === max(conditions.length - 1, 0) */
export function normalizeAdvancedFilterExpression(
  expression: AdvancedFilterExpression | undefined | null,
): AdvancedFilterExpression {
  const conditions = (expression?.conditions ?? []).filter((c) => c.field);
  const needed = Math.max(conditions.length - 1, 0);
  const connectors = (expression?.connectors ?? []).slice(0, needed);
  while (connectors.length < needed) {
    connectors.push("and");
  }
  return { conditions, connectors };
}

export function validateAdvancedFilterExpression(expression: AdvancedFilterExpression): string | null {
  const conditions = expression?.conditions ?? [];
  const connectors = expression?.connectors ?? [];
  if (connectors.length !== Math.max(conditions.length - 1, 0)) {
    return "connector-count-mismatch";
  }
  for (const connector of connectors) {
    if (connector !== "and" && connector !== "or") {
      return "invalid-connector";
    }
  }
  return null;
}

/** ارزیابی left-to-right: ((A op1 B) op2 C) — deterministic، بدون SQL precedence. */
export function evaluateAdvancedFilterLeftToRight(
  conditionResults: boolean[],
  connectors: AdvancedFilterConnector[],
): boolean {
  if (conditionResults.length === 0) {
    return true;
  }
  let acc = conditionResults[0]!;
  for (let index = 1; index < conditionResults.length; index++) {
    const connector = connectors[index - 1] ?? "and";
    acc = connector === "and" ? acc && conditionResults[index]! : acc || conditionResults[index]!;
  }
  return acc;
}

export function activeAdvancedConditions(expression: AdvancedFilterExpression | undefined): AdvancedFilterCondition[] {
  return normalizeAdvancedFilterExpression(expression).conditions.filter((c) => isFilterActive(c.value));
}

export function isAdvancedFilterExpressionActive(expression: AdvancedFilterExpression | undefined): boolean {
  return activeAdvancedConditions(expression).length > 0;
}

export function createAdvancedCondition(field: string, value?: GridFilterValue): AdvancedFilterCondition {
  return {
    id: crypto.randomUUID(),
    field,
    value: value ?? { kind: "text", operator: "contains", query: "" },
  };
}

/** migration v2 advancedFilters record → v3 ordered expression (همه AND). */
export function migrateAdvancedFiltersRecord(
  advancedFilters: Record<string, GridFilterValue> | undefined,
  fieldOrder: readonly string[],
): AdvancedFilterExpression {
  if (!advancedFilters) {
    return EMPTY_ADVANCED_FILTER;
  }
  const keys = [
    ...fieldOrder.filter((key) => isFilterActive(advancedFilters[key])),
    ...Object.keys(advancedFilters).filter((key) => isFilterActive(advancedFilters[key]!) && !fieldOrder.includes(key)),
  ];
  const conditions = keys.map((field) => ({
    id: crypto.randomUUID(),
    field,
    value: advancedFilters[field]!,
  }));
  return normalizeAdvancedFilterExpression({
    conditions,
    connectors: Array(Math.max(conditions.length - 1, 0)).fill("and"),
  });
}

export function serializeAdvancedFilterExpression(expression: AdvancedFilterExpression): string {
  return JSON.stringify(normalizeAdvancedFilterExpression(expression));
}

export function deserializeAdvancedFilterExpression(raw: string): AdvancedFilterExpression {
  return normalizeAdvancedFilterExpression(JSON.parse(raw) as AdvancedFilterExpression);
}
