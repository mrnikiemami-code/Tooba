import type {
  GridAdvancedFilterCondition,
  GridAdvancedFilterExpression,
  GridFilterRequest,
  GridQueryRequest,
  GridSortRequest,
} from "./types.ts";
import type { GridFilterValue, GridServerQuery, GridSort } from "../data-grid/types.ts";
import {
  activeAdvancedConditions,
  normalizeAdvancedFilterExpression,
  type AdvancedFilterCondition,
  type AdvancedFilterExpression,
} from "./advanced-filter-expression.ts";

/** تبدیل GridServerQuery UI به قرارداد Host. AG Grid state اینجا نرمال می‌شود. */
export function toHostGridQuery(query: GridServerQuery): GridQueryRequest {
  return {
    page: query.page,
    pageSize: query.pageSize,
    search: query.search?.trim() || undefined,
    sort: query.sorts.map(
      (sort): GridSortRequest => ({
        field: sort.columnId,
        direction: sort.direction,
      }),
    ),
    filters: Object.entries(query.filters).map(([field, value]) => mapFilter(field, value)),
    advancedFilter: mapAdvancedFilterExpression(query.advancedFilter),
  };
}

function mapAdvancedFilterExpression(
  expression: AdvancedFilterExpression | undefined,
): GridAdvancedFilterExpression | undefined {
  const active = activeAdvancedConditions(expression);
  if (active.length === 0) {
    return undefined;
  }
  const normalized = normalizeAdvancedFilterExpression(expression);
  const activeIds = new Set(active.map((c) => c.id));
  const conditions: AdvancedFilterCondition[] = [];
  const connectors: ("and" | "or")[] = [];
  for (let index = 0; index < normalized.conditions.length; index++) {
    const condition = normalized.conditions[index]!;
    if (!activeIds.has(condition.id)) {
      continue;
    }
    if (conditions.length > 0) {
      connectors.push(normalized.connectors[index - 1] ?? "and");
    }
    conditions.push(condition);
  }
  return {
    conditions: conditions.map(mapAdvancedCondition),
    connectors,
  };
}

function mapAdvancedCondition(condition: AdvancedFilterCondition): GridAdvancedFilterCondition {
  const mapped = mapFilter(condition.field, condition.value);
  return {
    id: condition.id,
    field: mapped.field,
    operator: mapped.operator,
    value: mapped.value,
    valueTo: mapped.valueTo,
    values: mapped.values,
  };
}

function mapFilter(field: string, value: GridFilterValue): GridFilterRequest {
  switch (value.kind) {
    case "text":
      return {
        field,
        operator: value.operator,
        value: value.query,
      };
    case "number":
      if (value.operator === "blank" || value.operator === "notBlank") {
        return { field, operator: value.operator };
      }
      return {
        field,
        operator: value.operator,
        value: String(value.value),
        valueTo: value.valueTo !== undefined ? String(value.valueTo) : undefined,
      };
    case "enum":
      return {
        field,
        operator: value.operator ?? (value.values.length === 1 ? "equals" : "in"),
        value: value.values.length === 1 ? value.values[0] : undefined,
        values: value.values.length === 1 ? undefined : value.values,
      };
    case "status": {
      const operator = value.operator ?? (value.values.length === 1 ? "equals" : "in");
      if (operator === "in" || operator === "notIn") {
        return { field, operator, values: value.values };
      }
      return {
        field,
        operator,
        value: value.values[0],
      };
    }
    case "date":
      return {
        field,
        operator: value.operator,
        value: value.iso,
        valueTo: value.isoTo,
      };
    case "boolean":
      return {
        field,
        operator: "equals",
        value: value.state,
      };
    default:
      return { field, operator: "contains", value: "" };
  }
}

export interface HostGridPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function fromHostGridPage<TItem, TRow>(
  page: HostGridPage<TItem>,
  mapRow: (item: TItem) => TRow,
): { rows: TRow[]; total: number } {
  return {
    rows: page.items.map(mapRow),
    total: page.totalCount,
  };
}

export const DEFAULT_GRID_QUERY: GridServerQuery = {
  page: 1,
  pageSize: 20,
  sorts: [{ columnId: "updatedAt", direction: "desc" }],
  filters: {},
  advancedFilter: { conditions: [], connectors: [] },
};

export function mergeSorts(sorts: GridSort[]): GridSort[] {
  if (sorts.length === 0) {
    return DEFAULT_GRID_QUERY.sorts;
  }
  return sorts;
}
