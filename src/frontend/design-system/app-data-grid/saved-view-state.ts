import type { ColumnState, GridApi } from "ag-grid-community";
import type { FilterModel } from "ag-grid-community";
import type { GridColumnLayout, GridFilterValue, SavedGridView } from "../data-grid/types";
import { toAgFilterModel } from "./ag-filter-mapper.ts";

/** وضعیت ستون AG را به layout ذخیره‌شدهٔ پروژه تبدیل می‌کند. */
export function captureColumnLayoutFromApi<T>(
  api: GridApi<T> | null | undefined,
  fallbackColumnIds: string[],
): GridColumnLayout {
  if (!api) {
    return { order: fallbackColumnIds, visibility: {}, widths: {} };
  }

  const state = api.getColumnState().filter((col) => col.colId && col.colId !== "actions");
  const order = state.map((col) => col.colId!).filter(Boolean);
  const visibility = Object.fromEntries(state.map((col) => [col.colId!, !col.hide]));
  const widths = Object.fromEntries(
    state.filter((col) => col.colId && col.width != null).map((col) => [col.colId!, col.width!]),
  );

  return {
    order: order.length > 0 ? order : fallbackColumnIds,
    visibility,
    widths,
  };
}

/** SavedGridView را به state قابل اعمال AG Grid تبدیل می‌کند (ترتیب، نمایش، عرض، مرتب‌سازی). */
export function buildAgColumnApplyState(view: SavedGridView): ColumnState[] {
  const sortByColumn = Object.fromEntries(view.sorts.map((sort) => [sort.columnId, sort.direction]));

  return view.layout.order.map((colId) => ({
    colId,
    hide: view.layout.visibility[colId] === false,
    width: view.layout.widths[colId],
    sort: sortByColumn[colId] ?? null,
  }));
}

/** فیلترهای ذخیره‌شده را به AG FilterModel نگاشت می‌کند — فیلدهای پیشرفته از AG مستثنا می‌شوند. */
export function agFilterModelForSavedView(
  filters: Record<string, GridFilterValue>,
  advancedFieldIds: ReadonlySet<string>,
): FilterModel | null {
  const model = toAgFilterModel(filters, { excludeFields: advancedFieldIds });
  return Object.keys(model).length > 0 ? model : null;
}

/** قرارداد validation: نمای ذخیره‌شده باید layout/filters/sorts/pageSize را round-trip کند. */
export function normalizeSavedViewForPersistence(view: SavedGridView): SavedGridView {
  return JSON.parse(JSON.stringify(view)) as SavedGridView;
}
