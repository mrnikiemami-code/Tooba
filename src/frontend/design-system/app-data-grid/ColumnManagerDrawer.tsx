"use client";

import { useMemo, useState } from "react";
import type { ColumnState } from "ag-grid-community";
import { Drawer } from "../primitives/overlays";
import { Button, Checkbox } from "../primitives/core";
import { moveColumn } from "../data-grid/serialize";
import { isColumnVisibilityLocked, resolveColumnLabel } from "./column-labels";

export function ColumnManagerDrawer({
  open,
  onClose,
  locale,
  title,
  searchPlaceholder,
  dragLabel,
  closeLabel,
  restoreLabel,
  lockedVisibilityLabel,
  columnLabels,
  columnState,
  onReorder,
  onToggleVisibility,
  onRestore,
}: {
  open: boolean;
  onClose: () => void;
  locale: "fa" | "en";
  title: string;
  searchPlaceholder: string;
  dragLabel: string;
  closeLabel: string;
  restoreLabel: string;
  lockedVisibilityLabel: string;
  columnLabels: Record<string, string>;
  columnState: ColumnState[];
  onReorder: (fromColId: string, toColId: string) => void;
  onToggleVisibility: (colId: string, hide: boolean) => void;
  onRestore: () => void;
}) {
  const [search, setSearch] = useState("");
  const [dragId, setDragId] = useState<string | null>(null);

  const rows = useMemo(() => {
    const query = search.trim().toLowerCase();
    return columnState.filter((col) => {
      if (!col.colId) return false;
      if (!query) return true;
      const label = resolveColumnLabel(col.colId, columnLabels, locale).toLowerCase();
      return label.includes(query) || col.colId.toLowerCase().includes(query);
    });
  }, [columnLabels, columnState, locale, search]);

  return (
    <Drawer open={open} onClose={onClose} title={title}>
      <div className="flex h-full min-h-[50vh] flex-col gap-3" data-testid="column-manager-drawer">
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={searchPlaceholder}
          className="min-h-[2.75rem] w-full rounded-ds border border-border bg-surface px-3 text-sm"
          data-testid="column-manager-search"
        />
        <div className="min-h-0 flex-1 space-y-2 overflow-y-auto">
          {rows.map((col) => {
            if (!col.colId) return null;
            const locked = isColumnVisibilityLocked(col.colId);
            const label = resolveColumnLabel(col.colId, columnLabels, locale);
            return (
              <div
                key={col.colId}
                data-column-manager-row
                data-col-id={col.colId}
                draggable
                onDragStart={() => setDragId(col.colId!)}
                onDragOver={(event) => event.preventDefault()}
                onDrop={() => {
                  if (!dragId || dragId === col.colId) return;
                  onReorder(dragId, col.colId!);
                  setDragId(null);
                }}
                className="flex min-h-[2.75rem] items-center gap-3 rounded-ds border border-border bg-surface-elevated px-3 py-2 shadow-sm"
              >
                <button
                  type="button"
                  className="inline-flex size-10 shrink-0 cursor-grab items-center justify-center rounded-ds text-muted hover:bg-secondary active:cursor-grabbing"
                  aria-label={dragLabel}
                  data-column-drag-handle
                  onMouseDown={(event) => event.stopPropagation()}
                >
                  ⠿
                </button>
                <div className="min-w-0 flex-1">
                  <Checkbox
                    label={label}
                    checked={!col.hide}
                    disabled={locked}
                    aria-label={locked ? `${label} — ${lockedVisibilityLabel}` : label}
                    onChange={() => {
                      if (locked) return;
                      onToggleVisibility(col.colId!, !col.hide);
                    }}
                  />
                </div>
              </div>
            );
          })}
        </div>
        <div className="sticky bottom-0 flex flex-wrap gap-2 border-t border-border bg-surface pt-3">
          <Button type="button" tone="ghost" onClick={onRestore}>
            {restoreLabel}
          </Button>
          <Button type="button" tone="secondary" onClick={onClose}>
            {closeLabel}
          </Button>
        </div>
      </div>
    </Drawer>
  );
}
