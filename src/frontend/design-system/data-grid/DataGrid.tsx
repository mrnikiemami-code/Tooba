"use client";

import { useCallback, useEffect, useMemo, useState, type KeyboardEvent } from "react";
import { cn } from "../cn";
import { Button, Checkbox, EmptyState, ErrorState, Input, Select, Skeleton, Spinner } from "../primitives/core";
import { Drawer } from "../primitives/overlays";
import { FilterControl } from "./FilterControl";
import { faGridMessages } from "./messages";
import { rowsToCsv } from "./query-engine";
import {
  clampWidth,
  cycleSort,
  defaultLayout,
  isFilterActive,
  moveColumn,
  selectPage,
  serializeGridQuery,
  stickyLogicalSide,
  toggleSelection,
  visibleExportColumns,
} from "./serialize";
import type {
  EntityFilterAdapter,
  GridBulkAction,
  GridColumnDef,
  GridColumnLayout,
  GridDensity,
  GridMessages,
  GridQueryAdapter,
  GridServerQuery,
  SavedGridView,
  SavedViewStore,
} from "./types";

export interface DataGridProps<T extends { id: string }> {
  columns: GridColumnDef<T>[];
  queryAdapter: GridQueryAdapter<T>;
  messages?: GridMessages;
  bulkActions?: GridBulkAction<T>[];
  savedViewStore?: SavedViewStore;
  entityLookup?: EntityFilterAdapter;
  onServerExport?: (query: GridServerQuery) => Promise<void>;
}

/**
 * گرید عملیاتی قابل‌استفاده مجدد. صفحه را از آداپتر می‌گیرد تا مجموعهٔ بزرگ روی سرور بماند.
 */
export function DataGrid<T extends { id: string }>({
  columns,
  queryAdapter,
  messages = faGridMessages,
  bulkActions = [],
  savedViewStore,
  entityLookup,
  onServerExport,
}: DataGridProps<T>) {
  const [layout, setLayout] = useState<GridColumnLayout>(() =>
    defaultLayout(
      columns.map((column) => column.id),
      Object.fromEntries(columns.map((column) => [column.id, column.width])),
    ),
  );
  const [query, setQuery] = useState<GridServerQuery>({ page: 1, pageSize: 10, sorts: [], filters: {} });
  const [rows, setRows] = useState<T[]>([]);
  const [total, setTotal] = useState(0);
  const [status, setStatus] = useState<"loading" | "ready" | "error">("loading");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [density, setDensity] = useState<GridDensity>("comfortable");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [columnsOpen, setColumnsOpen] = useState(false);
  const [views, setViews] = useState<SavedGridView[]>([]);
  const [viewName, setViewName] = useState("");
  const [notice, setNotice] = useState<string | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [narrow, setNarrow] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const visibleColumns = useMemo(
    () =>
      layout.order
        .map((id) => columns.find((column) => column.id === id))
        .filter((column): column is GridColumnDef<T> => Boolean(column && layout.visibility[column.id] !== false)),
    [columns, layout],
  );

  const reload = useCallback(async () => {
    setStatus((current) => (current === "ready" ? current : "loading"));
    setRefreshing(true);
    try {
      const page = await queryAdapter(query);
      setRows(page.rows);
      setTotal(page.total);
      setStatus("ready");
    } catch {
      setStatus("error");
    } finally {
      setRefreshing(false);
    }
  }, [query, queryAdapter]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    const media = window.matchMedia("(max-width: 767px)");
    const sync = () => setNarrow(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, []);

  useEffect(() => {
    void savedViewStore?.list().then(setViews);
  }, [savedViewStore]);

  const pageCount = Math.max(1, Math.ceil(total / query.pageSize));
  const activeFilters = Object.values(query.filters).filter(isFilterActive).length;
  const rowClass = density === "compact" ? "h-11" : "h-14";
  const exportIds = columns.filter((column) => column.exportable !== false).map((column) => column.id);

  function download(content: string, name: string) {
    const blob = new Blob([content], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
  }

  function onSortKey(column: GridColumnDef<T>, event: KeyboardEvent<HTMLButtonElement>) {
    if ((event.key === "Enter" || event.key === " ") && column.sortable !== false) {
      event.preventDefault();
      setQuery((current) => ({ ...current, page: 1, sorts: cycleSort(current.sorts, column.id) }));
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <Input
          aria-label={messages.search}
          placeholder={messages.search}
          value={query.search ?? ""}
          onChange={(event) => setQuery((current) => ({ ...current, page: 1, search: event.target.value }))}
        />
        <Button type="button" tone="secondary" onClick={() => setFiltersOpen(true)}>
          {messages.filters}
          {activeFilters ? ` (${activeFilters})` : ""}
        </Button>
        <Button type="button" tone="secondary" onClick={() => setColumnsOpen(true)}>
          {messages.columns}
        </Button>
        <Button
          type="button"
          tone="secondary"
          onClick={() => download(rowsToCsv(rows, columns, visibleExportColumns(layout, exportIds)), "grid-visible.csv")}
        >
          {messages.exportVisible}
        </Button>
        <Button
          type="button"
          tone="secondary"
          disabled={selected.size === 0}
          onClick={() =>
            download(
              rowsToCsv(
                rows.filter((row) => selected.has(row.id)),
                columns,
                visibleExportColumns(layout, exportIds),
              ),
              "grid-selected.csv",
            )
          }
        >
          {messages.exportSelected}
        </Button>
        <Button
          type="button"
          tone="secondary"
          onClick={() => {
            void onServerExport?.(query);
            setNotice(messages.exportServer);
          }}
        >
          {messages.exportServer}
        </Button>
        <Select aria-label={messages.densityComfortable} value={density} onChange={(event) => setDensity(event.target.value as GridDensity)}>
          <option value="comfortable">{messages.densityComfortable}</option>
          <option value="compact">{messages.densityCompact}</option>
        </Select>
        <Button type="button" tone="ghost" onClick={() => void reload()}>
          {messages.reload}
        </Button>
        <span className="text-sm text-muted">
          {messages.selected}: {selected.size}
        </span>
      </div>
      {savedViewStore ? (
        <div className="flex flex-wrap items-center gap-2">
          <Input aria-label={messages.saveView} value={viewName} onChange={(event) => setViewName(event.target.value)} />
          <Button
            type="button"
            tone="secondary"
            onClick={() => {
              const view: SavedGridView = {
                id: crypto.randomUUID(),
                name: viewName || "view",
                filters: query.filters,
                sorts: query.sorts,
                layout,
                pageSize: query.pageSize,
                density,
              };
              void savedViewStore.save(view).then(() => savedViewStore.list().then(setViews));
            }}
          >
            {messages.saveView}
          </Button>
          <Select
            aria-label={messages.savedViews}
            defaultValue=""
            onChange={(event) => {
              const view = views.find((item) => item.id === event.target.value);
              if (!view) {
                return;
              }
              setQuery((current) => ({
                ...current,
                page: 1,
                pageSize: view.pageSize,
                filters: view.filters,
                sorts: view.sorts,
              }));
              setLayout(view.layout);
              if (view.density) {
                setDensity(view.density);
              }
            }}
          >
            <option value="">{messages.savedViews}</option>
            {views.map((view) => (
              <option key={view.id} value={view.id}>
                {view.name}
              </option>
            ))}
          </Select>
        </div>
      ) : null}
      {bulkActions.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {bulkActions.map((action) => {
            const chosen = rows.filter((row) => selected.has(row.id));
            return (
              <Button
                key={action.id}
                type="button"
                tone="secondary"
                disabled={!action.isAvailable(chosen)}
                onClick={() => {
                  if (action.requiresConfirmation && !window.confirm(messages.bulkConfirm)) {
                    return;
                  }
                  void action.execute(chosen).then((result) => setNotice(result.message));
                }}
              >
                {action.label}
              </Button>
            );
          })}
        </div>
      ) : null}
      {notice ? <p className="text-sm text-muted">{notice}</p> : null}
      {refreshing && status === "ready" ? <Spinner /> : null}
      {status === "loading" ? (
        <div className="space-y-2" aria-busy="true" aria-label={messages.loading}>
          <Spinner />
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-12 w-full" />
        </div>
      ) : null}
      {status === "error" ? <ErrorState title={messages.error} retryLabel={messages.retry} onRetry={() => void reload()} /> : null}
      {status === "ready" && rows.length === 0 ? (
        <EmptyState title={activeFilters || query.search ? messages.emptyFiltered : messages.empty} />
      ) : null}
      {status === "ready" && rows.length > 0 && narrow ? (
        <ul className="flex flex-col gap-2">
          {rows.map((row) => (
            <li key={row.id} className="rounded-ds border border-border bg-surface p-3">
              <Checkbox label={messages.selectPage} checked={selected.has(row.id)} onChange={() => setSelected((current) => toggleSelection(current, row.id))} />
              {visibleColumns.map((column) => (
                <p key={column.id} className="text-sm">
                  <span className="text-muted">{column.header}: </span>
                  {(column.cell ?? ((item: T) => String(column.accessor(item))))(row)}
                </p>
              ))}
            </li>
          ))}
        </ul>
      ) : null}
      {status === "ready" && rows.length > 0 && !narrow ? (
        <div className="overflow-x-auto rounded-ds border border-border">
          <table className="min-w-full border-separate border-spacing-0 text-sm">
            <thead className="sticky top-0 z-10 bg-surface">
              <tr>
                <th className="sticky start-0 z-20 bg-surface p-2">
                  <Checkbox
                    label={messages.selectPage}
                    checked={rows.length > 0 && rows.every((row) => selected.has(row.id))}
                    onChange={() =>
                      setSelected((current) => (rows.every((row) => current.has(row.id)) ? new Set() : selectPage(rows.map((row) => row.id))))
                    }
                  />
                </th>
                {visibleColumns.map((column) => {
                  const side = column.sticky ? stickyLogicalSide(column.sticky) : undefined;
                  return (
                    <th
                      key={column.id}
                      draggable={column.reorderable !== false}
                      onDragStart={() => setDragId(column.id)}
                      onDragOver={(event) => event.preventDefault()}
                      onDrop={() => {
                        if (dragId) {
                          setLayout((current) => ({ ...current, order: moveColumn(current.order, dragId, column.id) }));
                          setDragId(null);
                        }
                      }}
                      tabIndex={0}
                      onKeyDown={(event) => {
                        if (!event.altKey || (event.key !== "ArrowLeft" && event.key !== "ArrowRight")) {
                          return;
                        }
                        event.preventDefault();
                        const visible = layout.order.filter((id) => layout.visibility[id] !== false);
                        const index = visible.indexOf(column.id);
                        const target = visible[index + (event.key === "ArrowRight" ? 1 : -1)];
                        if (target) {
                          setLayout((current) => ({ ...current, order: moveColumn(current.order, column.id, target) }));
                        }
                      }}
                      style={{
                        width: layout.widths[column.id] ?? column.width,
                        minWidth: column.minWidth,
                        maxWidth: column.maxWidth,
                        position: column.sticky ? "sticky" : undefined,
                        insetInlineStart: side === "inline-start" ? 48 : undefined,
                        insetInlineEnd: side === "inline-end" ? 0 : undefined,
                      }}
                      className="border-b border-border bg-surface p-2 text-start"
                    >
                      <button
                        type="button"
                        className="font-medium"
                        onClick={() =>
                          column.sortable !== false &&
                          setQuery((current) => ({ ...current, page: 1, sorts: cycleSort(current.sorts, column.id) }))
                        }
                        onKeyDown={(event) => onSortKey(column, event)}
                      >
                        {column.header}
                        {query.sorts[0]?.columnId === column.id ? (query.sorts[0].direction === "asc" ? " ↑" : " ↓") : ""}
                      </button>
                      {column.resizable !== false ? (
                        <input
                          aria-label={`${column.header} width`}
                          type="range"
                          min={column.minWidth}
                          max={column.maxWidth}
                          value={layout.widths[column.id] ?? column.width}
                          onChange={(event) =>
                            setLayout((current) => ({
                              ...current,
                              widths: {
                                ...current.widths,
                                [column.id]: clampWidth(Number(event.target.value), column.minWidth, column.maxWidth),
                              },
                            }))
                          }
                        />
                      ) : null}
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id} className={cn(rowClass, "bg-surface")}>
                  <td className="sticky start-0 bg-surface p-2">
                    <Checkbox label={row.id} checked={selected.has(row.id)} onChange={() => setSelected((current) => toggleSelection(current, row.id))} />
                  </td>
                  {visibleColumns.map((column) => {
                    const side = column.sticky ? stickyLogicalSide(column.sticky) : undefined;
                    return (
                      <td
                        key={column.id}
                        className="border-b border-border p-2"
                        style={{ position: column.sticky ? "sticky" : undefined, insetInlineStart: side === "inline-start" ? 48 : undefined }}
                      >
                        {(column.cell ?? ((item: T) => String(column.accessor(item))))(row)}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
      <div className="flex flex-wrap items-center gap-2">
        <Button type="button" tone="secondary" disabled={query.page <= 1} onClick={() => setQuery((current) => ({ ...current, page: current.page - 1 }))}>
          {messages.previous}
        </Button>
        <span className="text-sm">
          {query.page} / {pageCount}
        </span>
        <Button type="button" tone="secondary" disabled={query.page >= pageCount} onClick={() => setQuery((current) => ({ ...current, page: current.page + 1 }))}>
          {messages.next}
        </Button>
        <Select aria-label={messages.pageSize} value={String(query.pageSize)} onChange={(event) => setQuery((current) => ({ ...current, page: 1, pageSize: Number(event.target.value) }))}>
          {[10, 20, 50].map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </Select>
        <Button type="button" tone="ghost" onClick={() => setSelected(new Set())}>
          {messages.clearSelection}
        </Button>
      </div>
      <Drawer open={filtersOpen} onClose={() => setFiltersOpen(false)} title={messages.filters}>
        <div className="flex flex-col gap-3">
          {columns
            .filter((column) => column.filterable !== false && column.filterKind)
            .map((column) => (
              <FilterControl
                key={column.id}
                column={column}
                value={query.filters[column.id]}
                onChange={(value) => setQuery((current) => ({ ...current, page: 1, filters: { ...current.filters, [column.id]: value } }))}
                entityLookup={entityLookup}
              />
            ))}
          <Button type="button" onClick={() => setFiltersOpen(false)}>
            {messages.close}
          </Button>
        </div>
      </Drawer>
      <Drawer open={columnsOpen} onClose={() => setColumnsOpen(false)} title={messages.columns}>
        <div className="flex flex-col gap-3">
          {layout.order.map((id) => {
            const column = columns.find((item) => item.id === id);
            if (!column || column.hideable === false) {
              return null;
            }
            return (
              <div key={id} className="flex items-center gap-2">
                <Checkbox
                  label={column.header}
                  checked={layout.visibility[id] !== false}
                  onChange={() => setLayout((current) => ({ ...current, visibility: { ...current.visibility, [id]: current.visibility[id] === false } }))}
                />
                <Button
                  type="button"
                  tone="ghost"
                  onClick={() => {
                    const index = layout.order.indexOf(id);
                    const target = layout.order[index - 1];
                    if (target) {
                      setLayout((current) => ({ ...current, order: moveColumn(current.order, id, target) }));
                    }
                  }}
                >
                  ↑
                </Button>
                <Button
                  type="button"
                  tone="ghost"
                  onClick={() => {
                    const index = layout.order.indexOf(id);
                    const target = layout.order[index + 1];
                    if (target) {
                      setLayout((current) => ({ ...current, order: moveColumn(current.order, id, target) }));
                    }
                  }}
                >
                  ↓
                </Button>
              </div>
            );
          })}
          <Button
            type="button"
            tone="secondary"
            onClick={() =>
              setLayout(
                defaultLayout(
                  columns.map((column) => column.id),
                  Object.fromEntries(columns.map((column) => [column.id, column.width])),
                ),
              )
            }
          >
            {messages.restoreColumns}
          </Button>
        </div>
      </Drawer>
      <p className="sr-only">{serializeGridQuery(query)}</p>
    </div>
  );
}

/**
 * ذخیرهٔ نمایش در حافظه برای ویترین. پیاده‌سازی سرور از همین قرارداد SavedViewStore استفاده می‌کند.
 */
export function createMemorySavedViewStore(): SavedViewStore {
  const views = new Map<string, SavedGridView>();
  return {
    async list() {
      return [...views.values()];
    },
    async save(view) {
      views.set(view.id, view);
    },
    async remove(id) {
      views.delete(id);
    },
  };
}
