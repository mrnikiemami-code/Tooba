"use client";

import { useCallback, useRef, useState } from "react";
import type { IHeaderParams } from "ag-grid-community";
import type { GridFilterValue } from "../data-grid/types";
import { isFilterActive } from "../data-grid/serialize";
import { ColumnFilterPopover } from "./column-filter-popover";
import { JalaliHeaderFilterPanel } from "./jalali-header-filter-panel";
import { TextHeaderFilterPanel } from "./text-header-filter-panel";

export type ExternalHeaderFilterKind = "text" | "jalali-date";

export type AppGridHeaderContext = {
  locale?: "fa" | "en";
  externalFilters?: Record<string, GridFilterValue>;
  onExternalFilterApply?: (field: string, value: GridFilterValue | null) => void;
};

export type AppColumnHeaderParams = {
  externalFilter?: ExternalHeaderFilterKind;
};

/** هدر ستون با فیلتر app-owned — بدون AG Grid filter popup. */
export function AppColumnHeader(props: IHeaderParams & AppColumnHeaderParams) {
  const field = String(props.column.getColDef().field ?? props.column.getColId());
  const displayName = props.displayName ?? field;
  const filterKind = props.externalFilter;
  const ctx = (props.context ?? {}) as AppGridHeaderContext;
  const locale = ctx.locale ?? "fa";
  const activeFilter = ctx.externalFilters?.[field];
  const isActive = Boolean(activeFilter && isFilterActive(activeFilter));
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);

  const sort = props.column.getSort();
  const sortIndicator = sort === "asc" ? "↑" : sort === "desc" ? "↓" : "";

  const applyFilter = useCallback(
    (value: GridFilterValue | null) => {
      ctx.onExternalFilterApply?.(field, value);
      setOpen(false);
      triggerRef.current?.focus();
    },
    [ctx, field],
  );

  return (
    <div className="flex h-full w-full items-center gap-1" data-app-column-header>
      <button
        type="button"
        className="flex min-w-0 flex-1 items-center gap-1 truncate text-start text-[13px] font-semibold"
        onClick={(event) => props.progressSort?.(event.shiftKey)}
      >
        <span className="truncate">{displayName}</span>
        {sortIndicator ? <span className="text-xs text-muted">{sortIndicator}</span> : null}
      </button>
      {filterKind ? (
        <>
          <button
            ref={triggerRef}
            type="button"
            className={`inline-flex size-7 shrink-0 items-center justify-center rounded-ds text-sm ${
              isActive ? "bg-primary text-primary-foreground" : "text-muted hover:bg-secondary"
            }`}
            aria-label={`فیلتر ${displayName}`}
            aria-expanded={open}
            data-testid={`header-filter-trigger-${field}`}
            onClick={() => setOpen((value) => !value)}
          >
            ⚲
          </button>
          <ColumnFilterPopover
            open={open}
            onClose={() => {
              setOpen(false);
              triggerRef.current?.focus();
            }}
            anchorRef={triggerRef}
            title={filterKind === "jalali-date" ? `فیلتر ${displayName}` : `فیلتر ${displayName}`}
            width={filterKind === "jalali-date" ? 400 : 360}
            testId={`header-filter-popover-${field}`}
          >
            {filterKind === "jalali-date" ? (
              <JalaliHeaderFilterPanel
                locale={locale}
                value={activeFilter?.kind === "date" ? activeFilter : undefined}
                onApply={applyFilter}
                onClear={() => applyFilter(null)}
              />
            ) : (
              <TextHeaderFilterPanel
                locale={locale}
                value={activeFilter?.kind === "text" ? activeFilter : undefined}
                onApply={applyFilter}
                onClear={() => applyFilter(null)}
              />
            )}
          </ColumnFilterPopover>
        </>
      ) : null}
    </div>
  );
}
