"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import {
  AllCommunityModule,
  ModuleRegistry,
  type ColDef,
  type FilterChangedEvent,
  type GridApi,
  type GridReadyEvent,
  type SortChangedEvent,
} from "ag-grid-community";
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-quartz.css";
import "./theme.css";

import { Drawer } from "../primitives/overlays";
import { Button, Checkbox } from "../primitives/core";
import { moveColumn } from "../data-grid/serialize";
import type {
  AdvancedFilterExpression,
  GridBulkAction,
  GridColumnLayout,
  GridFilterValue,
  GridQueryAdapter,
  GridServerQuery,
  SavedGridView,
  SavedViewStore,
} from "../data-grid/types";
import { isFilterActive } from "../data-grid/serialize";
import { DEFAULT_GRID_QUERY } from "./grid-query-mapper";
import { filterChipLabel, fromAgFilterModel } from "./ag-filter-mapper";
import {
  activeAdvancedConditions,
  createAdvancedCondition,
  normalizeAdvancedFilterExpression,
} from "./advanced-filter-expression";
import { AdvancedFilterBuilder } from "./AdvancedFilterBuilder";
import { SavedViewsToolbar } from "./SavedViewsToolbar";
import {
  agFilterModelForSavedView,
  buildAgColumnApplyState,
  captureColumnLayoutFromApi,
  mergeSavedViewFilters,
  prepareSavedViewForPersistence,
  sanitizeSavedView,
  type SavedViewSanitizeContext,
} from "./saved-view-state";
import type { AppGridFilterColumnDef } from "./filter-column-def";
import { buildAgGridLocaleText, resolveGridLocale } from "./locale-text";
import { exportRowsToCsv, exportRowsToXlsx } from "./export";
import { COLUMN_FILTER_APPLY_PARAMS, filtersEqual, shouldCommitGridQuery } from "./filter-commit";
import { JalaliDateColumnFilter } from "./jalali-date-column-filter";

ModuleRegistry.registerModules([AllCommunityModule]);

export interface AppDataGridProps<T extends { id: string }> {
  columnDefs: ColDef<T>[];
  queryAdapter: GridQueryAdapter<T>;
  locale?: "fa" | "en";
  direction?: "rtl" | "ltr";
  savedViewStore?: SavedViewStore;
  bulkActions?: GridBulkAction<T>[];
  getExportRow?: (row: T) => string[];
  exportHeaders?: string[];
  exportFilenameBase?: string;
  /** فیلتر پیشرفته Community-safe (کشو) — enum/status/date جلالی و غیره */
  advancedFilterColumns?: AppGridFilterColumnDef[];
  /** پرس‌وجوی پیش‌فرض برای restore system default */
  defaultQuery?: GridServerQuery;
  /** برچسب صادقانه: انتخاب فقط صفحهٔ جاری */
  pageSelectionOnly?: boolean;
}

const PAGE_SIZES = [10, 20, 50, 100];
const GRID_ROW_HEIGHT = 56;
const GRID_HEADER_HEIGHT = 48;

/**
 * گرید reusable مبتنی بر AG Grid Community با قرارداد GridServerQuery/project.
 * AG Grid state به queryAdapter نگاشت می‌شود؛ backend مدل خام AG Grid نمی‌بیند.
 */
export function AppDataGrid<T extends { id: string }>({
  columnDefs,
  queryAdapter,
  locale = "fa",
  direction = "rtl",
  savedViewStore,
  bulkActions = [],
  getExportRow,
  exportHeaders = [],
  exportFilenameBase = "export",
  advancedFilterColumns = [],
  pageSelectionOnly = true,
  defaultQuery = DEFAULT_GRID_QUERY,
}: AppDataGridProps<T>) {
  const messages = useMemo(() => resolveGridLocale(locale), [locale]);
  const localeText = useMemo(() => buildAgGridLocaleText(locale), [locale]);
  const [query, setQuery] = useState<GridServerQuery>(DEFAULT_GRID_QUERY);
  const [rows, setRows] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | undefined>();
  const [searchInput, setSearchInput] = useState("");
  const [selected, setSelected] = useState<T[]>([]);
  const [savedViews, setSavedViews] = useState<SavedGridView[]>([]);
  const [defaultViewId, setDefaultViewId] = useState<string | null>(null);
  const [activeViewId, setActiveViewId] = useState<string | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [draftAdvancedFilter, setDraftAdvancedFilter] = useState<AdvancedFilterExpression>(
    DEFAULT_GRID_QUERY.advancedFilter ?? { conditions: [], connectors: [] },
  );
  const [columnsOpen, setColumnsOpen] = useState(false);
  const [drawerDragId, setDrawerDragId] = useState<string | null>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const searchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const queryRef = useRef(query);
  queryRef.current = query;
  const suppressGridEventsRef = useRef(false);
  const defaultLayoutRef = useRef<GridColumnLayout | null>(null);

  const defaultColumnIds = useMemo(
    () => columnDefs.map((col) => col.colId ?? col.field).filter((id): id is string => Boolean(id)),
    [columnDefs],
  );

  const columnLabels = useMemo(() => {
    const labels: Record<string, string> = {};
    for (const col of columnDefs) {
      const id = col.colId ?? col.field;
      if (id) {
        labels[id] = col.headerName ?? id;
      }
    }
    return labels;
  }, [columnDefs]);

  const activeFilterEntries = useMemo(() => {
    const columnEntries = Object.entries(query.filters).filter(([, value]) => isFilterActive(value));
    const advancedEntries = activeAdvancedConditions(query.advancedFilter).map(
      (condition) => [condition.id, condition.value, condition.field] as const,
    );
    return { columnEntries, advancedEntries };
  }, [query.filters, query.advancedFilter]);

  const activeFilterCount =
    activeFilterEntries.columnEntries.length + activeFilterEntries.advancedEntries.length;

  const enumLabels = useMemo(() => {
    const labels: Record<string, string> = {};
    for (const column of advancedFilterColumns) {
      for (const option of column.enumOptions ?? []) {
        labels[option.value] = option.label;
      }
    }
    return labels;
  }, [advancedFilterColumns]);

  const hasActiveFiltering =
    activeFilterCount > 0 || Boolean(searchInput.trim()) || Boolean(query.search?.trim());

  const advancedFieldIds = useMemo(
    () => new Set(advancedFilterColumns.map((column) => column.id)),
    [advancedFilterColumns],
  );

  const sanitizeContext = useMemo((): SavedViewSanitizeContext => {
    const knownColumnIds = new Set(defaultColumnIds);
    const knownFilterFields = new Set([
      ...defaultColumnIds,
      ...advancedFilterColumns.map((column) => column.id),
    ]);
    const enumOptionsByField: Record<string, Set<string>> = {};
    for (const column of advancedFilterColumns) {
      if (column.enumOptions?.length) {
        enumOptionsByField[column.id] = new Set(column.enumOptions.map((option) => option.value));
      }
    }
    return {
      knownColumnIds,
      knownFilterFields,
      advancedFieldIds,
      enumOptionsByField,
      advancedFieldOrder: advancedFilterColumns.map((column) => column.id),
    };
  }, [advancedFieldIds, advancedFilterColumns, defaultColumnIds]);

  useEffect(() => {
    if (!filtersOpen) return;
    const current = normalizeAdvancedFilterExpression(query.advancedFilter);
    if (current.conditions.length === 0 && advancedFilterColumns[0]) {
      setDraftAdvancedFilter({
        conditions: [createAdvancedCondition(advancedFilterColumns[0].id)],
        connectors: [],
      });
      return;
    }
    setDraftAdvancedFilter(current);
  }, [filtersOpen, query.advancedFilter, advancedFilterColumns]);

  const mergeFilters = useCallback(
    (base: Record<string, GridFilterValue>, agFilters: Record<string, GridFilterValue>) => {
      const fromAg = Object.fromEntries(
        Object.entries(agFilters).filter(([key]) => !advancedFieldIds.has(key)),
      );
      const columnBase = Object.fromEntries(
        Object.entries(base).filter(([key]) => !advancedFieldIds.has(key)),
      );
      return { ...columnBase, ...fromAg };
    },
    [advancedFieldIds],
  );

  const load = useCallback(
    async (nextQuery: GridServerQuery, options?: { force?: boolean }) => {
      if (!options?.force && !shouldCommitGridQuery(queryRef.current, nextQuery)) {
        return;
      }
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;
      setLoading(true);
      setError(undefined);
      try {
        const page = await queryAdapter(nextQuery);
        if (controller.signal.aborted) return;
        setRows(page.rows);
        setTotal(page.total);
        setQuery(nextQuery);
      } catch (loadError) {
        if (controller.signal.aborted) return;
        setError(loadError instanceof Error ? loadError.message : messages.error);
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    },
    [messages.error, queryAdapter],
  );

  useEffect(() => {
    void load(defaultQuery, { force: true });
    return () => abortRef.current?.abort();
  }, [load, defaultQuery]);

  useEffect(() => {
    if (!savedViewStore) return;
    void (async () => {
      const views = await savedViewStore.list().catch(() => [] as SavedGridView[]);
      setSavedViews(views);
      const defaultId = savedViewStore.getDefaultViewId
        ? await savedViewStore.getDefaultViewId().catch(() => null)
        : null;
      setDefaultViewId(defaultId);
    })();
  }, [savedViewStore]);

  const totalPages = Math.max(1, Math.ceil(total / query.pageSize));

  const onGridReady = useCallback(
    (event: GridReadyEvent<T>) => {
      gridApiRef.current = event.api;
      if (!defaultLayoutRef.current) {
        defaultLayoutRef.current = captureColumnLayoutFromApi(event.api, defaultColumnIds);
      }
    },
    [defaultColumnIds],
  );

  const onSortChanged = useCallback(
    (event: SortChangedEvent<T>) => {
      if (suppressGridEventsRef.current) return;
      const colState = event.api.getColumnState().find((col) => col.sort);
      const sorts = colState?.colId && colState.sort
        ? [{ columnId: colState.colId, direction: colState.sort as "asc" | "desc" }]
        : DEFAULT_GRID_QUERY.sorts;
      setActiveViewId(null);
      void load({ ...queryRef.current, page: 1, sorts });
    },
    [load],
  );

  const commitColumnFilters = useCallback(() => {
    if (suppressGridEventsRef.current) return;
    const api = gridApiRef.current;
    if (!api) return;
    const agFilters = fromAgFilterModel(api.getFilterModel());
    const filters = mergeFilters(queryRef.current.filters, agFilters);
    if (filtersEqual(filters, queryRef.current.filters)) return;
    setActiveViewId(null);
    void load({ ...queryRef.current, page: 1, filters });
  }, [load, mergeFilters]);

  const onFilterChanged = useCallback(
    (_event: FilterChangedEvent<T>) => {
      commitColumnFilters();
    },
    [commitColumnFilters],
  );

  function applyDraftAdvancedFilter() {
    const normalized = normalizeAdvancedFilterExpression(draftAdvancedFilter);
    setActiveViewId(null);
    void load({ ...queryRef.current, page: 1, advancedFilter: normalized });
    setFiltersOpen(false);
  }

  function clearAdvancedCondition(conditionId: string) {
    const normalized = normalizeAdvancedFilterExpression(query.advancedFilter);
    const conditions = normalized.conditions.filter((condition) => condition.id !== conditionId);
    setActiveViewId(null);
    void load({
      ...queryRef.current,
      page: 1,
      advancedFilter: normalizeAdvancedFilterExpression({ conditions, connectors: normalized.connectors }),
    });
  }

  function clearAllFilters() {
    gridApiRef.current?.setFilterModel(null);
    setSearchInput("");
    setActiveViewId(null);
    void load({
      ...queryRef.current,
      page: 1,
      filters: {},
      search: undefined,
      advancedFilter: { conditions: [], connectors: [] },
    });
  }

  function columnStateForDrawer() {
    const api = gridApiRef.current;
    if (!api) return [];
    return api.getColumnState().filter((col) => col.colId && col.colId !== "actions");
  }

  function clearFilter(columnId: string) {
    const api = gridApiRef.current;
    if (api) {
      const model = { ...api.getFilterModel() };
      delete model[columnId];
      api.setFilterModel(Object.keys(model).length > 0 ? model : null);
    }
    const next = { ...queryRef.current.filters };
    delete next[columnId];
    setActiveViewId(null);
    void load({ ...queryRef.current, page: 1, filters: next });
  }

  function restoreColumns() {
    gridApiRef.current?.resetColumnState();
    setColumnsOpen(false);
  }

  function scheduleSearch(value: string) {
    setSearchInput(value);
    if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      setActiveViewId(null);
      void load({ ...queryRef.current, page: 1, search: value.trim() || undefined });
    }, 350);
  }

  async function persistView(view: SavedGridView) {
    if (!savedViewStore) return;
    await savedViewStore.save(view);
    setSavedViews(await savedViewStore.list());
  }

  async function createSavedView(name: string, setAsDefault: boolean) {
    if (!savedViewStore) return;
    const currentQuery = queryRef.current;
    const layout = captureColumnLayoutFromApi(gridApiRef.current, defaultColumnIds);
    const view = prepareSavedViewForPersistence(
      {
        id: crypto.randomUUID(),
        name,
        filters: currentQuery.filters,
        advancedFilterExpression: normalizeAdvancedFilterExpression(currentQuery.advancedFilter),
        sorts: currentQuery.sorts,
        layout,
        pageSize: currentQuery.pageSize,
        search: currentQuery.search,
      },
      advancedFieldIds,
    );
    await persistView(view);
    if (setAsDefault && savedViewStore.setDefaultViewId) {
      await savedViewStore.setDefaultViewId(view.id);
      setDefaultViewId(view.id);
    }
    setActiveViewId(view.id);
  }

  async function updateSavedView(viewId: string) {
    if (!savedViewStore) return;
    const existing = savedViews.find((item) => item.id === viewId);
    if (!existing) return;
    const currentQuery = queryRef.current;
    const layout = captureColumnLayoutFromApi(gridApiRef.current, defaultColumnIds);
    const view = prepareSavedViewForPersistence(
      {
        ...existing,
        filters: currentQuery.filters,
        advancedFilterExpression: normalizeAdvancedFilterExpression(currentQuery.advancedFilter),
        sorts: currentQuery.sorts,
        layout,
        pageSize: currentQuery.pageSize,
        search: currentQuery.search,
      },
      advancedFieldIds,
    );
    await persistView(view);
    setActiveViewId(view.id);
  }

  async function applyView(rawView: SavedGridView) {
    const view = sanitizeSavedView(rawView, sanitizeContext);
    const api = gridApiRef.current;
    suppressGridEventsRef.current = true;
    try {
      setActiveViewId(view.id);
      setSearchInput(view.search ?? "");
      api?.setFilterModel(agFilterModelForSavedView(view.filters, advancedFieldIds));
      api?.applyColumnState({
        state: buildAgColumnApplyState(view, sanitizeContext.knownColumnIds),
        applyOrder: true,
      });
      await load({
        page: 1,
        pageSize: view.pageSize,
        sorts: view.sorts.length > 0 ? view.sorts : defaultQuery.sorts,
        filters: view.filters,
        advancedFilter: view.advancedFilterExpression ?? { conditions: [], connectors: [] },
        search: view.search,
      });
    } finally {
      suppressGridEventsRef.current = false;
    }
  }

  async function renameView(viewId: string, name: string) {
    if (!savedViewStore) return;
    const view = savedViews.find((item) => item.id === viewId);
    if (!view) return;
    const trimmed = name.trim() || messages.defaultViewName;
    await persistView({ ...view, name: trimmed });
  }

  async function setDefaultView(viewId: string) {
    if (!savedViewStore?.setDefaultViewId) return;
    await savedViewStore.setDefaultViewId(viewId);
    setDefaultViewId(viewId);
  }

  async function restoreSystemDefault() {
    const api = gridApiRef.current;
    suppressGridEventsRef.current = true;
    try {
      setActiveViewId(null);
      setSearchInput("");
      api?.setFilterModel(null);
      if (defaultLayoutRef.current) {
        api?.applyColumnState({
          state: buildAgColumnApplyState(
            {
              id: "default",
              name: "default",
              filters: {},
              sorts: defaultQuery.sorts,
              layout: defaultLayoutRef.current,
              pageSize: defaultQuery.pageSize,
            },
            sanitizeContext.knownColumnIds,
          ),
          applyOrder: true,
        });
      } else {
        api?.resetColumnState();
      }
      await load({ ...defaultQuery, advancedFilter: { conditions: [], connectors: [] } });
    } finally {
      suppressGridEventsRef.current = false;
    }
  }

  async function deleteView(viewId: string) {
    if (!savedViewStore) return;
    await savedViewStore.remove(viewId);
    setSavedViews(await savedViewStore.list());
    if (activeViewId === viewId) {
      setActiveViewId(null);
    }
  }

  async function exportCurrent(format: "csv" | "xlsx") {
    if (!getExportRow || exportHeaders.length === 0) return;
    const data = (selected.length > 0 ? selected : rows).map(getExportRow);
    const stamp = new Date().toISOString().slice(0, 10);
    if (format === "csv") {
      await exportRowsToCsv(`${exportFilenameBase}-${stamp}.csv`, exportHeaders, data);
    } else {
      await exportRowsToXlsx(`${exportFilenameBase}-${stamp}.xlsx`, "Sheet1", exportHeaders, data);
    }
  }

  const defaultColDef = useMemo<ColDef<T>>(
    () => ({
      sortable: true,
      filter: true,
      resizable: true,
      minWidth: 72,
      flex: 1,
      filterParams: COLUMN_FILTER_APPLY_PARAMS,
    }),
    [],
  );

  const gridComponents = useMemo(
    () => ({
      jalaliDateColumnFilter: JalaliDateColumnFilter,
    }),
    [],
  );

  const pageNumbers = useMemo(() => {
    const windowSize = 5;
    let start = Math.max(1, query.page - Math.floor(windowSize / 2));
    const end = Math.min(totalPages, start + windowSize - 1);
    start = Math.max(1, end - windowSize + 1);
    const pages: number[] = [];
    for (let page = start; page <= end; page += 1) pages.push(page);
    return pages;
  }, [query.page, totalPages]);

  const rowFrom = total === 0 ? 0 : (query.page - 1) * query.pageSize + 1;
  const rowTo = Math.min(query.page * query.pageSize, total);

  return (
    <div dir={direction} className="w-full" data-app-grid-shell>
      <div data-app-grid-toolbar>
        <div data-app-grid-toolbar-row>
          <div data-app-grid-search-wrap>
            <input
              type="search"
              data-app-grid-search
              value={searchInput}
              onChange={(e) => scheduleSearch(e.target.value)}
              placeholder={messages.search}
              aria-label={messages.search}
            />
          </div>
          {hasActiveFiltering ? (
            <button
              type="button"
              data-app-grid-clear-filters
              className="inline-flex min-h-9 items-center rounded-full border px-3 text-sm font-medium"
              onClick={clearAllFilters}
              data-testid="app-grid-clear-all-filters"
            >
              {messages.clearAllFilters}
            </button>
          ) : null}
          {advancedFilterColumns.length > 0 ? (
            <button
              type="button"
              className="inline-flex min-h-9 items-center gap-1 rounded-full border border-border bg-surface px-3 text-sm hover:bg-secondary"
              onClick={() => setFiltersOpen(true)}
              data-testid="app-grid-advanced-filters"
              aria-label={messages.advancedFilterEntry}
            >
              <span aria-hidden>⚲</span>
              {messages.advancedFilterEntry}
              {activeFilterCount > 0 ? (
                <span className="rounded-full bg-primary px-1.5 py-0.5 text-xs text-primary-foreground">{activeFilterCount}</span>
              ) : null}
            </button>
          ) : null}
          <button
            type="button"
            className="inline-flex min-h-9 items-center rounded-full border border-border bg-surface px-3 text-sm hover:bg-secondary"
            onClick={() => setColumnsOpen(true)}
            data-testid="app-grid-columns"
          >
            {messages.columns}
          </button>
          {getExportRow ? (
            <>
              <button
                type="button"
                className="inline-flex min-h-9 items-center rounded-full border border-border bg-surface px-3 text-sm hover:bg-secondary"
                onClick={() => void exportCurrent("csv")}
              >
                {messages.exportCsv}
              </button>
              <button
                type="button"
                className="inline-flex min-h-9 items-center rounded-full border border-border bg-surface px-3 text-sm hover:bg-secondary"
                onClick={() => void exportCurrent("xlsx")}
              >
                {messages.exportExcel}
              </button>
            </>
          ) : null}
          {selected.length > 0 ? (
            <span className="text-sm text-muted tabular-nums" data-testid="app-grid-selected-count">
              {messages.selectedCount}: {selected.length.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")}
            </span>
          ) : null}
        </div>
        {savedViewStore ? (
          <SavedViewsToolbar
            locale={locale}
            messages={{
              savedViews: messages.savedViews,
              saveView: messages.saveView,
              deleteView: messages.deleteView,
              renameView: messages.renameView,
              restoreDefault: messages.restoreDefault,
              defaultViewName: messages.defaultViewName,
              apply: messages.apply,
              cancel: messages.cancel,
              setDefault: messages.setDefault,
              updateView: messages.updateView,
              systemDefault: messages.systemDefault,
            }}
            savedViews={savedViews}
            activeViewId={activeViewId}
            defaultViewId={defaultViewId}
            onApply={(view) => void applyView(view)}
            onCreate={(name, setAsDefault) => void createSavedView(name, setAsDefault)}
            onUpdate={(viewId) => void updateSavedView(viewId)}
            onRename={(viewId, name) => void renameView(viewId, name)}
            onDelete={(viewId) => void deleteView(viewId)}
            onSetDefault={(viewId) => void setDefaultView(viewId)}
            onRestoreSystemDefault={() => void restoreSystemDefault()}
          />
        ) : null}
        {hasActiveFiltering ? (
          <div className="flex flex-wrap items-center gap-2" data-testid="app-grid-filter-chips">
            {searchInput.trim() ? (
              <button type="button" data-app-grid-chip onClick={() => scheduleSearch("")}>
                <span>{locale === "fa" ? "جستجو" : "Search"}: {searchInput.trim()}</span>
                <span aria-hidden>×</span>
              </button>
            ) : null}
            {activeFilterEntries.columnEntries.map(([columnId, value]) => (
              <button
                key={columnId}
                type="button"
                data-app-grid-chip
                onClick={() => clearFilter(columnId)}
              >
                <span>{filterChipLabel(columnId, columnLabels[columnId] ?? columnId, value, locale, { enumLabels })}</span>
                <span aria-hidden>×</span>
              </button>
            ))}
            {activeFilterEntries.advancedEntries.map(([conditionId, value, fieldId]) => (
              <button
                key={conditionId}
                type="button"
                data-app-grid-chip
                onClick={() => clearAdvancedCondition(conditionId)}
              >
                <span>{filterChipLabel(fieldId, columnLabels[fieldId] ?? fieldId, value, locale, { enumLabels })}</span>
                <span aria-hidden>×</span>
              </button>
            ))}
          </div>
        ) : null}
      </div>

      <Drawer open={filtersOpen} onClose={() => setFiltersOpen(false)} title={messages.advancedFilterEntry}>
        <AdvancedFilterBuilder
          columns={advancedFilterColumns}
          expression={draftAdvancedFilter}
          onChange={setDraftAdvancedFilter}
          locale={locale}
          andLabel={messages.andConnector}
          orLabel={messages.orConnector}
          addLabel={messages.addCondition}
          removeLabel={messages.removeCondition}
          fieldLabel={messages.filters}
        />
        <div className="flex flex-wrap gap-2 pt-3">
          <Button type="button" tone="primary" onClick={applyDraftAdvancedFilter}>
            {messages.apply}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setFiltersOpen(false)}>
            {messages.close}
          </Button>
          {hasActiveFiltering ? (
            <Button type="button" tone="ghost" onClick={clearAllFilters}>
              {messages.clearAllFilters}
            </Button>
          ) : null}
        </div>
      </Drawer>

      <Drawer open={columnsOpen} onClose={() => setColumnsOpen(false)} title={messages.columns}>
        <div className="flex flex-col gap-1.5">
          {columnStateForDrawer().map((col, index, cols) => (
            <div
              key={col.colId}
              draggable
              onDragStart={() => setDrawerDragId(col.colId!)}
              onDragOver={(event) => event.preventDefault()}
              onDrop={() => {
                if (!drawerDragId || !gridApiRef.current) return;
                const order = cols.map((item) => item.colId!);
                const nextOrder = moveColumn(order, drawerDragId, col.colId!);
                gridApiRef.current.applyColumnState({ state: nextOrder.map((colId) => ({ colId })), applyOrder: true });
                setDrawerDragId(null);
              }}
              className="flex items-center gap-2 rounded-ds border border-border bg-surface px-2 py-1.5"
            >
              <span className="cursor-grab text-muted" aria-label={messages.dragColumn}>
                ⋮⋮
              </span>
              <Checkbox
                label={columnLabels[col.colId!] ?? col.colId!}
                checked={!col.hide}
                onChange={() => {
                  gridApiRef.current?.applyColumnState({
                    state: [{ colId: col.colId!, hide: !col.hide }],
                  });
                }}
              />
              <button
                type="button"
                className="text-xs text-muted"
                aria-label={messages.moveColumnUp}
                disabled={index === 0}
                onClick={() => {
                  if (index === 0 || !gridApiRef.current) return;
                  const order = cols.map((item) => item.colId!);
                  gridApiRef.current.applyColumnState({
                    state: moveColumn(order, col.colId!, order[index - 1]!).map((colId) => ({ colId })),
                    applyOrder: true,
                  });
                }}
              >
                ↑
              </button>
              <button
                type="button"
                className="text-xs text-muted"
                aria-label={messages.moveColumnDown}
                disabled={index === cols.length - 1}
                onClick={() => {
                  if (index >= cols.length - 1 || !gridApiRef.current) return;
                  const order = cols.map((item) => item.colId!);
                  gridApiRef.current.applyColumnState({
                    state: moveColumn(order, col.colId!, order[index + 1]!).map((colId) => ({ colId })),
                    applyOrder: true,
                  });
                }}
              >
                ↓
              </button>
            </div>
          ))}
          <div className="flex flex-wrap gap-2 pt-2">
            <Button type="button" tone="secondary" onClick={() => setColumnsOpen(false)}>
              {messages.close}
            </Button>
            <Button type="button" tone="ghost" onClick={restoreColumns}>
              {messages.restoreColumns}
            </Button>
          </div>
        </div>
      </Drawer>

      {error ? (
        <div className="rounded-ds border border-danger/30 bg-danger/5 p-4 text-sm">
          <p>{error}</p>
          <button type="button" className="mt-2 underline" onClick={() => void load(query)}>
            {messages.retry}
          </button>
        </div>
      ) : null}

      <div data-app-grid-viewport className="ag-theme-quartz ag-theme-tooba w-full" style={{ height: 560 }}>
        <AgGridReact<T>
          theme="legacy"
          rowHeight={GRID_ROW_HEIGHT}
          headerHeight={GRID_HEADER_HEIGHT}
          rowData={loading ? [] : rows}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          components={gridComponents}
          context={{ locale }}
          getRowId={(params) => params.data.id}
          localeText={localeText}
          enableRtl={direction === "rtl"}
          ensureDomOrder
          animateRows
          suppressDragLeaveHidesColumns
          rowSelection={{ mode: "multiRow", checkboxes: true, headerCheckbox: pageSelectionOnly }}
          onGridReady={onGridReady}
          onSortChanged={onSortChanged}
          onFilterChanged={onFilterChanged}
          onSelectionChanged={(event) => {
            setSelected(event.api.getSelectedRows());
          }}
          loading={loading}
          overlayLoadingTemplate={`<span class="ag-overlay-loading-center">${messages.loading}</span>`}
          overlayNoRowsTemplate={`<span class="ag-overlay-no-rows-center">${total === 0 && !loading ? messages.empty : messages.emptyFiltered}</span>`}
        />
      </div>

      <div data-app-grid-pagination>
        <span className="text-muted tabular-nums">
          {messages.showingRows}{" "}
          {rowFrom.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")} {locale === "fa" ? "تا" : "to"}{" "}
          {rowTo.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")} {locale === "fa" ? "از" : "of"}{" "}
          {total.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")} {locale === "fa" ? "محصول" : "rows"}
        </span>
        <div className="flex flex-wrap items-center gap-1">
          <button
            type="button"
            data-app-grid-page-btn
            disabled={query.page <= 1 || loading}
            onClick={() => void load({ ...query, page: query.page - 1 })}
          >
            {messages.previous}
          </button>
          {pageNumbers.map((page) => (
            <button
              key={page}
              type="button"
              data-app-grid-page-btn
              data-active={page === query.page ? "true" : undefined}
              disabled={loading}
              onClick={() => void load({ ...query, page })}
            >
              {page.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")}
            </button>
          ))}
          <button
            type="button"
            data-app-grid-page-btn
            disabled={query.page >= totalPages || loading}
            onClick={() => void load({ ...query, page: query.page + 1 })}
          >
            {messages.next}
          </button>
        </div>
        <label className="flex items-center gap-2">
          {messages.pageSize}
          <select
            value={query.pageSize}
            className="min-h-9 rounded-ds border border-border bg-surface px-2"
            onChange={(e) => void load({ ...query, page: 1, pageSize: Number(e.target.value) })}
          >
            {PAGE_SIZES.map((size) => (
              <option key={size} value={size}>
                {size.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")}
              </option>
            ))}
          </select>
        </label>
      </div>

      {bulkActions.length > 0 && selected.length > 0 ? (
        <div className="mt-3 flex flex-wrap gap-2">
          {bulkActions
            .filter((action) => action.isAvailable(selected))
            .map((action) => (
              <button
                key={action.id}
                type="button"
                className="rounded-ds border border-border bg-surface px-3 py-2 text-sm"
                onClick={() => void action.execute(selected)}
              >
                {action.label}
              </button>
            ))}
        </div>
      ) : null}
    </div>
  );
}
