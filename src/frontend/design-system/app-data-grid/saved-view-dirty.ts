import type { GridColumnLayout, GridServerQuery, GridSort, SavedGridView } from "../data-grid/types.ts";
import { isFilterActive } from "../data-grid/serialize.ts";
import {
  isAdvancedFilterExpressionActive,
  normalizeAdvancedFilterExpression,
} from "./advanced-filter-expression.ts";
import { filtersEqual } from "./filter-commit.ts";

/** آیا فیلتر/جستجوی فعال transient روی گرید وجود دارد؟ */
export function hasActiveTransientFilters(query: GridServerQuery): boolean {
  if (query.search?.trim()) return true;
  if (isAdvancedFilterExpressionActive(query.advancedFilter)) return true;
  return Object.values(query.filters).some(isFilterActive);
}

function layoutKey(layout: GridColumnLayout): string {
  return JSON.stringify({
    order: layout.order,
    visibility: layout.visibility,
    widths: layout.widths,
  });
}

function sortsKey(sorts: GridSort[]): string {
  return JSON.stringify(sorts);
}

/** مقایسهٔ وضعیت فعلی گرید با snapshot نمای ذخیره‌شده — برای dirty indicator. */
export function isSelectedViewDirty(
  view: SavedGridView,
  query: GridServerQuery,
  layout: GridColumnLayout,
): boolean {
  const viewAdvanced = normalizeAdvancedFilterExpression(
    view.advancedFilterExpression ?? { conditions: [], connectors: [] },
  );
  const queryAdvanced = normalizeAdvancedFilterExpression(query.advancedFilter);

  if (!filtersEqual(view.filters, query.filters)) return true;
  if (JSON.stringify(viewAdvanced) !== JSON.stringify(queryAdvanced)) return true;
  if ((view.search ?? "") !== (query.search ?? "")) return true;
  if (sortsKey(view.sorts) !== sortsKey(query.sorts)) return true;
  if (view.pageSize !== query.pageSize) return true;
  if (layoutKey(view.layout) !== layoutKey(layout)) return true;
  return false;
}

export type ViewApplyResolution = {
  query: GridServerQuery;
  /** AG filter model باید از نمای جدید restore شود؟ */
  restoreSavedFilters: boolean;
  /** مقدار draft جستجو در input */
  searchDraft: string;
};

/**
 * هنگام تعویض Saved View:
 * - layout/sort/pageSize از view
 * - اگر فیلتر transient فعال است، فیلتر/جستجوی فعلی حفظ می‌شود
 */
export function resolveViewApplyQuery(
  view: SavedGridView,
  current: GridServerQuery,
  defaultSorts: GridSort[],
  currentSearchDraft: string,
): ViewApplyResolution {
  const preserveTransient = hasActiveTransientFilters(current);
  const viewAdvanced = normalizeAdvancedFilterExpression(
    view.advancedFilterExpression ?? { conditions: [], connectors: [] },
  );

  if (preserveTransient) {
    return {
      query: {
        page: 1,
        pageSize: view.pageSize,
        sorts: view.sorts.length > 0 ? view.sorts : defaultSorts,
        filters: current.filters,
        advancedFilter: normalizeAdvancedFilterExpression(current.advancedFilter),
        search: current.search,
      },
      restoreSavedFilters: false,
      searchDraft: currentSearchDraft,
    };
  }

  return {
    query: {
      page: 1,
      pageSize: view.pageSize,
      sorts: view.sorts.length > 0 ? view.sorts : defaultSorts,
      filters: view.filters,
      advancedFilter: viewAdvanced,
      search: view.search,
    },
    restoreSavedFilters: true,
    searchDraft: view.search ?? "",
  };
}
