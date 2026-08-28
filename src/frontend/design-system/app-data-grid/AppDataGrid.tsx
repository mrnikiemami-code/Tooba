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
import { cn } from "../cn";
import { FilterControl } from "../data-grid/FilterControl";
import { moveColumn } from "../data-grid/serialize";
import type {
  GridBulkAction,
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
  agFilterModelForSavedView,
  buildAgColumnApplyState,
  captureColumnLayoutFromApi,
  normalizeSavedViewForPersistence,
} from "./saved-view-state";
import type { AppGridFilterColumnDef } from "./filter-column-def";
import { toFilterControlColumn } from "./filter-column-def";
import { JalaliDateFilterControl } from "./JalaliDateFilterControl";
import { buildAgGridLocaleText, resolveGridLocale } from "./locale-text";
import { exportRowsToCsv, exportRowsToXlsx } from "./export";

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
  /** برچسب صادقانه: انتخاب فقط صفحهٔ جاری */
  pageSelectionOnly?: boolean;
}

const PAGE_SIZES = [10, 20, 50, 100];

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
  const [activeViewId, setActiveViewId] = useState<string | null>(null);
  const [viewName, setViewName] = useState("");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [columnsOpen, setColumnsOpen] = useState(false);
  const [drawerDragId, setDrawerDragId] = useState<string | null>(null);
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const searchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const queryRef = useRef(query);
  queryRef.current = query;
  const suppressGridEventsRef = useRef(false);

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

  const activeFilterEntries = useMemo(
    () => Object.entries(query.filters).filter(([, value]) => isFilterActive(value)),
    [query.filters],
  );

  const advancedFieldIds = useMemo(
    () => new Set(advancedFilterColumns.map((column) => column.id)),
    [advancedFilterColumns],
  );

  const mergeFilters = useCallback(
    (base: Record<string, GridFilterValue>, agFilters: Record<string, GridFilterValue>) => {
      const preservedAdvanced = Object.fromEntries(
        Object.entries(base).filter(([key]) => advancedFieldIds.has(key)),
      );
      const fromAg = Object.fromEntries(
        Object.entries(agFilters).filter(([key]) => !advancedFieldIds.has(key)),
      );
      return { ...fromAg, ...preservedAdvanced };
    },
    [advancedFieldIds],
  );

  const load = useCallback(
    async (nextQuery: GridServerQuery) => {
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
    void load(DEFAULT_GRID_QUERY);
    return () => abortRef.current?.abort();
  }, [load]);

  useEffect(() => {
    if (!savedViewStore) return;
    void savedViewStore.list().then(setSavedViews).catch(() => setSavedViews([]));
  }, [savedViewStore]);

  const totalPages = Math.max(1, Math.ceil(total / query.pageSize));

  const onGridReady = useCallback((event: GridReadyEvent<T>) => {
    gridApiRef.current = event.api;
  }, []);

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

  const onFilterChanged = useCallback(
    (_event: FilterChangedEvent<T>) => {
      if (suppressGridEventsRef.current) return;
      const api = gridApiRef.current;
      if (!api) return;
      if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
      filterTimerRef.current = setTimeout(() => {
        const agFilters = fromAgFilterModel(api.getFilterModel());
        const filters = mergeFilters(queryRef.current.filters, agFilters);
        setActiveViewId(null);
        void load({ ...queryRef.current, page: 1, filters });
      }, 300);
    },
    [load, mergeFilters],
  );

  function updateAdvancedFilter(columnId: string, value: GridFilterValue) {
    const next = { ...queryRef.current.filters, [columnId]: value };
    if (!isFilterActive(value)) {
      delete next[columnId];
    }
    setActiveViewId(null);
    void load({ ...queryRef.current, page: 1, filters: next });
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

  function clearAllFilters() {
    gridApiRef.current?.setFilterModel(null);
    setSearchInput("");
    setActiveViewId(null);
    void load({ ...queryRef.current, page: 1, filters: {}, search: undefined });
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

  async function saveCurrentView() {
    if (!savedViewStore) return;
    const trimmed = viewName.trim() || messages.defaultViewName;
    const currentQuery = queryRef.current;
    const layout = captureColumnLayoutFromApi(gridApiRef.current, defaultColumnIds);
    const view: SavedGridView = normalizeSavedViewForPersistence({
      id: crypto.randomUUID(),
      name: trimmed,
      filters: currentQuery.filters,
      sorts: currentQuery.sorts,
      layout,
      pageSize: currentQuery.pageSize,
    });
    await savedViewStore.save(view);
    setSavedViews(await savedViewStore.list());
    setActiveViewId(view.id);
    setViewName("");
  }

  async function applyView(view: SavedGridView) {
    const api = gridApiRef.current;
    suppressGridEventsRef.current = true;
    try {
      setActiveViewId(view.id);
      api?.setFilterModel(agFilterModelForSavedView(view.filters, advancedFieldIds));
      api?.applyColumnState({
        state: buildAgColumnApplyState(view),
        applyOrder: true,
      });
      await load({
        page: 1,
        pageSize: view.pageSize,
        sorts: view.sorts,
        filters: view.filters,
        search: queryRef.current.search,
      });
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
    }),
    [],
  );

  return (
    <div dir={direction} className="w-full">
      <div data-app-grid-toolbar className="border-b border-border px-1">
        <input
          type="search"
          value={searchInput}
          onChange={(e) => scheduleSearch(e.target.value)}
          placeholder={messages.search}
          className="min-w-[12rem] flex-1 border border-border bg-surface px-3 text-sm"
          aria-label={messages.search}
        />
        {advancedFilterColumns.length > 0 ? (
          <button
            type="button"
            className="border border-border bg-surface px-3 text-sm"
            onClick={() => setFiltersOpen(true)}
            data-testid="app-grid-advanced-filters"
          >
            {messages.filters}
            {activeFilterEntries.length > 0 ? ` (${activeFilterEntries.length})` : ""}
          </button>
        ) : null}
        <button
          type="button"
          className="border border-border bg-surface px-3 text-sm"
          onClick={() => setColumnsOpen(true)}
          data-testid="app-grid-columns"
        >
          {messages.columns}
        </button>
        {savedViewStore ? (
          <div
            className="flex min-w-[14rem] flex-col gap-2 rounded-ds border border-border bg-secondary/40 p-2"
            data-testid="app-grid-saved-views"
          >
            <div className="flex flex-wrap items-center gap-2">
              <input
                aria-label={messages.saveView}
                value={viewName}
                onChange={(e) => setViewName(e.target.value)}
                placeholder={messages.defaultViewName}
                className="min-w-[10rem] flex-1 border border-border bg-surface px-2 text-sm"
              />
              <button type="button" className="border border-border bg-surface px-3 text-sm" onClick={() => void saveCurrentView()}>
                {messages.saveView}
              </button>
            </div>
            {savedViews.length > 0 ? (
              <ul className="flex flex-wrap gap-2">
                {savedViews.map((view) => {
                  const active = activeViewId === view.id;
                  return (
                    <li key={view.id} className="inline-flex items-center gap-1">
                      <button
                        type="button"
                        className={cn(
                          "inline-flex min-h-9 items-center rounded-full px-3 text-sm font-medium transition-colors",
                          active
                            ? "bg-primary text-primary-foreground shadow-sm"
                            : "border border-border bg-surface hover:bg-secondary",
                        )}
                        aria-pressed={active}
                        onClick={() => void applyView(view)}
                      >
                        {view.name || messages.defaultViewName}
                      </button>
                      <button
                        type="button"
                        className="inline-flex size-8 items-center justify-center rounded-full text-muted hover:bg-danger/10 hover:text-danger"
                        aria-label={`${messages.deleteView}: ${view.name || messages.defaultViewName}`}
                        onClick={() => void deleteView(view.id)}
                      >
                        ×
                      </button>
                    </li>
                  );
                })}
              </ul>
            ) : (
              <p className="px-1 text-xs text-muted">{messages.savedViews}</p>
            )}
          </div>
        ) : null}
        {getExportRow ? (
          <>
            <button type="button" className="border border-border bg-surface px-3 text-sm" onClick={() => void exportCurrent("csv")}>
              {messages.exportCsv}
            </button>
            <button type="button" className="border border-border bg-surface px-3 text-sm" onClick={() => void exportCurrent("xlsx")}>
              {messages.exportExcel}
            </button>
          </>
        ) : null}
        <span className="text-xs text-muted">{messages.exportScopeNote}</span>
        {activeFilterEntries.length > 0 ? (
          <button type="button" className="border border-border bg-surface px-3 text-sm" onClick={clearAllFilters}>
            {messages.clearFilters}
          </button>
        ) : null}
      </div>

      {activeFilterEntries.length > 0 ? (
        <div className="flex flex-wrap items-center gap-2 px-1 py-2" data-testid="app-grid-filter-chips">
          {activeFilterEntries.map(([columnId, value]) => (
            <button
              key={columnId}
              type="button"
              className="inline-flex items-center gap-2 rounded-ds border border-border bg-secondary px-2 py-1 text-xs"
              onClick={() => clearFilter(columnId)}
            >
              <span>{filterChipLabel(columnId, columnLabels[columnId] ?? columnId, value, locale)}</span>
              <span aria-hidden>×</span>
            </button>
          ))}
        </div>
      ) : null}

      <Drawer open={filtersOpen} onClose={() => setFiltersOpen(false)} title={messages.filters}>
        <div className="flex flex-col gap-3">
          {advancedFilterColumns.map((column) => {
            const current = query.filters[column.id];
            if (column.filterKind === "date" && locale === "fa") {
              return (
                <JalaliDateFilterControl
                  key={column.id}
                  header={column.header}
                  locale={locale}
                  value={current}
                  onChange={(value) => updateAdvancedFilter(column.id, value)}
                />
              );
            }

            return (
              <FilterControl
                key={column.id}
                column={toFilterControlColumn(column)}
                value={current}
                onChange={(value) => updateAdvancedFilter(column.id, value)}
              />
            );
          })}
          <div className="flex flex-wrap gap-2 pt-2">
            <Button type="button" tone="secondary" onClick={() => setFiltersOpen(false)}>
              {messages.close}
            </Button>
            {activeFilterEntries.length > 0 ? (
              <Button type="button" tone="ghost" onClick={clearAllFilters}>
                {messages.clearFilters}
              </Button>
            ) : null}
          </div>
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

      {pageSelectionOnly ? (
        <p className="px-1 py-2 text-xs text-muted">{messages.pageSelectionNote}</p>
      ) : null}

      {error ? (
        <div className="rounded-ds border border-danger/30 bg-danger/5 p-4 text-sm">
          <p>{error}</p>
          <button type="button" className="mt-2 underline" onClick={() => void load(query)}>
            {messages.retry}
          </button>
        </div>
      ) : null}

      <div className="ag-theme-quartz ag-theme-tooba w-full" style={{ height: 520 }}>
        <AgGridReact<T>
          rowData={loading ? [] : rows}
          columnDefs={columnDefs}
          defaultColDef={defaultColDef}
          getRowId={(params) => params.data.id}
          localeText={localeText}
          enableRtl={direction === "rtl"}
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

      <div className="mt-3 flex flex-wrap items-center justify-between gap-3 text-sm">
        <div className="flex items-center gap-2">
          <button
            type="button"
            disabled={query.page <= 1 || loading}
            className="rounded-ds border border-border px-3 py-1 disabled:opacity-50"
            onClick={() => void load({ ...query, page: query.page - 1 })}
          >
            {messages.previous}
          </button>
          <span className="tabular-nums">
            {query.page.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")} / {totalPages.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")}
          </span>
          <button
            type="button"
            disabled={query.page >= totalPages || loading}
            className="rounded-ds border border-border px-3 py-1 disabled:opacity-50"
            onClick={() => void load({ ...query, page: query.page + 1 })}
          >
            {messages.next}
          </button>
        </div>
        <label className="flex items-center gap-2">
          {messages.pageSize}
          <select
            value={query.pageSize}
            className="rounded-ds border border-border bg-surface px-2 py-1"
            onChange={(e) => void load({ ...query, page: 1, pageSize: Number(e.target.value) })}
          >
            {PAGE_SIZES.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
        <span className="text-muted tabular-nums">{total.toLocaleString(locale === "fa" ? "fa-IR" : "en-US")} rows</span>
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
