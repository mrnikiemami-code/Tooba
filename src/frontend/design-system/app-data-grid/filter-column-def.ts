import type { GridColumnDef, GridFilterKind } from "../data-grid/types";

/** تعریف ستون فیلتر پیشرفته Community-safe — جدا از ColDef خام AG Grid. */
export interface AppGridFilterColumnDef {
  id: string;
  header: string;
  filterKind: GridFilterKind;
  enumOptions?: { value: string; label: string }[];
}

/** برای استفاده از FilterControl legacy بدون ردیف واقعی. */
export function toFilterControlColumn(col: AppGridFilterColumnDef): GridColumnDef<unknown> {
  return {
    id: col.id,
    header: col.header,
    filterKind: col.filterKind,
    enumOptions: col.enumOptions,
    filterable: true,
    accessor: () => null,
    width: 120,
    minWidth: 80,
    maxWidth: 480,
  };
}

export const COMMUNITY_AG_FILTERS = new Set([
  "agTextColumnFilter",
  "agNumberColumnFilter",
  "agDateColumnFilter",
]);

/** فیلترهای Enterprise ممنوع در Community wrapper. */
export const FORBIDDEN_AG_FILTERS = new Set([
  "agSetColumnFilter",
  "agMultiColumnFilter",
]);

export function assertCommunityColumnFilter(filter: string | boolean | undefined): void {
  if (typeof filter !== "string") {
    return;
  }

  if (FORBIDDEN_AG_FILTERS.has(filter)) {
    throw new Error(`Enterprise-only AG Grid filter is forbidden: ${filter}`);
  }
}
