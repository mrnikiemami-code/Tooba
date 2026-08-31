"use client";

import { useCallback, useMemo } from "react";
import { AppDataGrid } from "./AppDataGrid.tsx";
import { buildLegacyGridBridge } from "./legacy-grid-bridge.ts";
import { executeGridQuery } from "../data-grid/query-engine.ts";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../data-grid/types.ts";
import type { StatusFilterOption } from "./status-header-filter-panel.tsx";

export interface LegacyAppDataGridProps<T extends { id: string }> {
  gridId: string;
  columns: GridColumnDef<T>[];
  rows: T[];
  savedViewStore?: SavedViewStore;
  exportFilenameBase?: string;
  locale?: "fa" | "en";
  direction?: "rtl" | "ltr";
  statusFilterOptions?: StatusFilterOption[];
}

/**
 * AppDataGrid برای فهرست‌های Admin با دادهٔ client-side و ستون‌های legacy GridColumnDef.
 * مهاجرت یک‌جا از DataGrid قدیمی بدون تکرار زیرساخت گرید.
 */
export function LegacyAppDataGrid<T extends { id: string }>({
  gridId,
  columns,
  rows,
  savedViewStore,
  exportFilenameBase,
  locale = "fa",
  direction = "rtl",
  statusFilterOptions,
}: LegacyAppDataGridProps<T>) {
  const bridge = useMemo(() => buildLegacyGridBridge(columns, direction), [columns, direction]);
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => executeGridQuery(rows, columns, query),
    [rows, columns],
  );

  return (
    <AppDataGrid<T>
      gridId={gridId}
      columnDefs={bridge.columnDefs}
      queryAdapter={queryAdapter}
      locale={locale}
      direction={direction}
      savedViewStore={savedViewStore}
      advancedFilterColumns={bridge.advancedFilterColumns}
      externalFilterFields={bridge.externalFilterFields}
      getExportRow={bridge.getExportRow}
      exportHeaders={bridge.exportHeaders}
      exportFilenameBase={exportFilenameBase ?? gridId.replace(/\./g, "-")}
      statusFilterOptions={statusFilterOptions}
    />
  );
}
