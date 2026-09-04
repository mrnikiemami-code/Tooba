"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import {
  buildCategoryPath,
  getCategoryTreeLevel,
  type AppCategoryTreeNode,
} from "../../design-system";
import type { ContentCategoryTreeNodeDto } from "./content-category-api.ts";

function toFlatNodes(rows: ContentCategoryTreeNodeDto[]): AppCategoryTreeNode[] {
  return rows.map((row) => ({
    id: row.id,
    parentId: row.parentId,
    name: row.name,
    slug: row.slug,
    status: row.status === "Archived" ? "Archived" : "Published",
    sortOrder: row.sortOrder,
    isVisible: row.status !== "Archived",
    hasChildren: row.hasChildren,
    productCount: row.articleCount,
  }));
}

export type ContentArticleCategoryOption = {
  id: string;
  label: string;
  level: 1 | 2;
  active: boolean;
  name: string;
};

/** گزینه‌های سلسله‌مراتبی قابل انتخاب برای مقاله (L1 و L2). */
export function buildContentArticleCategoryOptions(
  rows: ContentCategoryTreeNodeDto[],
): ContentArticleCategoryOption[] {
  const flat = toFlatNodes(rows);
  return rows
    .map((row) => {
      const level = getCategoryTreeLevel(flat, row.id);
      if (level !== 1 && level !== 2) return null;
      const path = buildCategoryPath(flat, row.id);
      const label = level === 2 ? path.join(" › ") : row.name;
      return {
        id: row.id,
        label,
        level: level as 1 | 2,
        active: row.status !== "Archived",
        name: row.name,
      };
    })
    .filter((row): row is ContentArticleCategoryOption => row != null)
    .sort((a, b) => a.label.localeCompare(b.label, "fa"));
}

/**
 * انتخابگر سلسله‌مراتبی/جستجوپذیر دستهٔ مقاله — بدون شناسهٔ خام و بدون select تخت.
 */
export function ContentArticleCategoryPicker({
  options,
  value,
  disabled,
  onChange,
  emptyLabel = "دسته‌ای برای این زبان ثبت نشده است.",
  placeholder = "جستجوی دسته…",
  testId = "content-article-category-picker",
}: {
  options: ContentArticleCategoryOption[];
  value: string;
  disabled?: boolean;
  onChange: (id: string, name: string) => void;
  emptyLabel?: string;
  placeholder?: string;
  testId?: string;
}) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  useEffect(() => {
    if (!open) return;
    function onDoc(ev: MouseEvent) {
      if (!rootRef.current?.contains(ev.target as Node)) {
        setOpen(false);
        setQuery("");
      }
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  const selected = options.find((row) => row.id === value) ?? null;
  const assignable = useMemo(
    () =>
      options.filter((row) => {
        if (!row.active && row.id !== value) return false;
        const q = query.trim().toLowerCase();
        if (!q) return true;
        return row.label.toLowerCase().includes(q) || row.name.toLowerCase().includes(q);
      }),
    [options, query, value],
  );

  return (
    <div ref={rootRef} className="relative" data-testid={testId}>
      <button
        type="button"
        className="flex w-full items-center justify-between rounded-xl border px-3 py-2 text-start text-sm disabled:opacity-60"
        disabled={disabled}
        data-testid={`${testId}-trigger`}
        onClick={() => !disabled && setOpen((v) => !v)}
      >
        <span className={selected ? "" : "text-muted"}>
          {selected ? selected.label : "— بدون دسته —"}
        </span>
        <span className="text-xs text-muted">{selected?.level === 2 ? "زیردسته" : selected ? "دسته اصلی" : ""}</span>
      </button>

      {open ? (
        <div
          className="absolute z-20 mt-1 w-full rounded-xl border bg-white p-2 shadow-lg"
          data-testid={`${testId}-panel`}
        >
          <input
            className="mb-2 w-full rounded-lg border px-3 py-2 text-sm"
            value={query}
            placeholder={placeholder}
            data-testid={`${testId}-search`}
            onChange={(e) => setQuery(e.target.value)}
            autoFocus
          />
          {options.length === 0 ? (
            <p className="px-2 py-3 text-sm text-muted" data-testid={`${testId}-empty`}>
              {emptyLabel}
            </p>
          ) : (
            <ul id={listId} className="max-h-64 overflow-auto" data-testid={`${testId}-list`}>
              <li>
                <button
                  type="button"
                  className="w-full rounded-lg px-2 py-2 text-start text-sm hover:bg-slate-50"
                  data-testid={`${testId}-clear`}
                  onClick={() => {
                    onChange("", "");
                    setOpen(false);
                    setQuery("");
                  }}
                >
                  — بدون دسته —
                </button>
              </li>
              {assignable.map((row) => (
                <li key={row.id}>
                  <button
                    type="button"
                    className={`flex w-full items-center justify-between gap-2 rounded-lg px-2 py-2 text-start text-sm hover:bg-slate-50 ${
                      row.id === value ? "bg-slate-100 font-medium" : ""
                    }`}
                    data-testid={`${testId}-option-${row.id}`}
                    onClick={() => {
                      if (!row.active && row.id !== value) return;
                      onChange(row.id, row.name);
                      setOpen(false);
                      setQuery("");
                    }}
                  >
                    <span className={row.level === 2 ? "ps-3" : ""}>{row.label}</span>
                    <span className="shrink-0 text-[11px] text-muted">
                      {row.level === 2 ? "زیردسته" : "دسته اصلی"}
                      {!row.active ? " · بایگانی" : ""}
                    </span>
                  </button>
                </li>
              ))}
              {assignable.length === 0 ? (
                <li className="px-2 py-3 text-sm text-muted">نتیجه‌ای یافت نشد.</li>
              ) : null}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}
