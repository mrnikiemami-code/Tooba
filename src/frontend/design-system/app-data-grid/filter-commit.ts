import type { GridFilterValue, GridServerQuery } from "../data-grid/types";

/** مقایسهٔ پایدار فیلترها برای جلوگیری از درخواست تکراری. */
export function normalizeFiltersRecord(filters: Record<string, GridFilterValue>): string {
  const keys = Object.keys(filters).sort();
  const normalized: Record<string, GridFilterValue> = {};
  for (const key of keys) {
    normalized[key] = filters[key]!;
  }
  return JSON.stringify(normalized);
}

export function filtersEqual(
  left: Record<string, GridFilterValue>,
  right: Record<string, GridFilterValue>,
): boolean {
  return normalizeFiltersRecord(left) === normalizeFiltersRecord(right);
}

export function gridQueryCommitKey(query: GridServerQuery): string {
  return JSON.stringify({
    page: query.page,
    filters: normalizeFiltersRecord(query.filters),
    search: query.search ?? null,
    advancedFilter: query.advancedFilter ?? null,
    sorts: query.sorts,
    pageSize: query.pageSize,
  });
}

export function shouldCommitGridQuery(current: GridServerQuery, next: GridServerQuery): boolean {
  return gridQueryCommitKey(current) !== gridQueryCommitKey(next);
}

/** پارامترهای پیش‌فرض فیلتر ستونی — اعمال فقط با Enter/Apply. */
export const COLUMN_FILTER_APPLY_PARAMS = {
  buttons: ["apply", "reset"] as const,
  closeOnApply: true,
  debounceMs: 0,
};
