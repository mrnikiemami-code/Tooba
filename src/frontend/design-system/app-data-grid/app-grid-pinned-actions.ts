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
  /** تعداد جایگاه عملیات (۱–۳) برای عرض پیش‌فرض جمع‌تر؛ کاربر همچنان می‌تواند resize کند. */
  actionSlots?: 1 | 2 | 3;
};

function widthForActionSlots(slots: 1 | 2 | 3): { width: number; minWidth: number; maxWidth: number } {
  const perSlot = 40;
  const pad = 28;
  const width = pad + slots * perSlot;
  return { width, minWidth: width - 8, maxWidth: width + 48 };
}

/** ستون عملیات pin‌شده در انتهای گرید (RTL = چپ) — الگوی canonical. */
export function buildPinnedActionsColumnDef<T>({
  direction,
  cellRenderer,
  headerName = "عملیات",
  width,
  minWidth,
  maxWidth,
  actionSlots = 2,
}: AppGridPinnedActionsColumnOptions<T>): ColDef<T> {
  const slotWidths = widthForActionSlots(actionSlots);
  const actionsPin = pinnedGridEdge(direction);
  return {
    colId: "actions",
    headerName,
    width: width ?? slotWidths.width,
    minWidth: minWidth ?? slotWidths.minWidth,
    maxWidth: maxWidth ?? slotWidths.maxWidth,
    sortable: false,
    filter: false,
    resizable: true,
    lockVisible: true,
    lockPinned: true,
    lockPosition: actionsPin,
    pinned: actionsPin,
    cellClass: "app-grid-cell-align-center app-grid-actions-cell",
    cellRenderer,
  };
}
