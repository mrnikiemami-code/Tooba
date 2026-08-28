import type { GridFilterRequest, GridQueryRequest, GridSortRequest } from "./types";
import type { GridFilterValue, GridServerQuery, GridSort } from "../data-grid/types";

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
      return {
        field,
        operator: value.operator,
        value: String(value.value),
        valueTo: value.valueTo !== undefined ? String(value.valueTo) : undefined,
      };
    case "enum":
      return {
        field,
        operator: "in",
        values: value.values,
      };
    case "status":
      return {
        field,
        operator: value.values.length === 1 ? "equals" : "in",
        value: value.values.length === 1 ? value.values[0] : undefined,
        values: value.values.length === 1 ? undefined : value.values,
      };
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
};

export function mergeSorts(sorts: GridSort[]): GridSort[] {
  if (sorts.length === 0) {
    return DEFAULT_GRID_QUERY.sorts;
  }
  return sorts;
}
