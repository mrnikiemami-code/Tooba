"use client";

import { useCallback, useRef, useState } from "react";
import type { IHeaderParams } from "ag-grid-community";
import type { GridFilterValue } from "../data-grid/types";
import { isFilterActive } from "../data-grid/serialize";
import { ColumnFilterPopover } from "./column-filter-popover";
import { ColumnFilterIcon } from "./column-filter-icon";
import { JalaliHeaderFilterPanel } from "./jalali-header-filter-panel";
import { NumberHeaderFilterPanel } from "./number-header-filter-panel";
import { StatusHeaderFilterPanel, type StatusFilterOption } from "./status-header-filter-panel";
import { TextHeaderFilterPanel } from "./text-header-filter-panel";

export type ExternalHeaderFilterKind = "text" | "jalali-date" | "number" | "status";

export type AppGridHeaderContext = {
  locale?: "fa" | "en";
  externalFilters?: Record<string, GridFilterValue>;
  onExternalFilterApply?: (field: string, value: GridFilterValue | null) => void;
  statusFilterOptions?: StatusFilterOption[];
};

export type AppColumnHeaderParams = {
  externalFilter?: ExternalHeaderFilterKind;
  filterValueLabel?: string;
  /** اگر باشد، بر گزینه‌های سراسری گرید اولویت دارد (مثلاً نقش اختصاص vs وضعیت محصول). */
  statusFilterOptions?: StatusFilterOption[];
};

/** هدر ستون با فیلتر app-owned — آیکون یکپارچه؛ بدون AG Grid filter popup. */
export function AppColumnHeader(props: IHeaderParams & AppColumnHeaderParams) {
  const field = String(props.column.getColDef().field ?? props.column.getColId());
  const displayName = props.displayName ?? field;
  const filterKind = props.externalFilter;
  const ctx = (props.context ?? {}) as AppGridHeaderContext;
  const locale = ctx.locale ?? "fa";
  const activeFilter = ctx.externalFilters?.[field];
  const isActive = Boolean(activeFilter && isFilterActive(activeFilter));
  const statusOptions = props.statusFilterOptions ?? ctx.statusFilterOptions ?? [];
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
            className="inline-flex size-8 shrink-0 items-center justify-center rounded-ds text-sm hover:bg-secondary/90"
            aria-label={`فیلتر ${displayName}`}
            aria-expanded={open}
            data-filter-active={isActive ? "true" : "false"}
            data-testid={`header-filter-trigger-${field}`}
            onClick={() => setOpen((value) => !value)}
          >
            <ColumnFilterIcon active={isActive} />
          </button>
          <ColumnFilterPopover
            open={open}
            onClose={() => {
              setOpen(false);
              triggerRef.current?.focus();
            }}
            anchorRef={triggerRef}
            title={`فیلتر ${displayName}`}
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
            ) : filterKind === "number" ? (
              <NumberHeaderFilterPanel
                locale={locale}
                value={activeFilter?.kind === "number" ? activeFilter : undefined}
                valueLabel={props.filterValueLabel}
                onApply={applyFilter}
                onClear={() => applyFilter(null)}
              />
            ) : filterKind === "status" ? (
              <StatusHeaderFilterPanel
                locale={locale}
                value={activeFilter?.kind === "status" ? activeFilter : undefined}
                options={statusOptions}
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
