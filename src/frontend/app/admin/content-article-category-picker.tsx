"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  AppCategoryTree,
  buildCategoryPath,
  collectAncestorIds,
  buildParentMap,
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

/** گزینه‌های سلسله‌مراتبی قابل انتخاب برای مقاله (L1 و L2) — نگه داشته برای تست/سازگاری. */
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
 * انتخابگر سلسله‌مراتبی دستهٔ مقاله با AppCategoryTree — بدون CRUD و بدون شناسهٔ خام.
 */
export function ContentArticleCategoryPicker({
  rows,
  value,
  disabled,
  onChange,
  emptyLabel = "دسته‌ای برای این زبان ثبت نشده است.",
  placeholder = "جستجوی دسته…",
  testId = "content-article-category-picker",
}: {
  rows: ContentCategoryTreeNodeDto[];
  value: string;
  disabled?: boolean;
  onChange: (id: string, name: string) => void;
  emptyLabel?: string;
  placeholder?: string;
  testId?: string;
}) {
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);

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

  const assignableRows = useMemo(
    () =>
      rows.filter((row) => {
        if (row.status === "Archived" && row.id !== value) return false;
        return true;
      }),
    [rows, value],
  );

  const treeNodes = useMemo(() => toFlatNodes(assignableRows), [assignableRows]);

  const selectedPath = useMemo(() => {
    if (!value) return null;
    const path = buildCategoryPath(treeNodes, value);
    return path.length > 0 ? path.join(" › ") : null;
  }, [treeNodes, value]);

  const selectedLevel = useMemo(() => {
    if (!value) return null;
    return getCategoryTreeLevel(treeNodes, value);
  }, [treeNodes, value]);

  const selectedName = useMemo(() => {
    if (!value) return "";
    return treeNodes.find((n) => n.id === value)?.name ?? rows.find((r) => r.id === value)?.name ?? "";
  }, [rows, treeNodes, value]);

  useEffect(() => {
    if (!open || !value) return;
    const parentMap = buildParentMap(treeNodes);
    const ancestors = collectAncestorIds(parentMap, value);
    if (ancestors.length === 0) return;
    setExpandedKeys((prev) => {
      const merged = new Set([...prev, ...ancestors]);
      return [...merged];
    });
  }, [open, treeNodes, value]);

  return (
    <div ref={rootRef} className="relative" data-testid={testId}>
      <button
        type="button"
        className="flex w-full items-center justify-between rounded-xl border px-3 py-2 text-start text-sm disabled:opacity-60"
        disabled={disabled}
        data-testid={`${testId}-trigger`}
        onClick={() => !disabled && setOpen((v) => !v)}
      >
        <span className={selectedPath ? "" : "text-muted"}>
          {selectedPath ?? "— بدون دسته —"}
        </span>
        <span className="text-xs text-muted">
          {selectedLevel === 2 ? "زیردسته" : selectedLevel === 1 ? "دسته اصلی" : ""}
        </span>
      </button>

      {selectedPath ? (
        <p className="mt-1 text-xs text-muted" data-testid={`${testId}-path`}>
          {selectedPath}
        </p>
      ) : null}

      {open ? (
        <div
          className="absolute z-30 mt-1 min-w-[28rem] w-[min(100vw-2rem,36rem)] rounded-xl border bg-white p-3 shadow-lg"
          data-testid={`${testId}-panel`}
        >
          {assignableRows.length === 0 ? (
            <p className="px-2 py-3 text-sm text-muted" data-testid={`${testId}-empty`}>
              {emptyLabel}
            </p>
          ) : (
            <>
              <div className="mb-2 flex items-center justify-between gap-2">
                <button
                  type="button"
                  className="rounded-lg border px-2 py-1.5 text-xs hover:bg-slate-50"
                  data-testid={`${testId}-clear`}
                  onClick={() => {
                    onChange("", "");
                    setOpen(false);
                    setQuery("");
                  }}
                >
                  — بدون دسته —
                </button>
                {value && selectedName ? (
                  <span className="truncate text-xs text-muted" title={selectedPath ?? selectedName}>
                    انتخاب‌شده: {selectedPath ?? selectedName}
                  </span>
                ) : null}
              </div>
              <AppCategoryTree
                nodes={treeNodes}
                expandedKeys={expandedKeys}
                selectedKeys={value ? [value] : []}
                onExpandedKeysChange={setExpandedKeys}
                onSelect={(id) => {
                  const level = getCategoryTreeLevel(treeNodes, id);
                  if (level !== 1 && level !== 2) return;
                  const node = treeNodes.find((n) => n.id === id);
                  if (!node) return;
                  if (node.status === "Archived" && node.id !== value) return;
                  onChange(id, node.name);
                  setOpen(false);
                  setQuery("");
                }}
                searchQuery={query}
                onSearchQueryChange={setQuery}
                allowDrag={false}
                maxDepth={2}
                direction="rtl"
                uiLocale="fa"
                title="انتخاب دسته"
                searchPlaceholder={placeholder}
                virtualHeight={280}
                className="border-0 shadow-none"
              />
            </>
          )}
        </div>
      ) : null}
    </div>
  );
}
