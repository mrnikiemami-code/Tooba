import { useCallback, useMemo } from "react";
import { executeGridQuery } from "../data-grid/query-engine.ts";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../data-grid/types.ts";
import { AppDataGrid } from "./AppDataGrid.tsx";
import { buildLegacyGridBridge } from "./legacy-grid-bridge.ts";
import type { AdminGridQueryResult } from "./admin-grid-query-client.ts";
import type { StatusFilterOption } from "./status-header-filter-panel.tsx";

/** Pure client-side GridQuery adapter for bounded admin lists. */
export function createClientGridQueryAdapter<T>(
  rows: readonly T[],
  columns: GridColumnDef<T>[],
) {
  return async (query: GridServerQuery) => executeGridQuery(rows, columns, query);
}

export type LegacyAdminGridDirectProps<T extends { id: string }> = {
  gridId: string;
  columns: GridColumnDef<T>[];
  queryAdapter: (query: GridServerQuery) => Promise<{ rows: T[]; total: number }>;
  savedViewStore?: SavedViewStore;
  exportFilenameBase?: string;
  locale?: "fa" | "en";
  direction?: "rtl" | "ltr";
  statusFilterOptions?: StatusFilterOption[];
};

/** Hook wiring legacy GridColumnDef to direct AppDataGrid (no render wrapper component). */
export function useLegacyAdminGridDirectProps<T extends { id: string }>({
  gridId,
  columns,
  queryAdapter,
  savedViewStore,
  exportFilenameBase,
  locale = "fa",
  direction = "rtl",
  statusFilterOptions,
}: LegacyAdminGridDirectProps<T>) {
  const bridge = useMemo(() => buildLegacyGridBridge(columns, direction), [columns, direction]);
  const stableQueryAdapter = useCallback(queryAdapter, [queryAdapter]);

  return useMemo(
    () => ({
      gridId,
      columnDefs: bridge.columnDefs,
      queryAdapter: stableQueryAdapter,
      locale,
      direction,
      savedViewStore,
      advancedFilterColumns: bridge.advancedFilterColumns,
      externalFilterFields: bridge.externalFilterFields,
      getExportRow: bridge.getExportRow,
      exportHeaders: bridge.exportHeaders,
      exportFilenameBase: exportFilenameBase ?? gridId.replace(/\./g, "-"),
      statusFilterOptions,
    }),
    [
      bridge,
      direction,
      exportFilenameBase,
      gridId,
      locale,
      savedViewStore,
      stableQueryAdapter,
      statusFilterOptions,
    ],
  );
}

/** Maps server grid query result to AppDataGrid adapter shape. */
export function adminGridQueryAdapter<TRow extends { id: string }>(
  queryFn: (query: GridServerQuery) => Promise<AdminGridQueryResult<TRow>>,
  onDenied?: () => void,
  onError?: (message?: string) => void,
) {
  return async (query: GridServerQuery) => {
    const result = await queryFn(query);
    if (result.denied) {
      onDenied?.();
      throw new Error(result.message);
    }
    if (result.source === "error") {
      onError?.(result.message);
      throw new Error(result.message ?? "host-unreachable");
    }
    return result.page;
  };
}
