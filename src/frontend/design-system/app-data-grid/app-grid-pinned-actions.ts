import type { ColDef, ICellRendererParams } from "ag-grid-community";
import type { ComponentType } from "react";
import { pinnedGridEdge } from "./grid-direction";

export type AppGridPinnedActionsColumnOptions<T> = {
  direction: "rtl" | "ltr";
  cellRenderer: ComponentType<ICellRendererParams<T>>;
  headerName?: string;
  width?: number;
  minWidth?: number;
  maxWidth?: number;
};

/** ستون عملیات pin‌شده در انتهای گرید (RTL = چپ) — الگوی canonical. */
export function buildPinnedActionsColumnDef<T>({
  direction,
  cellRenderer,
  headerName = "عملیات",
  width = 188,
  minWidth = 176,
  maxWidth = 240,
}: AppGridPinnedActionsColumnOptions<T>): ColDef<T> {
  const actionsPin = pinnedGridEdge(direction);
  return {
    colId: "actions",
    headerName,
    width,
    minWidth,
    maxWidth,
    sortable: false,
    filter: false,
    lockVisible: true,
    lockPinned: true,
    lockPosition: actionsPin,
    pinned: actionsPin,
    cellClass: "app-grid-cell-align-center",
    cellRenderer,
  };
}
