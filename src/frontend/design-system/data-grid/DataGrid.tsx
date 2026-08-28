"use client";

import { useCallback, useEffect, useMemo, useState, type KeyboardEvent } from "react";
import { cn } from "../cn";
import { Button, Checkbox, EmptyState, ErrorState, Input, Select, Skeleton, Spinner } from "../primitives/core";
import { Drawer } from "../primitives/overlays";
import { FilterControl } from "./FilterControl";
import { faFilterOperatorLabels, faGridMessages } from "./messages";
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
  GridFilterValue,
  GridMessages,
  GridQueryAdapter,
  GridServerQuery,
  SavedGridView,
  SavedViewStore,
} from "./types";

/** عرض ثابت ستون انتخاب — با آفست چسبان ستون‌های sticky هماهنگ است. */
const SELECTION_COLUMN_WIDTH = 44;

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
      Object.fromEntries(columns.map((column) => [column.id, column.defaultVisible !== false])),
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
  const [activeViewId, setActiveViewId] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [drawerDragId, setDrawerDragId] = useState<string | null>(null);
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
  const activeFilterEntries = Object.entries(query.filters).filter(([, value]) => isFilterActive(value));
  const activeFilters = activeFilterEntries.length;
  const rowClass = density === "compact" ? "h-12" : "h-16";
  const exportIds = columns.filter((column) => column.exportable !== false).map((column) => column.id);
  const ops = faFilterOperatorLabels;

  function filterChipLabel(columnId: string, value: GridFilterValue): string {
    const header = columns.find((column) => column.id === columnId)?.header ?? columnId;
    switch (value.kind) {
      case "text":
        return `${header}: ${ops[value.operator]} ${value.query}`;
      case "number": {
        const opLabel =
          value.operator === "greaterThan"
            ? ops.greaterThan
            : value.operator === "lessThan"
              ? ops.lessThan
              : value.operator === "between"
                ? ops.between
                : ops.equals;
        return `${header}: ${opLabel} ${value.value}${value.valueTo != null ? `–${value.valueTo}` : ""}`;
      }
      case "money": {
        const opLabel =
          value.operator === "greaterThan"
            ? ops.greaterThan
            : value.operator === "lessThan"
              ? ops.lessThan
              : value.operator === "between"
                ? ops.between
                : ops.equals;
        return `${header}: ${opLabel} ${value.money.amount}${value.money.amountTo != null ? `–${value.money.amountTo}` : ""}`;
      }
      case "date": {
        const opLabel =
          value.operator === "before"
            ? ops.before
            : value.operator === "after"
              ? ops.after
              : value.operator === "between"
                ? ops.between
                : ops.on;
        return `${header}: ${opLabel} ${value.iso}${value.isoTo ? `–${value.isoTo}` : ""}`;
      }
      case "enum":
      case "status": {
        const options = columns.find((column) => column.id === columnId)?.enumOptions ?? [];
        const labels = value.values.map((code) => options.find((option) => option.value === code)?.label ?? code);
        return `${header}: ${ops.equals} ${labels.join("، ")}`;
      }
      case "boolean":
        return `${header}: ${value.state === "true" ? ops.yes : value.state === "false" ? ops.no : ops.all}`;
      case "entity":
        return `${header}: ${ops.equals} ${value.ids.length}`;
      default:
        return header;
    }
  }

  function clearFilter(columnId: string) {
    setQuery((current) => {
      const next = { ...current.filters };
      delete next[columnId];
      return { ...current, page: 1, filters: next };
    });
    setActiveViewId(null);
  }

  function clearAllFilters() {
    setQuery((current) => ({ ...current, page: 1, filters: {}, search: undefined }));
    setActiveViewId(null);
  }

  function applyView(view: SavedGridView) {
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
    setActiveViewId(view.id);
  }

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
      setActiveViewId(null);
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <Input
          aria-label={messages.search}
          placeholder={messages.search}
          value={query.search ?? ""}
          onChange={(event) => {
            setQuery((current) => ({ ...current, page: 1, search: event.target.value }));
            setActiveViewId(null);
          }}
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
        <div className="flex flex-col gap-2 rounded-ds border border-border bg-secondary/40 p-2" data-testid="grid-saved-views">
          <div className="flex flex-wrap items-center gap-2">
            <Input
              aria-label={messages.saveView}
              placeholder={messages.defaultViewName}
              value={viewName}
              onChange={(event) => setViewName(event.target.value)}
              className="min-w-[10rem] flex-1"
            />
            <Button
              type="button"
              tone="secondary"
              onClick={() => {
                const view: SavedGridView = {
                  id: crypto.randomUUID(),
                  name: viewName.trim() || messages.defaultViewName,
                  filters: query.filters,
                  sorts: query.sorts,
                  layout,
                  pageSize: query.pageSize,
                  density,
                };
                void savedViewStore.save(view).then(() =>
                  savedViewStore.list().then((next) => {
                    setViews(next);
                    setActiveViewId(view.id);
                    setViewName("");
                  }),
                );
              }}
            >
              {messages.saveView}
            </Button>
          </div>
          {views.length > 0 ? (
            <ul className="flex flex-wrap gap-2">
              {views.map((view) => {
                const active = activeViewId === view.id;
                return (
                  <li key={view.id} className="inline-flex items-center gap-1">
                    <button
                      type="button"
                      className={cn(
                        "inline-flex min-h-9 items-center rounded-full px-3 text-sm font-medium transition-colors",
                        active ? "bg-primary text-primary-foreground shadow-sm" : "bg-surface border border-border hover:bg-secondary",
                      )}
                      aria-pressed={active}
                      onClick={() => applyView(view)}
                    >
                      {view.name || messages.defaultViewName}
                    </button>
                    <button
                      type="button"
                      className="inline-flex size-8 items-center justify-center rounded-full text-muted hover:bg-danger/10 hover:text-danger"
                      aria-label={`${messages.deleteView}: ${view.name || messages.defaultViewName}`}
                      onClick={() => {
                        void savedViewStore.remove(view.id).then(() =>
                          savedViewStore.list().then((next) => {
                            setViews(next);
                            if (activeViewId === view.id) {
                              setActiveViewId(null);
                            }
                          }),
                        );
                      }}
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
      {activeFilters > 0 ? (
        <div className="flex flex-wrap items-center gap-2" data-testid="grid-filter-chips">
          {activeFilterEntries.map(([columnId, value]) => (
            <button
              key={columnId}
              type="button"
              className="inline-flex min-h-9 items-center gap-2 rounded-full bg-secondary px-3 text-sm"
              onClick={() => clearFilter(columnId)}
              aria-label={`${messages.clearFilter} ${columns.find((column) => column.id === columnId)?.header ?? columnId}`}
            >
              <span>{filterChipLabel(columnId, value)}</span>
              <span aria-hidden>×</span>
            </button>
          ))}
          <Button type="button" tone="ghost" onClick={clearAllFilters}>
            {messages.clearAllFilters}
          </Button>
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
              <Checkbox hideLabel label={messages.selectRow} checked={selected.has(row.id)} onChange={() => setSelected((current) => toggleSelection(current, row.id))} />
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
        <div className="min-w-0 overflow-x-auto rounded-ds border border-border">
          <table className="w-full table-fixed border-separate border-spacing-0 text-base">
            <thead className="sticky top-0 z-10 bg-surface">
              <tr>
                <th
                  className="sticky start-0 z-20 border-b border-border bg-surface p-1"
                  style={{ width: SELECTION_COLUMN_WIDTH, minWidth: SELECTION_COLUMN_WIDTH, maxWidth: SELECTION_COLUMN_WIDTH }}
                >
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
                          setActiveViewId(null);
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
                          setActiveViewId(null);
                        }
                      }}
                      style={{
                        width: layout.widths[column.id] ?? column.width,
                        minWidth: column.minWidth,
                        maxWidth: column.maxWidth,
                        position: column.sticky ? "sticky" : undefined,
                        insetInlineStart: side === "inline-start" ? SELECTION_COLUMN_WIDTH : undefined,
                        insetInlineEnd: side === "inline-end" ? 0 : undefined,
                      }}
                      className="relative border-b border-border bg-surface p-3 text-start"
                    >
                      <button
                        type="button"
                        className="font-medium"
                        onClick={() => {
                          if (column.sortable === false) {
                            return;
                          }
                          setQuery((current) => ({ ...current, page: 1, sorts: cycleSort(current.sorts, column.id) }));
                          setActiveViewId(null);
                        }}
                        onKeyDown={(event) => onSortKey(column, event)}
                      >
                        {column.header}
                        {query.sorts[0]?.columnId === column.id ? (query.sorts[0].direction === "asc" ? " ↑" : " ↓") : ""}
                      </button>
                      {column.resizable !== false ? (
                        <span
                          role="separator"
                          aria-orientation="vertical"
                          aria-label={`${messages.resizeColumn} ${column.header}`}
                          className="absolute end-0 top-0 z-10 h-full w-2 cursor-col-resize after:absolute after:inset-y-3 after:end-0 after:w-px after:bg-border hover:after:bg-primary"
                          onPointerDown={(event) => {
                            event.preventDefault();
                            event.stopPropagation();
                            const origin = event.clientX;
                            const initial = layout.widths[column.id] ?? column.width;
                            const rtl = document.documentElement.dir === "rtl";
                            const onMove = (move: PointerEvent) => {
                              const delta = rtl ? origin - move.clientX : move.clientX - origin;
                              setLayout((current) => ({
                                ...current,
                                widths: {
                                  ...current.widths,
                                  [column.id]: clampWidth(initial + delta, column.minWidth, column.maxWidth),
                                },
                              }));
                              setActiveViewId(null);
                            };
                            const onUp = () => {
                              window.removeEventListener("pointermove", onMove);
                              window.removeEventListener("pointerup", onUp);
                            };
                            window.addEventListener("pointermove", onMove);
                            window.addEventListener("pointerup", onUp);
                          }}
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
                  <td
                    className="sticky start-0 bg-surface p-1"
                    style={{ width: SELECTION_COLUMN_WIDTH, minWidth: SELECTION_COLUMN_WIDTH, maxWidth: SELECTION_COLUMN_WIDTH }}
                  >
                    <Checkbox hideLabel label={messages.selectRow} checked={selected.has(row.id)} onChange={() => setSelected((current) => toggleSelection(current, row.id))} />
                  </td>
                  {visibleColumns.map((column) => {
                    const side = column.sticky ? stickyLogicalSide(column.sticky) : undefined;
                    return (
                      <td
                        key={column.id}
                        className="border-b border-border p-3"
                        style={{ position: column.sticky ? "sticky" : undefined, insetInlineStart: side === "inline-start" ? SELECTION_COLUMN_WIDTH : undefined }}
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
        <div className="flex flex-col gap-2">
          {columns
            .filter((column) => column.filterable !== false && column.filterKind)
            .map((column) => (
              <FilterControl
                key={column.id}
                column={column}
                value={query.filters[column.id]}
                onChange={(value) => {
                  setQuery((current) => ({ ...current, page: 1, filters: { ...current.filters, [column.id]: value } }));
                  setActiveViewId(null);
                }}
                entityLookup={entityLookup}
              />
            ))}
          <Button type="button" tone="secondary" onClick={() => setFiltersOpen(false)}>
            {messages.close}
          </Button>
          {activeFilters > 0 ? (
            <Button type="button" tone="ghost" onClick={clearAllFilters}>
              {messages.clearAllFilters}
            </Button>
          ) : null}
        </div>
      </Drawer>
      <Drawer open={columnsOpen} onClose={() => setColumnsOpen(false)} title={messages.columns}>
        <div className="flex flex-col gap-1.5">
          {layout.order.map((id) => {
            const column = columns.find((item) => item.id === id);
            if (!column || column.hideable === false) {
              return null;
            }
            return (
              <div
                key={id}
                draggable={column.reorderable !== false}
                onDragStart={() => setDrawerDragId(id)}
                onDragOver={(event) => event.preventDefault()}
                onDrop={() => {
                  if (drawerDragId) {
                    setLayout((current) => ({ ...current, order: moveColumn(current.order, drawerDragId, id) }));
                    setDrawerDragId(null);
                    setActiveViewId(null);
                  }
                }}
                className={cn(
                  "flex items-center gap-1.5 rounded-ds border border-border bg-surface px-2 py-1.5",
                  drawerDragId === id ? "opacity-60" : null,
                )}
              >
                <span
                  className="inline-flex size-8 cursor-grab items-center justify-center rounded-ds text-muted active:cursor-grabbing"
                  aria-label={messages.dragColumn}
                  title={messages.dragColumn}
                >
                  ⋮⋮
                </span>
                <div className="min-w-0 flex-1">
                  <Checkbox
                    label={column.header}
                    checked={layout.visibility[id] !== false}
                    onChange={() => {
                      setLayout((current) => ({ ...current, visibility: { ...current.visibility, [id]: current.visibility[id] === false } }));
                      setActiveViewId(null);
                    }}
                  />
                </div>
                <Button
                  type="button"
                  tone="ghost"
                  aria-label={messages.moveColumnUp}
                  onClick={() => {
                    const index = layout.order.indexOf(id);
                    const target = layout.order[index - 1];
                    if (target) {
                      setLayout((current) => ({ ...current, order: moveColumn(current.order, id, target) }));
                      setActiveViewId(null);
                    }
                  }}
                >
                  ↑
                </Button>
                <Button
                  type="button"
                  tone="ghost"
                  aria-label={messages.moveColumnDown}
                  onClick={() => {
                    const index = layout.order.indexOf(id);
                    const target = layout.order[index + 1];
                    if (target) {
                      setLayout((current) => ({ ...current, order: moveColumn(current.order, id, target) }));
                      setActiveViewId(null);
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
            onClick={() => {
              setLayout(
                defaultLayout(
                  columns.map((column) => column.id),
                  Object.fromEntries(columns.map((column) => [column.id, column.width])),
                  Object.fromEntries(columns.map((column) => [column.id, column.defaultVisible !== false])),
                ),
              );
              setActiveViewId(null);
            }}
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
