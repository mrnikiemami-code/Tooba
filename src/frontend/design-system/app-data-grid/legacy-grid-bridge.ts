import type { ColDef, ICellRendererParams } from "ag-grid-community";
import type { GridColumnDef } from "../data-grid/types.ts";
import { applyAppGridFilterHeader, type AppGridFilterKind, type AppGridFilterSpec } from "./app-grid-filter-header.ts";
import type { AppGridFilterColumnDef } from "./filter-column-def.ts";
import { pinnedGridEdge } from "./grid-direction.ts";

function mapFilterKind(filterKind: GridColumnDef<unknown>["filterKind"]): AppGridFilterKind {
  if (!filterKind || filterKind === "entity" || filterKind === "boolean") return "none";
  if (filterKind === "text") return "text";
  if (filterKind === "number" || filterKind === "money") return "number";
  if (filterKind === "date") return "jalali-date";
  if (filterKind === "enum" || filterKind === "status") return "status";
  return "none";
}

function mapPinned(
  sticky: GridColumnDef<unknown>["sticky"],
  direction: "rtl" | "ltr",
): ColDef["pinned"] {
  if (!sticky) return undefined;
  const edge = pinnedGridEdge(direction);
  if (sticky === "start") return edge;
  return edge === "left" ? "right" : "left";
}

function renderLegacyCell<T>(col: GridColumnDef<T>, params: ICellRendererParams<T>) {
  if (!params.data) return null;
  if (col.cell) return col.cell(params.data);
  const value = col.accessor(params.data);
  if (value == null || value === "") return "—";
  return String(value);
}

export type LegacyGridBridge<T> = {
  columnDefs: ColDef<T>[];
  advancedFilterColumns: AppGridFilterColumnDef[];
  externalFilterFields: string[];
  exportHeaders: string[];
  getExportRow: (row: T) => string[];
};

/** تبدیل GridColumnDefهای legacy P04 به ColDef سازگار با AppDataGrid. */
export function buildLegacyGridBridge<T>(
  columns: GridColumnDef<T>[],
  direction: "rtl" | "ltr" = "rtl",
): LegacyGridBridge<T> {
  const filterMatrix: Record<string, AppGridFilterSpec> = {};
  const advancedFilterColumns: AppGridFilterColumnDef[] = [];
  const dataColumns = columns.filter((column) => column.id !== "actions");
  const actionsColumn = columns.find((column) => column.id === "actions");

  const columnDefs = dataColumns.map((col) => {
    const field = col.id;
    const kind = mapFilterKind(col.filterKind);
    if (kind !== "none" && col.filterKind) {
      filterMatrix[field] = {
        field,
        kind,
        statusFilterOptions: col.enumOptions,
      };
      advancedFilterColumns.push({
        id: col.id,
        header: col.header,
        filterKind: col.filterKind,
        enumOptions: col.enumOptions,
      });
    }

    const base: ColDef<T> = {
      colId: col.id,
      field,
      headerName: col.header,
      sortable: col.sortable ?? false,
      width: col.width,
      minWidth: col.minWidth,
      maxWidth: col.maxWidth,
      hide: col.defaultVisible === false,
      pinned: mapPinned(col.sticky, direction),
      cellRenderer: (params: ICellRendererParams<T>) => renderLegacyCell(col, params),
    };

    return applyAppGridFilterHeader(
      base,
      kind !== "none" ? filterMatrix[field] : { field, kind: "none" },
    );
  });

  if (actionsColumn) {
    const actionsPin = pinnedGridEdge(direction);
    columnDefs.push({
      colId: "actions",
      headerName: actionsColumn.header,
      width: actionsColumn.width ?? 108,
      minWidth: actionsColumn.minWidth ?? 100,
      maxWidth: actionsColumn.maxWidth ?? 156,
      sortable: false,
      filter: false,
      resizable: true,
      lockVisible: true,
      lockPinned: true,
      lockPosition: actionsPin,
      pinned: actionsPin,
      cellClass: "app-grid-cell-align-center app-grid-actions-cell",
      cellRenderer: (params: ICellRendererParams<T>) => renderLegacyCell(actionsColumn, params),
    });
  }

  const exportableColumns = columns.filter((column) => column.exportable !== false && column.id !== "actions");
  return {
    columnDefs,
    advancedFilterColumns,
    externalFilterFields: Object.keys(filterMatrix),
    exportHeaders: exportableColumns.map((column) => column.header),
    getExportRow: (row: T) =>
      exportableColumns.map((column) => {
        const value = column.accessor(row);
        return value == null ? "" : String(value);
      }),
  };
}
