import type { ColumnState, GridApi } from "ag-grid-community";
import type { FilterModel } from "ag-grid-community";
import type {
  AdvancedFilterExpression,
  GridColumnLayout,
  GridFilterValue,
  SavedGridView,
} from "../data-grid/types.ts";
import { SAVED_GRID_VIEW_SCHEMA_VERSION as SCHEMA_VERSION } from "../data-grid/types.ts";
import { isFilterActive } from "../data-grid/serialize.ts";
import { toAgFilterModel } from "./ag-filter-mapper.ts";
import {
  migrateAdvancedFiltersRecord,
  normalizeAdvancedFilterExpression,
} from "./advanced-filter-expression.ts";

export { SCHEMA_VERSION as SAVED_VIEW_SCHEMA_VERSION };

export interface SavedViewSanitizeContext {
  knownColumnIds: ReadonlySet<string>;
  knownFilterFields: ReadonlySet<string>;
  advancedFieldIds: ReadonlySet<string>;
  enumOptionsByField: Readonly<Record<string, ReadonlySet<string>>>;
  advancedFieldOrder: readonly string[];
}

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

export function mergeSavedViewFilters(view: SavedGridView): Record<string, GridFilterValue> {
  const column = { ...view.filters };
  const expression = view.advancedFilterExpression;
  if (expression) {
    for (const condition of expression.conditions) {
      if (isFilterActive(condition.value)) {
        column[condition.field] = condition.value;
      }
    }
    return column;
  }
  return { ...column, ...(view.advancedFilters ?? {}) };
}

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

function sanitizeAdvancedExpression(
  expression: AdvancedFilterExpression | undefined,
  ctx: SavedViewSanitizeContext,
): AdvancedFilterExpression {
  const normalized = normalizeAdvancedFilterExpression(expression);
  const conditions = normalized.conditions
    .filter((condition) => ctx.knownFilterFields.has(condition.field))
    .map((condition) => {
      const value = sanitizeFilterValue(condition.field, condition.value, ctx);
      return value && isFilterActive(value) ? { ...condition, value } : null;
    })
    .filter((condition): condition is (typeof normalized.conditions)[number] => condition !== null);
  return normalizeAdvancedFilterExpression({
    conditions,
    connectors: normalized.connectors,
  });
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

export function migrateSavedView(view: SavedGridView, advancedFieldOrder: readonly string[] = []): SavedGridView {
  const cloned = JSON.parse(JSON.stringify(view)) as SavedGridView;
  if (!cloned.advancedFilterExpression && cloned.advancedFilters && Object.keys(cloned.advancedFilters).length > 0) {
    cloned.advancedFilterExpression = migrateAdvancedFiltersRecord(cloned.advancedFilters, advancedFieldOrder);
  }
  cloned.schemaVersion = SCHEMA_VERSION;
  return cloned;
}

export function sanitizeSavedView(view: SavedGridView, ctx: SavedViewSanitizeContext): SavedGridView {
  const migrated = migrateSavedView(view, ctx.advancedFieldOrder);
  const columnOnlyFilters = sanitizeFilters(
    Object.fromEntries(Object.entries(migrated.filters).filter(([key]) => !ctx.advancedFieldIds.has(key))),
    ctx,
  );
  const advancedFilterExpression = sanitizeAdvancedExpression(migrated.advancedFilterExpression, ctx);

  return normalizeSavedViewForPersistence({
    ...migrated,
    filters: columnOnlyFilters,
    advancedFilterExpression,
    sorts: migrated.sorts.filter((sort) => ctx.knownColumnIds.has(sort.columnId)),
    layout: sanitizeLayout(migrated.layout, ctx.knownColumnIds),
    search: migrated.search?.trim() || undefined,
    schemaVersion: SCHEMA_VERSION,
  });
}

export function prepareSavedViewForPersistence(
  view: Omit<SavedGridView, "schemaVersion">,
  advancedFieldIds: ReadonlySet<string>,
): SavedGridView {
  const { columnFilters } = partitionFilters(view.filters, advancedFieldIds);
  return normalizeSavedViewForPersistence({
    ...view,
    schemaVersion: SCHEMA_VERSION,
    filters: columnFilters,
    advancedFilterExpression: normalizeAdvancedFilterExpression(view.advancedFilterExpression),
  });
}

export function normalizeSavedViewForPersistence(view: SavedGridView): SavedGridView {
  return JSON.parse(JSON.stringify(view)) as SavedGridView;
}

export function advancedFilterFieldOrder(
  advancedFilters: Record<string, GridFilterValue>,
  preferredOrder: readonly string[],
): string[] {
  const keys = Object.keys(advancedFilters).filter((key) => isFilterActive(advancedFilters[key]!));
  return [...preferredOrder.filter((key) => keys.includes(key)), ...keys.filter((key) => !preferredOrder.includes(key))];
}
