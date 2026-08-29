"use client";

/**
 * انتخابگر دستهٔ محصول — سلسله‌مراتبی سه‌سطحی؛ فقط سطح ۳ قابل اختصاص؛ مسیر انسانی؛ بدون شناسهٔ خام.
 */

import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { buildCategoryPath, type AppCategoryTreeNode } from "../../design-system";
import {
  loadCategoryTree,
  type CategoryTreeNodeDto,
} from "./catalog-category-api";
import {
  getCategoryLevel,
  isAssignableProductCategory,
  listCategoryChildren,
  PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA,
} from "./product-category-level";

const API_LOCALE = "fa-IR";
const PATH_JOIN = " > ";

function toTreeNodes(rows: CategoryTreeNodeDto[]): AppCategoryTreeNode[] {
  return rows.map((r) => ({
    id: r.id,
    parentId: r.parentId,
    name: r.name,
    slug: r.slug,
    status: r.status,
    sortOrder: r.sortOrder,
    isVisible: r.isVisible,
    hasChildren: r.hasChildren,
    productCount: r.productCount,
  }));
}

function pathLabel(nodes: AppCategoryTreeNode[], id: string): string {
  return buildCategoryPath(nodes, id).join(PATH_JOIN) || nodes.find((n) => n.id === id)?.name || "";
}

export function ProductCategoryPicker({
  value,
  onChange,
  label = "دسته",
  required = false,
  disabled = false,
  emptyLabel = "انتخاب دسته…",
  invalidSelectionHint = false,
}: {
  value: string | null;
  onChange: (next: string | null) => void;
  label?: string;
  required?: boolean;
  disabled?: boolean;
  emptyLabel?: string;
  /** هشدار وقتی مقدار فعلی سطح ۳ نیست (دادهٔ قدیمی). */
  invalidSelectionHint?: boolean;
}) {
  const [nodes, setNodes] = useState<AppCategoryTreeNode[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [expanded, setExpanded] = useState<Set<string>>(() => new Set());
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void loadCategoryTree(API_LOCALE).then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (result.state !== "ok" || !result.data) {
        setLoadError(result.message ?? "بارگذاری دسته‌ها ناموفق بود");
        setNodes([]);
        return;
      }
      setLoadError(null);
      setNodes(toTreeNodes(result.data));
    });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!value || nodes.length === 0) return;
    const byId = new Map(nodes.map((n) => [n.id, n]));
    const next = new Set<string>();
    let current: string | null = value;
    while (current && byId.has(current)) {
      const parent: string | null = byId.get(current)!.parentId;
      if (parent) next.add(parent);
      current = parent;
    }
    if (next.size > 0) {
      setExpanded((prev) => {
        const merged = new Set(prev);
        for (const id of next) merged.add(id);
        return merged;
      });
    }
  }, [value, nodes]);

  const selectedLabel = useMemo(() => {
    if (!value) return null;
    return pathLabel(nodes, value) || null;
  }, [nodes, value]);

  const selectedAssignable = isAssignableProductCategory(nodes, value);
  const showInvalid =
    invalidSelectionHint && Boolean(value) && nodes.length > 0 && !selectedAssignable;

  const searchHits = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return [];
    return nodes
      .map((n) => {
        const path = pathLabel(nodes, n.id);
        const level = getCategoryLevel(nodes, n.id) ?? 0;
        return { id: n.id, name: n.name, path, level, assignable: level === 3 };
      })
      .filter((o) => o.path.toLowerCase().includes(q) || o.name.toLowerCase().includes(q));
  }, [nodes, query]);

  const roots = useMemo(() => listCategoryChildren(nodes, null), [nodes]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  function toggleExpand(id: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function selectAssignable(id: string) {
    if (!isAssignableProductCategory(nodes, id)) return;
    onChange(id);
    setOpen(false);
    setQuery("");
  }

  function focusSearchHit(id: string, assignable: boolean) {
    if (assignable) {
      selectAssignable(id);
      return;
    }
    const byId = new Map(nodes.map((n) => [n.id, n]));
    const next = new Set(expanded);
    let current: string | null = id;
    while (current && byId.has(current)) {
      next.add(current);
      current = byId.get(current)!.parentId;
    }
    setExpanded(next);
    setQuery("");
  }

  function renderBranch(parentId: string | null, depth: number): ReactNode {
    const children = listCategoryChildren(nodes, parentId);
    return children.map((node) => {
      const level = getCategoryLevel(nodes, node.id) ?? depth + 1;
      const kids = listCategoryChildren(nodes, node.id);
      const hasKids = kids.length > 0;
      const isOpen = expanded.has(node.id);
      const assignable = level === 3;
      const active = node.id === value;
      const pad = `${0.75 + depth * 0.85}rem`;

      if (assignable) {
        return (
          <li key={node.id}>
            <button
              type="button"
              role="option"
              aria-selected={active}
              className={
                active
                  ? "flex min-h-10 w-full items-center gap-2 py-2 text-start text-sm font-semibold text-[#2563EB] bg-blue-50"
                  : "flex min-h-10 w-full items-center gap-2 py-2 text-start text-sm text-slate-800 hover:bg-slate-50"
              }
              style={{ paddingInlineStart: pad, paddingInlineEnd: "0.75rem" }}
              onClick={() => selectAssignable(node.id)}
              data-testid={`product-category-option-${node.id}`}
              data-category-level={level}
              data-assignable="true"
            >
              <span className="truncate">{node.name}</span>
            </button>
          </li>
        );
      }

      return (
        <li key={node.id}>
          <button
            type="button"
            className="flex min-h-10 w-full items-center gap-2 py-2 text-start text-sm font-medium text-slate-700 hover:bg-slate-50"
            style={{ paddingInlineStart: pad, paddingInlineEnd: "0.75rem" }}
            onClick={() => {
              if (hasKids) toggleExpand(node.id);
            }}
            aria-expanded={hasKids ? isOpen : undefined}
            data-testid={`product-category-branch-${node.id}`}
            data-category-level={level}
            data-assignable="false"
          >
            <span
              className={`inline-flex size-5 shrink-0 items-center justify-center text-slate-500 transition-transform ${
                isOpen ? "rotate-90" : ""
              }`}
              aria-hidden
            >
              {hasKids ? "▸" : "·"}
            </span>
            <span className="truncate">{node.name}</span>
          </button>
          {hasKids && isOpen ? <ul>{renderBranch(node.id, depth + 1)}</ul> : null}
        </li>
      );
    });
  }

  return (
    <div className="relative" ref={rootRef} data-testid="product-category-picker">
      <span className="block text-sm font-medium text-slate-700">
        {label}
        {required ? <span className="text-danger"> *</span> : null}
      </span>
      <button
        type="button"
        disabled={disabled || loading}
        className="mt-1 flex min-h-11 w-full items-center justify-between rounded-xl border border-gray-200 bg-white px-3 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => {
          if (disabled || loading) return;
          setOpen((v) => !v);
          setQuery("");
        }}
        data-testid="product-category-picker-trigger"
      >
        <span className="truncate text-start">
          {loading ? "در حال بارگذاری…" : (selectedLabel ?? emptyLabel)}
        </span>
        <span className="ms-2 text-slate-400" aria-hidden>
          ▾
        </span>
      </button>
      {showInvalid ? (
        <p className="mt-1 text-sm text-amber-800" data-testid="product-category-invalid-level">
          {PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA}
        </p>
      ) : null}
      {loadError ? <p className="mt-1 text-sm text-danger">{loadError}</p> : null}
      {open ? (
        <div
          className="absolute z-20 mt-1 max-h-72 w-full overflow-hidden rounded-xl border border-gray-200 bg-white shadow-lg"
          role="listbox"
          data-testid="product-category-picker-list"
        >
          <div className="border-b border-gray-100 p-2">
            <input
              className="min-h-10 w-full rounded-lg border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="جستجوی نام یا مسیر…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoFocus
              data-testid="product-category-picker-search"
              aria-label="جستجوی دسته"
            />
          </div>
          {query.trim() ? (
            <ul className="max-h-56 overflow-y-auto py-1" data-testid="product-category-search-results">
              {searchHits.length === 0 ? (
                <li className="px-3 py-2 text-sm text-slate-400">موردی یافت نشد</li>
              ) : (
                searchHits.map((hit) => (
                  <li key={hit.id}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={hit.id === value}
                      aria-disabled={!hit.assignable}
                      className={
                        hit.assignable
                          ? hit.id === value
                            ? "flex min-h-10 w-full flex-col px-3 py-2 text-start text-sm font-semibold text-[#2563EB] bg-blue-50"
                            : "flex min-h-10 w-full flex-col px-3 py-2 text-start text-sm text-slate-800 hover:bg-slate-50"
                          : "flex min-h-10 w-full flex-col px-3 py-2 text-start text-sm text-slate-600 hover:bg-slate-50"
                      }
                      onClick={() => focusSearchHit(hit.id, hit.assignable)}
                      data-testid={`product-category-search-${hit.id}`}
                      data-category-level={hit.level}
                      data-assignable={hit.assignable ? "true" : "false"}
                    >
                      <span className="truncate">{hit.path}</span>
                      {!hit.assignable ? (
                        <span className="mt-0.5 text-[11px] font-normal text-slate-400">
                          فقط برای مرور — قابل انتخاب نیست
                        </span>
                      ) : null}
                    </button>
                  </li>
                ))
              )}
            </ul>
          ) : (
            <ul className="max-h-56 overflow-y-auto py-1" data-testid="product-category-tree">
              {roots.length === 0 ? (
                <li className="px-3 py-2 text-sm text-slate-400">دسته‌ای تعریف نشده است</li>
              ) : (
                renderBranch(null, 0)
              )}
            </ul>
          )}
        </div>
      ) : null}
      <input type="hidden" value={value ?? ""} data-testid="product-category-picker-value" readOnly />
    </div>
  );
}
