import type { ColumnState, GridApi } from "ag-grid-community";
import type { FilterModel } from "ag-grid-community";
import type { GridColumnLayout, GridFilterValue, SavedGridView } from "../data-grid/types.ts";
import { SAVED_GRID_VIEW_SCHEMA_VERSION as SCHEMA_VERSION } from "../data-grid/types.ts";
import { isFilterActive } from "../data-grid/serialize.ts";
import { toAgFilterModel } from "./ag-filter-mapper.ts";

export { SCHEMA_VERSION as SAVED_VIEW_SCHEMA_VERSION };

export interface SavedViewSanitizeContext {
  knownColumnIds: ReadonlySet<string>;
  knownFilterFields: ReadonlySet<string>;
  advancedFieldIds: ReadonlySet<string>;
  enumOptionsByField: Readonly<Record<string, ReadonlySet<string>>>;
}

/** فیلترهای advanced را از کل state جدا می‌کند — AG و drawer مستقل می‌مانند. */
export function partitionFilters(
  filters: Record<string, GridFilterValue>,
  advancedFieldIds: ReadonlySet<string>,
): { advancedFilters: Record<string, GridFilterValue>; columnFilters: Record<string, GridFilterValue> } {
  const advancedFilters: Record<string, GridFilterValue> = {};
  const columnFilters: Record<string, GridFilterValue> = {};
  for (const [field, value] of Object.entries(filters)) {
    if (!isFilterActive(value)) continue;
    if (advancedFieldIds.has(field)) {
      advancedFilters[field] = value;
    } else {
      columnFilters[field] = value;
    }
  }
  return { advancedFilters, columnFilters };
}

/** advanced + column filters را برای server query ادغام می‌کند (advanced بر column غلبه دارد). */
export function mergeSavedViewFilters(view: SavedGridView): Record<string, GridFilterValue> {
  const advanced = view.advancedFilters ?? {};
  return { ...view.filters, ...advanced };
}

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
export function buildAgColumnApplyState(view: SavedGridView, knownColumnIds?: ReadonlySet<string>): ColumnState[] {
  const sortByColumn = Object.fromEntries(view.sorts.map((sort) => [sort.columnId, sort.direction]));
  const order = knownColumnIds
    ? view.layout.order.filter((colId) => knownColumnIds.has(colId))
    : view.layout.order;

  return order.map((colId) => ({
    colId,
    hide: view.layout.visibility[colId] === false,
    width: view.layout.widths[colId],
    sort: sortByColumn[colId] ?? null,
  }));
}

/** فیلترهای AG Community را از state کامل استخراج می‌کند — advanced در AG set نمی‌شوند. */
export function agFilterModelForSavedView(
  filters: Record<string, GridFilterValue>,
  advancedFieldIds: ReadonlySet<string>,
): FilterModel | null {
  const model = toAgFilterModel(filters, { excludeFields: advancedFieldIds });
  return Object.keys(model).length > 0 ? model : null;
}

function sanitizeFilterValue(
  field: string,
  value: GridFilterValue,
  ctx: SavedViewSanitizeContext,
): GridFilterValue | undefined {
  if (value.kind === "status" || value.kind === "enum") {
    const allowed = ctx.enumOptionsByField[field];
    const values = allowed ? value.values.filter((item) => allowed.has(item)) : value.values;
    if (values.length === 0) return undefined;
    const operator = value.operator ?? (values.length === 1 ? "equals" : "in");
    if ((operator === "equals" || operator === "notEqual") && values.length > 1) {
      return { kind: value.kind, operator, values: [values[0]!] };
    }
    return { kind: value.kind, operator, values };
  }
  return value;
}

function sanitizeFilters(
  filters: Record<string, GridFilterValue>,
  ctx: SavedViewSanitizeContext,
): Record<string, GridFilterValue> {
  const next: Record<string, GridFilterValue> = {};
  for (const [field, value] of Object.entries(filters)) {
    if (!ctx.knownFilterFields.has(field)) continue;
    const sanitized = sanitizeFilterValue(field, value, ctx);
    if (sanitized && isFilterActive(sanitized)) {
      next[field] = sanitized;
    }
  }
  return next;
}

function sanitizeLayout(layout: GridColumnLayout, knownColumnIds: ReadonlySet<string>): GridColumnLayout {
  const order = layout.order.filter((colId) => knownColumnIds.has(colId));
  for (const colId of knownColumnIds) {
    if (!order.includes(colId)) order.push(colId);
  }
  const visibility = Object.fromEntries(
    Object.entries(layout.visibility).filter(([colId]) => knownColumnIds.has(colId)),
  );
  const widths = Object.fromEntries(Object.entries(layout.widths).filter(([colId]) => knownColumnIds.has(colId)));
  return { order, visibility, widths };
}

/** migration v1→v2 + حذف ایمن ستون/فیلتر/enum ناشناخته. */
export function migrateSavedView(view: SavedGridView): SavedGridView {
  const cloned = JSON.parse(JSON.stringify(view)) as SavedGridView;
  if (!cloned.schemaVersion || cloned.schemaVersion < SCHEMA_VERSION) {
    cloned.schemaVersion = SCHEMA_VERSION;
    if (!cloned.advancedFilters) {
      cloned.advancedFilters = {};
    }
  }
  return cloned;
}

export function sanitizeSavedView(view: SavedGridView, ctx: SavedViewSanitizeContext): SavedGridView {
  const migrated = migrateSavedView(view);
  const mergedFilters = sanitizeFilters(mergeSavedViewFilters(migrated), ctx);
  const advancedFilters = sanitizeFilters(
    migrated.advancedFilters && Object.keys(migrated.advancedFilters).length > 0
      ? migrated.advancedFilters
      : Object.fromEntries(Object.entries(mergedFilters).filter(([key]) => ctx.advancedFieldIds.has(key))),
    ctx,
  );
  const columnOnlyFilters = Object.fromEntries(
    Object.entries(mergedFilters).filter(([key]) => !ctx.advancedFieldIds.has(key)),
  );

  return normalizeSavedViewForPersistence({
    ...migrated,
    filters: columnOnlyFilters,
    advancedFilters,
    sorts: migrated.sorts.filter((sort) => ctx.knownColumnIds.has(sort.columnId)),
    layout: sanitizeLayout(migrated.layout, ctx.knownColumnIds),
    search: migrated.search?.trim() || undefined,
    schemaVersion: SCHEMA_VERSION,
  });
}

/** آماده‌سازی برای persistence — schema + advanced partition. */
export function prepareSavedViewForPersistence(
  view: Omit<SavedGridView, "schemaVersion" | "advancedFilters"> & {
    advancedFilters?: Record<string, GridFilterValue>;
  },
  advancedFieldIds: ReadonlySet<string>,
): SavedGridView {
  const merged = { ...view.filters, ...(view.advancedFilters ?? {}) };
  const { advancedFilters, columnFilters } = partitionFilters(merged, advancedFieldIds);
  return normalizeSavedViewForPersistence({
    ...view,
    schemaVersion: SCHEMA_VERSION,
    filters: columnFilters,
    advancedFilters,
  });
}

/** قرارداد validation: نمای ذخیره‌شده باید layout/filters/sorts/pageSize را round-trip کند. */
export function normalizeSavedViewForPersistence(view: SavedGridView): SavedGridView {
  return JSON.parse(JSON.stringify(view)) as SavedGridView;
}

/** فیلدهای advanced به ترتیب ثابت — AND ضمنی در server GridQuery. */
export function advancedFilterFieldOrder(
  advancedFilters: Record<string, GridFilterValue>,
  preferredOrder: readonly string[],
): string[] {
  const keys = Object.keys(advancedFilters).filter((key) => isFilterActive(advancedFilters[key]!));
  return [...preferredOrder.filter((key) => keys.includes(key)), ...keys.filter((key) => !preferredOrder.includes(key))];
}
