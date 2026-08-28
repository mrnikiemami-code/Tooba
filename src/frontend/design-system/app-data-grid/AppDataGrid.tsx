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

import type {
  GridBulkAction,
  GridQueryAdapter,
  GridServerQuery,
  SavedGridView,
  SavedViewStore,
} from "../data-grid/types";
import { isFilterActive } from "../data-grid/serialize";
import { DEFAULT_GRID_QUERY } from "./grid-query-mapper";
import { filterChipLabel, fromAgFilterModel } from "./ag-filter-mapper";
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
  const [viewName, setViewName] = useState("");
  const gridApiRef = useRef<GridApi<T> | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const searchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const filterTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const queryRef = useRef(query);
  queryRef.current = query;

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
      const colState = event.api.getColumnState().find((col) => col.sort);
      const sorts = colState?.colId && colState.sort
        ? [{ columnId: colState.colId, direction: colState.sort as "asc" | "desc" }]
        : DEFAULT_GRID_QUERY.sorts;
      void load({ ...queryRef.current, page: 1, sorts });
    },
    [load],
  );

  const onFilterChanged = useCallback(
    (_event: FilterChangedEvent<T>) => {
      const api = gridApiRef.current;
      if (!api) return;
      if (filterTimerRef.current) clearTimeout(filterTimerRef.current);
      filterTimerRef.current = setTimeout(() => {
        const filters = fromAgFilterModel(api.getFilterModel());
        void load({ ...queryRef.current, page: 1, filters });
      }, 300);
    },
    [load],
  );

  function clearFilter(columnId: string) {
    const api = gridApiRef.current;
    if (api) {
      const model = { ...api.getFilterModel() };
      delete model[columnId];
      api.setFilterModel(Object.keys(model).length > 0 ? model : null);
    }
    const next = { ...queryRef.current.filters };
    delete next[columnId];
    void load({ ...queryRef.current, page: 1, filters: next });
  }

  function clearAllFilters() {
    gridApiRef.current?.setFilterModel(null);
    setSearchInput("");
    void load({ ...queryRef.current, page: 1, filters: {}, search: undefined });
  }

  function scheduleSearch(value: string) {
    setSearchInput(value);
    if (searchTimerRef.current) clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(() => {
      void load({ ...queryRef.current, page: 1, search: value.trim() || undefined });
    }, 350);
  }

  async function saveCurrentView() {
    if (!savedViewStore || !viewName.trim()) return;
    const api = gridApiRef.current;
    const order = api?.getColumnState().map((c) => c.colId!).filter(Boolean) ?? columnDefs.map((c) => c.field!).filter(Boolean);
    const visibility = Object.fromEntries(
      (api?.getColumnState() ?? []).map((c) => [c.colId!, !c.hide]),
    );
    const view: SavedGridView = {
      id: `view-${Date.now()}`,
      name: viewName.trim(),
      filters: query.filters,
      sorts: query.sorts,
      layout: { order, visibility, widths: {} },
      pageSize: query.pageSize,
    };
    await savedViewStore.save(view);
    setSavedViews(await savedViewStore.list());
    setViewName("");
  }

  async function applyView(view: SavedGridView) {
    void load({
      page: 1,
      pageSize: view.pageSize,
      sorts: view.sorts,
      filters: view.filters,
      search: query.search,
    });
    gridApiRef.current?.applyColumnState({
      state: view.layout.order.map((colId) => ({
        colId,
        hide: view.layout.visibility[colId] === false,
      })),
      applyOrder: true,
    });
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
        {savedViewStore ? (
          <>
            <select
              className="border border-border bg-surface px-2 text-sm"
              defaultValue=""
              onChange={(e) => {
                const view = savedViews.find((v) => v.id === e.target.value);
                if (view) void applyView(view);
                e.currentTarget.value = "";
              }}
            >
              <option value="">{messages.savedViews}</option>
              {savedViews.map((view) => (
                <option key={view.id} value={view.id}>
                  {view.name}
                </option>
              ))}
            </select>
            <input
              value={viewName}
              onChange={(e) => setViewName(e.target.value)}
              placeholder={messages.saveView}
              className="border border-border bg-surface px-2 text-sm"
            />
            <button type="button" className="border border-border bg-surface px-3 text-sm" onClick={() => void saveCurrentView()}>
              {messages.saveView}
            </button>
          </>
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
